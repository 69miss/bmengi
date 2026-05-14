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
        public bool Enabled { get; set; } = false;      // 是否启用心跳
        public int IntervalMs { get; set; } = 1000;     // 心跳间隔 (毫秒)

        /// <summary>
        /// 用户自定义的心跳实际写入逻辑。
        /// 外部可在此委托中自由实现对指定地址、类型、变化方式的写入。
        /// 返回 true 表示维持成功，返回 false 或抛出异常表示连接断开。
        /// </summary>
        public Func<Task<bool>> WriteFunc { get; set; }
    }

    internal class SiemensS7Adapter : IProtocolAdapter0
    {
        private readonly SiemensClient client;
        private bool _isConnected;
        private CancellationTokenSource _heartbeatCts;

        // 引入异步锁，防止心跳后台线程与业务主线程并发读写底层Socket导致数据错乱或崩溃
        private readonly SemaphoreSlim _plcLock = new SemaphoreSlim(1, 1);

        // 对外提供的心跳配置对象
        public HeartbeatConfig Heartbeat { get; } = new HeartbeatConfig();

        public string ConnectionString => throw new NotImplementedException();

        // 综合当前内部状态或底层状态返回连接情况
        public bool IsConnected => Heartbeat.Enabled ? _isConnected : client.Connected;

        public string ProtocolName => "SiemensS7";

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<Tuple<string, object>> DataReceived;

        public SiemensS7Adapter(SiemensVersion type, string ip, int port = 102, byte slot = 1, byte rack = 0, int timeout = 1500)
        {
            client = new SiemensClient(type, ip, port, slot, rack);
        }

        /// <summary>
        /// 统一处理连接状态的变更，并触发事件
        /// </summary>
        private void SetConnectionState(bool newState)
        {
            if (_isConnected != newState)
            {
                _isConnected = newState;
                ConnectionStateChanged?.Invoke(this, _isConnected);
            }
        }

        public async Task<bool> ConnectAsync()
        {
            await _plcLock.WaitAsync();
            try
            {
                // 真正的异步封装，避免底层同步方法阻塞UI或调用线程
                var re = await Task.Run(() => client.Open());
                if (re.IsSucceed)
                {
                    SetConnectionState(true);
                    StartHeartbeat();
                    return true;
                }
                Console.WriteLine(re.Err);
                SetConnectionState(false);
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

        #region 纯粹的心跳维持与状态感知机制

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
                    bool success = false;
                    try
                    {
                        // 执行用户传入的自定义心跳写入逻辑
                        success = await Heartbeat.WriteFunc();
                    }
                    catch
                    {
                        success = false;
                    }
                    SetConnectionState(success);
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

        #region 核心读写操作 (代码去重复用、支持真异步与并发安全)

        public async Task<T> ReadAsync<T>(string address)
        {
            var typeCode = Type.GetTypeCode(typeof(T));
            object result = await ReadAsync(address, typeCode);
            return (T)result;
        }

        public async Task<object> ReadAsync(string address, TypeCode typeCode)
        {
            await _plcLock.WaitAsync();
            try
            {
                return await Task.Run<object>(() =>
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
            finally
            {
                _plcLock.Release();
            }
        }

        public Task<bool> WriteAsync<T>(string address, T value)
        {
            if (value is IConvertible convertibleVal)
            {
                return WriteAsync(address, convertibleVal);
            }
            throw new NotSupportedException($"不支持的写入类型：{typeof(T).FullName}");
        }

        public async Task<bool> WriteAsync(string address, IConvertible value)
        {
            await _plcLock.WaitAsync();
            try
            {
                return await Task.Run(() =>
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
            finally
            {
                _plcLock.Release();
            }
        }

        public async Task<IDictionary<string, object>> ReadBatchAsync(IDictionary<string, TypeCode> dict)
        {
            await _plcLock.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    // 简化冗余的LINQ转换
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
            finally
            {
                _plcLock.Release();
            }
        }

        public async Task<bool> WriteBatchAsync(IDictionary<string, object> dict, int batchNumber = 10)
        {
            await _plcLock.WaitAsync();
            try
            {
                return await Task.Run(() =>
                {
                    var re = client.BatchWrite(dict.ToDictionary(k => k.Key, v => v.Value), batchNumber);
                    return ResultTo(re);
                });
            }
            finally
            {
                _plcLock.Release();
            }
        }

        public Task<T[]> ReadBatchAsync<T>(string addresse, int len)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 辅助转换方法

        private T ResultToVal<T>(IoTClient.Result<T> result)
        {
            if (result.IsSucceed)
                return result.Value;
            throw result.Exception ?? new SystemException(result.Err);
        }

        private bool ResultTo(IoTClient.Result result)
        {
            if (result.IsSucceed)
                return true;
            throw result.Exception ?? new SystemException(result.Err);
        }

        public static TypeCode ToTypeCode(DataTypeEnum dataType)
        {
            return dataType switch
            {
                DataTypeEnum.None => TypeCode.Empty,
                DataTypeEnum.Bool => TypeCode.Boolean,
                DataTypeEnum.Byte => TypeCode.Byte,
                DataTypeEnum.Int16 => TypeCode.Int16,
                DataTypeEnum.UInt16 => TypeCode.UInt16,
                DataTypeEnum.Int32 => TypeCode.Int32,
                DataTypeEnum.UInt32 => TypeCode.UInt32,
                DataTypeEnum.Int64 => TypeCode.Int64,
                DataTypeEnum.UInt64 => TypeCode.UInt64,
                DataTypeEnum.Float => TypeCode.Single,
                DataTypeEnum.Double => TypeCode.Double,
                DataTypeEnum.String => TypeCode.String,
                _ => TypeCode.Empty
            };
        }

        public static DataTypeEnum ToDataTypeEnum<T>()
        {
            return ToDataTypeEnum(Type.GetTypeCode(typeof(T)));
        }

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