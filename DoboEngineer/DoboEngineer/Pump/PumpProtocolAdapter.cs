using Avalonia.Threading;
using Dobo.Appl.Device;
using FluentModbus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DoboEngineer.Pump
{
    public class PumpProtocolAdapter : IProtocolAdapter
    {
        public string ConnectionString {  get;private set; }

        public string ProtocolName => "FluentModbus";
        public bool IsBigEndian { get;private set; }=false;
        public bool IsConnected { get => client.IsConnected; }

        public event EventHandler<bool> ConnectionStateChanged;
        public event EventHandler<Tuple<string, object>> DataReceived;

        public Task<bool> ConnectAsync()
        {
            try
            { 
                AddLog($"正在连接 {ConnectionString} ...");
                client.Connect(ConnectionString,IsBigEndian?ModbusEndianness.BigEndian:ModbusEndianness.LittleEndian);
                AddLog("设备连接成功！");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                AddLog($"连接失败: {ex.Message}");
                throw;
            }
            return Task.FromResult(false);
        }

        public Task DisconnectAsync()
        {
            client.Disconnect();
            AddLog("已断开连接");
            return Task.CompletedTask;
        }

        public async Task<T> ReadAsync<T>(string address)
        {
            var begin = ushort.Parse(address);
            var index = ToFIndex(address);
            var tType = typeof(T);
            var arr = await ReadValuesAsync<T>(begin, 1);
            return arr[0];
        }


        public async Task<IDictionary<string, T>> ReadBatchAsync<T>(IEnumerable<string> addresses)
        {
            var begin = ushort.Parse(addresses.First());
            ushort index = ToFIndex(begin);
            var dic = new Dictionary<string, T>();
            var datas = await ReadValuesAsync<T>(begin, addresses.Count());
            for (int i = 0; i < datas.Length; i++)
            {
                dic.Add((begin + i).ToString(), datas[i]);
            }
            return dic;
        }

        private async Task<T[]> ReadValuesAsync<T>(int begin, int len)
        {
            var tType = typeof(T);
            Array datas = null;
            if (tType == typeof(byte))
            {
                datas = (await client.ReadHoldingRegistersAsync<byte>(0, begin, len)).ToArray();
            }
            else if (tType == typeof(short))
            {
                datas = (await client.ReadHoldingRegistersAsync<short>(0, begin, len)).ToArray();
            }
            else if (tType == typeof(ushort))
            {
                datas = (await client.ReadHoldingRegistersAsync<ushort>(0, begin, len)).ToArray();
            }
            else if (tType == typeof(float))
            {
                datas = (await client.ReadHoldingRegistersAsync<float>(0, begin, len)).ToArray();
            }
            else
                throw new ArgumentException("不支持的类型");
            var tArray = new T[datas.Length];
            for (int i = 0; i < datas.Length; i++)
            {
                tArray[i] = (T)datas.GetValue(i);
            }
            return tArray;
        }

        private static ushort ToFIndex(string address)
        {
            var begin = ushort.Parse(address);
            return ToFIndex(begin);
        }
        private static ushort ToFIndex(ushort begin)
        {
            var index = begin;
            if (begin > 40000)
                index -= 40001;
            return index;
        }

        public async Task<bool> WriteAsync<T>(string address, T value)
        {
            //todo 应处理线程安全
            var index = ToFIndex(address);
            if (value is short val)
                await client.WriteSingleRegisterAsync(0, index, val);
            else if (value is ushort val1)
                await client.WriteSingleRegisterAsync(0, index, val1);
            else
                throw new ArgumentException("不支持的类型");
            
            return true;
        }

        private ModbusTcpClient client=new ModbusTcpClient();

        public PumpProtocolAdapter(string connectionStr="192.168.0.8:502",bool isBigEndian=false)
        {
            ConnectionString = connectionStr;
            IsBigEndian = isBigEndian;
        }

        public void AddLog(string msg)
        {
            Console.WriteLine(DateTime.Now+" : "+msg);
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
