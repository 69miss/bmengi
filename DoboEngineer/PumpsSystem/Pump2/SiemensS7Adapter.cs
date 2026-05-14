using Avalonia.Controls.Primitives;
using Dobo.Appl.Device;
using Dobo.Appl.HunterCmd;
using Dobo.Appl.Utility;
using IoTClient;
using IoTClient.Clients.Modbus;
using IoTClient.Clients.PLC;
using IoTClient.Common.Enums;
using IoTClient.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2
{
    /// <summary>
    /// 心跳配置
    /// </summary>
    public class HeartbeatConfig
    {
        public bool Enabled { get; set; } = false;
        public int IntervalMs { get; set; } = 1000;
        public Func<Task<bool>> WriteFunc { get; set; }
    }

    internal class SiemensS7Adapter : IProtocolAdapter0
    {
        private readonly SiemensClient client;
        private bool _isConnected;
        private CancellationTokenSource _heartbeatCts;

        // 异步锁，保护底层 Socket 并发安全
        private readonly SemaphoreSlim _plcLock = new SemaphoreSlim(1, 1);

        public HeartbeatConfig Heartbeat { get; } = new HeartbeatConfig();

        public string ConnectionString => throw new NotImplementedException();

        // 优化：不再依赖 client.Connected。因为无论是否开启心跳，我们自己维护的 _isConnected 
        // 已经被所有的读写操作实时刷新了，它是最准确的。
        public bool IsConnected => _isConnected;

        public string ProtocolName => "SiemensS7";

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<Tuple<string, object>> DataReceived;

        public SiemensS7Adapter(SiemensVersion type, string ip, int port = 102, byte slot = 1, byte rack = 0, int timeout = 1500)
        {
            client = new SiemensClient(type, ip, port, slot, rack);
        }

        private void SetConnectionState(bool newState)
        {
            if (_isConnected != newState)
            {
                _isConnected = newState;
                ConnectionStateChanged?.Invoke(this, _isConnected);
            }
        }

        #region 全局核心拦截器 (结合 client.Connected 的终极状态感知)

        /// <summary>
        /// 统一的执行包装器。
        /// 负责：线程池切换、并发排队、以及最精准的连接状态感知。
        /// </summary>
        private async Task<T> ExecuteWithConnectionCheckAsync<T>(Func<T> action)
        {
            await _plcLock.WaitAsync();
            try
            {
                var result = await Task.Run(action);
                SetConnectionState(true);

                return result;
            }
            catch (Exception ex)
            {
                // 剥离可能由 Task 包装的外层异常
                var actualEx = ex is AggregateException ae ? ae.GetBaseException() : ex;
                if (!client.Connected)
                {
                    SetConnectionState(false);
                }
                else
                {
                    Console.WriteLine($"[PLC警告] 网络正常，但读写发生业务异常: {actualEx.Message}");
                }
                throw actualEx;
            }
            finally
            {
                _plcLock.Release();
            }
        }

        #endregion

        public async Task<bool> ConnectAsync()
        {
            await _plcLock.WaitAsync();
            try
            {
                var re = await Task.Run(() => client.Open());
                if (re.IsSucceed)
                {
                    SetConnectionState(true);
                    StartHeartbeat();
                    return true;
                }
                SetConnectionState(false);
                Console.WriteLine(re.Err);
                return false;
            }
            finally
            {
                _plcLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            StopHeartbeat();
            await _plcLock.WaitAsync();
            try
            {
                var re = await Task.Run(() => client.Close());
                SetConnectionState(false);
                if (!re.IsSucceed)
                {
                    throw re.Exception ?? new SystemException(re.Err);
                }
            }
            finally
            {
                _plcLock.Release();
            }
        }

        public void Dispose()
        {
            StopHeartbeat();
            client.Close();
            _plcLock.Dispose();
        }

        #region 心跳维持 (仅作为闲置时的保活补充)

        private void StartHeartbeat()
        {
            StopHeartbeat();
            if (!Heartbeat.Enabled) return;

            _heartbeatCts = new CancellationTokenSource();
            _ = Task.Run(() => HeartbeatTask(_heartbeatCts.Token));
        }

        private void StopHeartbeat()
        {
            if (_heartbeatCts != null)
            {
                _heartbeatCts.Cancel();
                _heartbeatCts.Dispose();
                _heartbeatCts = null;
            }
        }

        private async Task HeartbeatTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (Heartbeat.Enabled && Heartbeat.WriteFunc != null)
                {
                    try
                    {
                        // 执行心跳逻辑
                        await Heartbeat.WriteFunc();

                        // 如果没抛异常，说明连接正常
                        SetConnectionState(true);
                    }
                    catch (Exception ex)
                    {
                        // 心跳发生异常，以底层 client.Connected 的真实状态为准！
                        SetConnectionState(client.Connected);

                        if (client.Connected)
                        {
                            Console.WriteLine($"[Heartbeat Warning] 网络未断开，但用户自定义心跳逻辑发生异常: {ex.Message}");
                        }
                    }
                }

                try
                {
                    await Task.Delay(Heartbeat.IntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        #endregion

        #region 核心读写操作

        public async Task<T> ReadAsync<T>(string address)
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            object result = await ReadAsync(address, typeCode);
            return (T)result;
        }

        public Task<object> ReadAsync(string address, TypeCode typeCode)
        {
            // 通过统一包装器执行，自带上锁、切线程、查断线功能！
            return ExecuteWithConnectionCheckAsync<object>(() =>
            {
                return typeCode switch
                {
                    TypeCode.Boolean => ResultToVal(client.ReadBoolean(address)),
                    TypeCode.Byte => ResultToVal(client.ReadByte(address)),
                    TypeCode.Int16 => ResultToVal(client.ReadInt16(address)),
                    TypeCode.UInt16 => ResultToVal(client.ReadUInt16(address)),
                    TypeCode.Int32 => ResultToVal(client.ReadInt32(address)),
                    TypeCode.UInt32 => ResultToVal(client.ReadUInt32(address)),
                    TypeCode.Int64 => ResultToVal(client.ReadInt64(address)),
                    TypeCode.UInt64 => ResultToVal(client.ReadUInt64(address)),
                    TypeCode.Single => ResultToVal(client.ReadFloat(address)),
                    TypeCode.Double => ResultToVal(client.ReadDouble(address)),
                    TypeCode.Decimal => ResultToVal(client.ReadDouble(address)),
                    TypeCode.Char => ResultToVal(client.ReadString(address)),
                    TypeCode.String => ResultToVal(client.ReadString(address)),
                    _ => throw new NotSupportedException($"不支持的 TypeCode 类型：{typeCode}")
                };
            });
        }

        public Task<bool> WriteAsync<T>(string address, T value)
        {
            if (value is IConvertible convertibleVal)
            {
                return WriteAsync(address, convertibleVal);
            }
            throw new NotSupportedException($"不支持的写入类型：{typeof(T).FullName}");
        }

        public Task<bool> WriteAsync(string address, IConvertible value)
        {
            return ExecuteWithConnectionCheckAsync(() =>
            {
                return value switch
                {
                    bool boolVal => ResultTo(client.Write(address, boolVal)),
                    byte byteVal => ResultTo(client.Write(address, byteVal)),
                    sbyte sbyteVal => ResultTo(client.Write(address, sbyteVal)),
                    short shortVal => ResultTo(client.Write(address, shortVal)),
                    ushort ushortVal => ResultTo(client.Write(address, ushortVal)),
                    int intVal => ResultTo(client.Write(address, intVal)),
                    uint uintVal => ResultTo(client.Write(address, uintVal)),
                    long longVal => ResultTo(client.Write(address, longVal)),
                    ulong ulongVal => ResultTo(client.Write(address, ulongVal)),
                    float floatVal => ResultTo(client.Write(address, floatVal)),
                    double doubleVal => ResultTo(client.Write(address, doubleVal)),
                    decimal decimalVal => ResultTo(client.Write(address, (double)decimalVal)),
                    char charVal => ResultTo(client.Write(address, charVal.ToString())),
                    string strVal => ResultTo(client.Write(address, strVal)),
                    _ => throw new NotSupportedException($"不支持的写入类型：{value.GetType().FullName}")
                };
            });
        }

        public Task<IDictionary<string, object>> ReadBatchAsync(IDictionary<string, TypeCode> dict)
        {
            return ExecuteWithConnectionCheckAsync(() =>
            {
                var mappedDict = dict.ToDictionary(k => k.Key, v => ToDataTypeEnum(v.Value));
                var re = client.BatchRead(mappedDict);

                if (re.IsSucceed)
                {
                    foreach (var kv in dict)
                    {
                        if (kv.Value != TypeCode.Boolean) continue;
                        var bitRe = re.Value[kv.Key];
                        if (bitRe is bool) continue;
                        re.Value[kv.Key] = "1".Equals(bitRe?.ToString());
                    }
                    return (IDictionary<string, object>)re.Value;
                }
                throw re.Exception ?? new SystemException(re.Err);
            });
        }

        public Task<bool> WriteBatchAsync(IDictionary<string, object> dict, int batchNumber = 10)
        {
            return ExecuteWithConnectionCheckAsync(() =>
            {
                var re = client.BatchWrite(dict.ToDictionary(k => k.Key, v => v.Value), batchNumber);
                return ResultTo(re);
            });
        }

        public Task<T[]> ReadBatchAsync<T>(string addresse, int len)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 辅助转换方法

        private T ResultToVal<T>(IoTClient.Result<T> result)
        {
            if (result.IsSucceed) return result.Value;
            throw result.Exception ?? new SystemException(result.Err);
        }

        private bool ResultTo(IoTClient.Result result)
        {
            if (result.IsSucceed) return true;
            throw result.Exception ?? new SystemException(result.Err);
        }

        public static DataTypeEnum ToDataTypeEnum<T>() => ToDataTypeEnum(Type.GetTypeCode(typeof(T)));

        public static DataTypeEnum ToDataTypeEnum(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.Empty => DataTypeEnum.None,
                TypeCode.Boolean => DataTypeEnum.Bool,
                TypeCode.Byte => DataTypeEnum.Byte,
                TypeCode.Int16 => DataTypeEnum.Int16,
                TypeCode.UInt16 => DataTypeEnum.UInt16,
                TypeCode.Int32 => DataTypeEnum.Int32,
                TypeCode.UInt32 => DataTypeEnum.UInt32,
                TypeCode.Int64 => DataTypeEnum.Int64,
                TypeCode.UInt64 => DataTypeEnum.UInt64,
                TypeCode.Single => DataTypeEnum.Float,
                TypeCode.Double => DataTypeEnum.Double,
                TypeCode.String => DataTypeEnum.String,
                _ => DataTypeEnum.None
            };
        }

        #endregion
    }
}