using Avalonia.Threading;
using Dobo.Appl.Device;
using Dobo.Appl.HunterCmd;
using Dobo.Appl.Utility;
using FluentModbus;
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

namespace DoboEngineer.Pump;
internal class PumpCmd:IDisposable
{
    IProtocolAdapter client ;
    private CancellationTokenSource cts;
    public  ObservableItemCollection<IDataItemBase> Items { get; } = new();
    ushort minAddr;
    ushort maxAddr;
    ushort modbusBeginIndex = 40001;
    string[] readBatchAddrs;
    public PumpCmd(IDataItemBase[] items = null)
    {
        Init(items);
        minAddr = Items.Min(x => x.Address);
        maxAddr = Items.Max(x => x.Address);
    }
    protected virtual void Init(IDataItemBase[] items = null)
    {

        if (items != null) {
            foreach (IDataItemBase item in items)
            {
                Items.Add(item);
            }
            return;
        }
        Items.Add(CreateItem(40001, "心跳", false));
        Items.Add(CreateItem(40002, "状态反馈", false, "", 2));
        Items.Add(CreateItem(40003, "控制字", true, "", 2));
        Items.Add(CreateItem(40004, "控制模式", true, "", 2));
        for (ushort i = 40005; i <= 40032;)
        {
            var pNum = (i - 40005) / 7 + 1;
            // 模拟 泵1 (地址 40008-40011)
            Items.Add(CreateItem(i++, $"泵{pNum}-频率", false, "Hz"));
            Items.Add(CreateItem(i++, $"泵{pNum}-冲程", false, "%"));
            Items.Add(CreateItem(i++, $"泵{pNum}-流量", false, "L/min"));
            //
            Items.Add(CreateItem(i++, $"泵{pNum}-最大流量", true, "L/min"));
            Items.Add(CreateItem(i++, $"泵{pNum}-频率设定", true, "Hz"));
            Items.Add(CreateItem(i++, $"泵{pNum}-冲程设定", true, "%"));
            Items.Add(CreateItem(i++, $"泵{pNum}-流量设定", true, "L/min"));
        }
       
    }
    public async Task Connect()
    {
        await ReConnect();
        cts = new CancellationTokenSource();
        //
        int count = maxAddr - minAddr + 1;
        if (count > 120) count = 120;
        ushort startReadAddr = (ushort)(minAddr - modbusBeginIndex);
        readBatchAddrs = new string[count];
        readBatchAddrs[0] = startReadAddr.ToString();
        await StatueInfoGet(cts.Token,false);
        _ = PollingLoop(cts.Token);
    }

    private async Task ReConnect()
    {
        if (client != null) { client.Dispose(); }
        client = new PumpProtocolAdapter(isBigEndian: true) { };
        var re = await client.ConnectAsync();
        if (!re)
            throw new System.Net.Sockets.SocketException(-1, "连接失败");
    }

    public async Task Disconnect()
    {
        cts?.Cancel();
       await client.DisconnectAsync();
       // StatusMsg = "已断开";
    }
    private DataItem CreateItem(ushort addr, string name, bool canWrite, string remark = "", byte? fmtRadix = null)
    {
        return new DataItem(fmtRadix)
        {
            Address = addr,
            Name = name,
            CanWrite = canWrite,
            Remark = remark,
        };
    }
    public async Task WriteValue(int address, short val)
    {
        if (client.IsConnected)
            await client.WriteAsync(address + "", val);
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
        int num = 0;
        while (await timer.WaitForNextTickAsync(token))
        {
            if (!client.IsConnected || Items.Count == 0) continue;
            try
            {
                await StatueInfoGet(token);
                var msg = (short)(num++ % 2);
                await client.WriteAsync("40001", msg);
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
    private async Task<IDictionary<string, short>> StatueInfoGet(CancellationToken token,bool isRetry=true)
    {
        string[] arr = readBatchAddrs;
        IDictionary<string, short> reDic = null;
        try
        {
            reDic = await client.ReadBatchAsync<short>(arr);
        }
        catch (Exception)
        {
            if(!isRetry)
                throw;
            await ReconnectionAsync(token, 1000, async (p1, p2, p3) =>
            {
                if (p2 == 0)
                    reDic = await client.ReadBatchAsync<short>(arr);
                if (p2 > 0)
                    await ReConnect();
                reDic = await client.ReadBatchAsync<short>(arr);
                return true;
            });
        }

        var rawBytes = reDic.Values.ToArray();
        var beginTIme = DateTime.Now;
        var isChange = false;
        var strMsg = "";
        foreach (var item in Items)
        {
            // 计算该项在读取数组中的索引
            int index = item.Address - minAddr;

            // 越界检查 (防止读取长度被截断后访问越界)
            if (index >= 0 && index < rawBytes.Length)
            {
                if (!rawBytes[index].Equals(item.Value))
                {
                    strMsg += $"{item.Name}:{item.Value}-->{rawBytes[index]}";
                    item.Value = rawBytes[index];
                    isChange = true;
                }
            }
        }
        if (isChange == true)
        {
            WriteLog(strMsg);
            Items.OnItemsPropertyChangedEnd(beginTIme);
        }
        return reDic;

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

 

