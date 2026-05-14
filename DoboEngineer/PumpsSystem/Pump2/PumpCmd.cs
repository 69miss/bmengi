using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Dobo.Appl.Device;
using Dobo.Appl.HunterCmd;
using Dobo.Appl.Utility;
using FluentModbus;
using IoTClient.Common.Enums;
using PumpsSystem.Module;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FreeSql.Internal.GlobalFilter;

namespace PumpsSystem.Pump2;
internal class PumpCmd:IDisposable
{
    IProtocolAdapter0 client ;
    private CancellationTokenSource cts;
    public  ObservableItemCollection<IDataItemProp> Items { get; } = new();
    Dictionary<string,TypeCode> readBatchAddrs;

    public PumpCmd(IDataItemProp[] items )
    {
        foreach (IDataItemProp item in items)
        {
            Items.Add(item);
        }
        readBatchAddrs = items.ToDictionary(p => p.Address, p => p.TypeCode);
    }
  
    public async Task Connect()
    {
        await ReConnect();
        cts = new CancellationTokenSource();
        await StatueInfoGet(cts.Token,false);
        _ = PollingLoop(cts.Token);
    }

    private async Task ReConnect()
    {
        if (client != null)
        {
            await Disconnect();
            client.Dispose();
        }
        client = new SiemensS7Adapter(SiemensVersion.S7_1200, "192.168.0.8", 102, timeout: 3000) { };
        var re = await client.ConnectAsync();
        if (!re)
        {
            WeakReferenceMessenger.Default.Send("网络异常，重连失败", BusEventName.Main_ShowNotification);
            throw new System.Net.Sockets.SocketException(-1, "连接失败");
        }
    }

    public async Task Disconnect()
    {
        cts?.Cancel();
       await client.DisconnectAsync();
       // StatusMsg = "已断开";
    }
    DateTime DBW2LastTime;
    public async Task WriteValue(string address, IConvertible val)
    {
        if (client.IsConnected)
        {
            Console.WriteLine($"{DateTime.Now}-->>{address}_{val}");
            if (address.EndsWith("DBW2"))
            {
                //Console.WriteLine(Environment.StackTrace);
                DBW2LastTime = DateTime.Now;
            }
           var re= await client.WriteAsync(address + "", val);
            Console.WriteLine($"{DateTime.Now}--<<re:{re},{address}_{val}");
        }
    }
    public async Task ReconnectionAsync(CancellationToken token, int msDelay, Func<Exception?, int, IDictionary<string, object>, Task<bool>> reconnectFunc)
    {
        var i = 0;
        var context = new Dictionary<string, object>();
        Exception exception = null;
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (await reconnectFunc(exception, i++, context))
                    return;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            await Task.Delay(msDelay, token);
        }
    }
    public bool IsConnection { get => client.IsConnected; }
    private async Task PollingLoop(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000));
        bool num = false;
        while (await timer.WaitForNextTickAsync(token))
        {
            if (!client.IsConnected || Items.Count == 0) continue;
            try
            {
                await StatueInfoGet(token);
                
                await client.WriteAsync("DB12.DBX0.0", num=!num);
            }
            catch (Exception ex)
            {
                WriteLog(ex + "");
            }
        }
        var StatusMsg = $"轮询停止";
    }

    /// <summary>
    /// 状态获取，如果不重试，失败则异常
    /// </summary>
    /// <param name="token"></param>
    /// <param name="isRetry"></param>
    /// <returns></returns>
    private async Task StatueInfoGet(CancellationToken token, bool isRetry = true)
    {

        IDictionary<string, object> reDic = null;
        try
        {
            reDic = await client.ReadBatchAsync(readBatchAddrs);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            if (!isRetry)
                throw;
            await ReconnectionAsync(token, 1000, async (p1, p2, p3) =>
            {
                if (p2 == 0)
                    reDic = await client.ReadBatchAsync(readBatchAddrs);
                if (p2 > 0)
                    await ReConnect();
                reDic = await client.ReadBatchAsync(readBatchAddrs);
                return true;
            });
        }
        var newDict = reDic.ToDictionary(p => p.Key, p => (IConvertible)p.Value);
        var rawBytes = reDic.Values.ToArray();
        var beginTIme = DateTime.Now;
        var isChange = false;
        var strMsg = "";
        foreach (var item in Items)
        {
            if (!newDict.TryGetValue(item.Address, out var val))
                continue;
            if (!val.Equals(item.Value))
            {
                //if (!item.Name.Contains("心跳"))
                //    strMsg += $"{item.Name}:{item.Value}-->{val} ; ";
                
                //可不用这样处理
                if (item.Address.EndsWith("DBW2")&&DateTime.Now-DBW2LastTime<TimeSpan.FromSeconds(1))
                {
                    strMsg += "DW2变更忽略";
                    continue;
                }
                item.Value = val;
                isChange = true;
            }
            //item.Value = val;
        }
        if (isChange == true)
        {
            if (!string.IsNullOrWhiteSpace(strMsg))
                WriteLog(strMsg);
            Items.OnItemsPropertyChangedEnd(beginTIme);
        }
    }

    void WriteLog(string msg) {
        Console.WriteLine($"{DateTime.Now} [{nameof(PumpCmd)}.PollingLoop] {msg}");
    }
 
    public async void Dispose()
    {
        await Disconnect();
        client?.Dispose();
    }
}

 

