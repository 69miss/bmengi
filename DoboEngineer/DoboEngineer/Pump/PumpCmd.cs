using Avalonia.Threading;
using Dobo.Appl.Device;
using Dobo.Appl.HunterCmd;
using FluentModbus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FreeSql.Internal.GlobalFilter;

namespace DoboEngineer.Pump;
internal class PumpCmd
{
    IProtocolAdapter client ;
    private CancellationTokenSource cts;
    public  ObservableCollection<DataItem> Items { get; } = new();
    ushort minAddr;
    ushort maxAddr;
    public PumpCmd() {
        Init();
        client = new PumpProtocolAdapter();     
    }
    protected virtual void Init()
    {
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
        minAddr = Items.Min(x => x.Address);
        maxAddr = Items.Max(x => x.Address);
    }
    private void Connect()
    {
        try
        {
            if (client.IsConnected) { Disconnect(); return; }

            client= new PumpProtocolAdapter(isBigEndian : true) { };

            cts = new CancellationTokenSource();
            _ = PollingLoop(cts.Token);
        }
        catch (Exception ex)
        {
            //StatusMsg = $"连接失败: {ex.Message}";
        }
    }

    public void Disconnect()
    {
        cts?.Cancel();
        client.DisconnectAsync();
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
    private async Task PollingLoop(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        int num = 0;
        while (await timer.WaitForNextTickAsync(token))
        {
            if (!client.IsConnected || Items.Count == 0) continue;

            try
            {
                int count = maxAddr - minAddr + 1;
                if (count > 120) count = 120;
                int startReadAddr = minAddr - 40001;
                var arr = new string[count];
                arr[0] = startReadAddr.ToString();
                var rawBytes = (await client.ReadBatchAsync<short>(arr)).Values.ToArray();
                foreach (var item in Items)
                {
                    // 计算该项在读取数组中的索引
                    int index = item.Address - minAddr;

                    // 越界检查 (防止读取长度被截断后访问越界)
                    if (index >= 0 && index < rawBytes.Length)
                    {
                        item.Value = rawBytes[index];
                    }
                }
                var msg = (short)(num++ % 2);
                await client.WriteAsync("40001", msg);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                //AddLog(ex + "");
            }
        }
        var StatusMsg = $"轮询停止";
    }
}

 

