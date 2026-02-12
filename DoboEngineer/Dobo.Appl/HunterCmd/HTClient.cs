using Dobo.Appl.Device;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dobo.Appl.HunterCmd;

public class HTClient : IProtocolAdapter
{
    SpectraTcpClient spectraTcpClient=new SpectraTcpClient();
    public string ConnectionString { get; private set; }

    public bool IsConnected => spectraTcpClient.IsConnect();

    public string ProtocolName => "HTSTC";

    public event EventHandler<bool> ConnectionStateChanged;
    public event EventHandler<Tuple<string, object>> DataReceived;

    public Task<bool> ConnectAsync()
    {
      //  spectraTcpClient.OnDataReceived = OnDataReceived;
       return spectraTcpClient.ConnectAsync();
    }
    public void OnDataReceived(Tuple<string, HunterPacket> data)
    {
        Console.WriteLine($"原始消息：{data.Item1}:{data.Item2.RawString}");
        DataReceived?.Invoke(this, Tuple.Create(data.Item1, (object)data.Item2));
    }
    public Task DisconnectAsync()
    {
        spectraTcpClient.Disconnect();
        return Task.CompletedTask;
    }
    public async Task<T> ReadAsync<T>(string address)
    {
        //var nextNum = spectraTcpClient.NextMsgNo();
        //var re =await spectraTcpClient.SendCommandAsync(address, msgNo: nextNum);
        //if (!re)
        //    throw new AggregateException("发送失败");
        //var reData =await spectraTcpClient.ReceiveResponseAsync(nextNum);

        var reData =spectraTcpClient.SendCommand(address);
        Console.WriteLine("读取：" + reData.CommandType + "" + reData.ResponseDataContent);

        if (reData.ResponseDataContent is T reT)
        {
            return (T)reT;// Task.FromResult(reT);
        }
        return (T)(object)reData.ResponseDataContent;
    }

    public Task<IDictionary<string, object>> ReadBatchAsync(IEnumerable<string> addresses)
    {
        throw new NotImplementedException();
    }

    public Task<bool> WriteAsync<T>(string address, T value)
    {
        throw new NotImplementedException();
    }
}