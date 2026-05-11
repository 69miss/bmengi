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
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2
{
    internal class SiemensS7Adapter : IProtocolAdapter0
    {
        private SiemensClient client;

        public   SiemensS7Adapter(SiemensVersion type, string ip, int port= 102, byte slot = 1, byte rack = 0, int timeout = 1500) {
            client = new SiemensClient(type, ip, port, slot, rack);
        }
        public string ConnectionString => throw new NotImplementedException();

        public bool IsConnected => client.Connected;

        public string ProtocolName => "SiemensS7";

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<Tuple<string, object>> DataReceived;

        public Task<bool> ConnectAsync()
        {
            var re = client.Open();
            if(re.IsSucceed)
            return Task.FromResult(true);
            Console.WriteLine(re.Err);
            return Task.FromResult(false);
        }

        public Task DisconnectAsync()
        {
             
           var re= client.Close();
            if(re.IsSucceed)
            return Task.CompletedTask;
            throw re.Exception ?? new SystemException(re.Err);
        }

        public void Dispose()
        {
            client.Close();
        }

        public Task<T> ReadAsync<T>(string address)
        {
            object result = typeof(T) switch
            {
                _ when typeof(T) == typeof(bool) => ResultToVal(client.ReadBoolean(address)),

                _ when typeof(T) == typeof(byte) => ResultToVal(client.ReadByte(address)),
                // _ when typeof(T) == typeof(sbyte) => ResultToVal(client.ReadSByte(address)),

                _ when typeof(T) == typeof(short) => ResultToVal(client.ReadInt16(address)),
                _ when typeof(T) == typeof(ushort) => ResultToVal(client.ReadUInt16(address)),

                _ when typeof(T) == typeof(int) => ResultToVal(client.ReadInt32(address)),
                _ when typeof(T) == typeof(uint) => ResultToVal(client.ReadUInt32(address)),

                _ when typeof(T) == typeof(long) => ResultToVal(client.ReadInt64(address)),
                _ when typeof(T) == typeof(ulong) => ResultToVal(client.ReadUInt64(address)),

                _ when typeof(T) == typeof(float) => ResultToVal(client.ReadFloat(address)),
                _ when typeof(T) == typeof(double) => ResultToVal(client.ReadDouble(address)),
                _ when typeof(T) == typeof(decimal) => ResultToVal(client.ReadDouble(address)),

                // 字符/字符串
                _ when typeof(T) == typeof(char) => ResultToVal(client.ReadString(address)),
                _ when typeof(T) == typeof(string) => ResultToVal(client.ReadString(address)),

                // 日期时间
                //_ when typeof(T) == typeof(DateTime) => ResultToVal(client.ReadDateTime(address)),

                // 不支持的类型抛出异常
                _ => throw new NotSupportedException($"不支持的 IConvertible 类型：{typeof(T).FullName}")
            };
            return Task.FromResult((T)result);
        }
        T ResultToVal<T>(IoTClient.Result<T> result) {
            if (result.IsSucceed)
                return result.Value;
            throw result.Exception ?? new SystemException(result.Err);
        }
        bool ResultTo(IoTClient.Result result)
        {
            if (result.IsSucceed)
                return true;
            throw result.Exception ?? new SystemException(result.Err);
        }
        //public async Task<IDictionary<string, T>> ReadBatchAsync<T>(IEnumerable<string> addresses)
        //{
        //    var dict = addresses.Select(p => KeyValuePair.Create(p, Type.GetTypeCode(typeof(T)))).ToDictionary();
        //    var re = await ReadBatchAsync(dict);
        //    return re.Select(p => KeyValuePair.Create(p.Key, (T)p.Value)).ToDictionary();
        //}

        public Task<IDictionary<string, object>> ReadBatchAsync(IDictionary<string, TypeCode> dict)
        {
            var dict2 = dict.Select(p => KeyValuePair.Create(p.Key, ToDataTypeEnum(p.Value))).ToDictionary();
            var re = client.BatchRead(dict2.ToDictionary());

            if (re.IsSucceed)
            {
                foreach (var kv in dict) {
                    if (kv.Value != TypeCode.Boolean)
                        continue;
                    var bitRe = re.Value[kv.Key];
                    if (bitRe is bool)
                        continue;
                    re.Value[kv.Key]="1".Equals(bitRe+"");
                }
                return Task.FromResult((IDictionary<string, object>)re.Value);
            }
            throw re.Exception ?? new SystemException(re.Err);
        }

        public Task<bool> WriteAsync<T>(string address, T value)
        {
            //if(value is bool boolVal)
            //    ResultTo(client.Write(address, boolVal));
            bool success = value switch
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
                //DateTime timeVal => ResultTo(client.Write(address, timeVal,DataTypeEnum.UInt64)),
                // 不支持的类型
                _ => throw new NotSupportedException($"不支持的写入类型：{typeof(T).FullName}")
            };
            return Task.FromResult(success);
        }


        public static TypeCode ToTypeCode(  DataTypeEnum dataType)
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
                DataTypeEnum.Float => TypeCode.Single, // 核心：Float→Single
                DataTypeEnum.Double => TypeCode.Double,
                DataTypeEnum.String => TypeCode.String,
                _ => TypeCode.Empty
            };
        }
        public static DataTypeEnum ToDataTypeEnum<T>() { 
        return ToDataTypeEnum(Type.GetTypeCode(typeof(T)));
        }
        /// <summary>
        /// 系统TypeCode → 自定义枚举
        /// </summary>
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
                TypeCode.Single => DataTypeEnum.Float, // 核心：Single→Float
                TypeCode.Double => DataTypeEnum.Double,
                TypeCode.String => DataTypeEnum.String,
                _ => DataTypeEnum.None
            };
        }

        public Task<object> ReadAsync(string address, TypeCode typeCode)
        {
            object result = typeCode switch
            {
                // 布尔型
                TypeCode.Boolean => ResultToVal(client.ReadBoolean(address)),

                // 字节型
                TypeCode.Byte => ResultToVal(client.ReadByte(address)),
                // TypeCode.SByte => ResultToVal(client.ReadSByte(address)),

                // 16位整数
                TypeCode.Int16 => ResultToVal(client.ReadInt16(address)),
                TypeCode.UInt16 => ResultToVal(client.ReadUInt16(address)),

                // 32位整数
                TypeCode.Int32 => ResultToVal(client.ReadInt32(address)),
                TypeCode.UInt32 => ResultToVal(client.ReadUInt32(address)),

                // 64位整数
                TypeCode.Int64 => ResultToVal(client.ReadInt64(address)),
                TypeCode.UInt64 => ResultToVal(client.ReadUInt64(address)),

                // 浮点/decimal型
                TypeCode.Single => ResultToVal(client.ReadFloat(address)),
                TypeCode.Double => ResultToVal(client.ReadDouble(address)),
                TypeCode.Decimal => ResultToVal(client.ReadDouble(address)),

                // 字符/字符串
                TypeCode.Char => ResultToVal(client.ReadString(address)),
                TypeCode.String => ResultToVal(client.ReadString(address)),

                // 日期时间
                // TypeCode.DateTime => ResultToVal(client.ReadDateTime(address)),

                // 不支持的类型抛出异常
                _ => throw new NotSupportedException($"不支持的 TypeCode 类型：{typeCode}")
            };
            return Task.FromResult(result);
        }

        public Task<bool> WriteAsync(string address, IConvertible value)
        {
            if (address.EndsWith("DBW2"))
            { 
            
            }
            bool success = value switch
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
                //DateTime timeVal => ResultTo(client.Write(address, timeVal,DataTypeEnum.UInt64)),
                // 不支持的类型
                _ => throw new NotSupportedException($"不支持的写入类型：{value.GetType().FullName}")
            };
            return Task.FromResult(success);
        }

        public Task<T[]> ReadBatchAsync<T>(string addresse, int len)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteBatchAsync(IDictionary<string, object> dict, int batchNumber=10)
        {
           var re= client.BatchWrite(dict.ToDictionary(), batchNumber);
            return Task.FromResult(ResultTo(re));
        }
    }
}
