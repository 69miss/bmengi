using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentModbus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DoboEngineer.Pump
{
    public partial class CmdTestVm : ObservableObject
    {
        private ModbusTcpClient _client;
        private CancellationTokenSource _cts;
        ushort minAddr;
        ushort maxAddr;

        public CmdTestVm()
        {
            _client = new ModbusTcpClient();
            Items.Add(CreateItem(40001, "心跳", false));
            Items.Add(CreateItem(40002, "状态反馈", false,"",2));
            Items.Add(CreateItem(40003, "控制字", true,"",2));
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

        // 核心数据列表
        public ObservableCollection<DataItem> Items { get; } = new();

        [ObservableProperty] private string _ipAddress = "192.168.0.8";
        [ObservableProperty] ushort prot = 502;
        [ObservableProperty] private bool _isConnected;
        [ObservableProperty] private string _statusMsg = "等待连接";
        [ObservableProperty] string cmdRemark = "";

        // 辅助工厂方法
        private DataItem CreateItem(ushort addr, string name, bool canWrite, string remark = "",byte? fmtRadix=null)
        {
                return new DataItem(fmtRadix) // 传入写入回调
                {
                    Address = addr,
                    Name = name,
                    CanWrite = canWrite,
                    Remark = remark,
                };
        }

        [RelayCommand]
        private void Connect()
        {
            try
            {
                if (_isConnected) { Disconnect(); return; }

                _client.Connect(new IPEndPoint(IPAddress.Parse(IpAddress), Prot), ModbusEndianness.BigEndian);
                IsConnected = true;
                StatusMsg = "已连接";

                _cts = new CancellationTokenSource();
                _ = PollingLoop(_cts.Token);
            }
            catch (Exception ex)
            {
                StatusMsg = $"连接失败: {ex.Message}";
            }
        }

        private void Disconnect()
        {
            _cts?.Cancel();
            _client.Disconnect();
            IsConnected = false;
            StatusMsg = "已断开";
        }
        DateTime lastFlushTime;
        // --- 核心：智能轮询循环 ---
        private async Task PollingLoop(CancellationToken token)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

            while (await timer.WaitForNextTickAsync(token))
            {
                if (!_client.IsConnected || Items.Count == 0) continue;

                try
                {
                    // 1. 动态计算读取范围
                    // 找出列表中最小和最大的地址
                    //ushort minAddr = Items.Min(x => x.Address);
                    //ushort maxAddr = Items.Max(x => x.Address);

                    // 计算总长度 (注意：如果 min=1, max=1000, 会读1000个，这需要分包优化。
                    // 这里假设地址相对集中，做简单处理)
                    int count = maxAddr - minAddr + 1;

                    // Modbus 协议单次最大通常 125个字
                    // 实际工程中这里需要写一个分片算法，这里为了演示简单，假设不超过100
                    if (count > 120) count = 120; // 简单保护

                    // 2. 批量读取
                    // 注意：FluentModbus 地址偏移问题，通常 40001 对应 0
                    // 这里假设 ViewModel 里的 Address 就是 0-based index
                    // 如果你的Address是40001，这里可能要减去 40001 或 0
                    int startReadAddr = minAddr-40001;

                    var rawBytes = _client.ReadHoldingRegisters<short>(0, startReadAddr, count).ToArray();
                    lastFlushTime=DateTime.Now;
                    // 转为 short 数组
                    short[] registerValues = rawBytes;// new short[count];
                    //Buffer.BlockCopy(rawBytes, 0, registerValues, 0, rawBytes.Length);

                    // 3. 分发数据到 Items
                    Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var item in Items)
                        {
                            // 计算该项在读取数组中的索引
                            int index = item.Address - minAddr;

                            // 越界检查 (防止读取长度被截断后访问越界)
                            if (index >= 0 && index < registerValues.Length)
                            {
                                item.Value = registerValues[index];
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    AddLog(ex+"");
                }
            }
            StatusMsg = $"轮询停止";
        }

        // --- 回调：写入单个寄存器 ---
        [RelayCommand]
        public async Task Write(DataItem item)
        {
            // Task.CompletedTask;
            AddLog(item);
            
            WriteRegisterAsync(item.Address, item.EditVal.ToInt16(null));
        }
        private async Task WriteRegisterAsync(ushort address, short value)
        {
            if (!_client.IsConnected)
            {
                AddLog("未连接");
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    _client.WriteSingleRegister(0, address-40001, value);
                   
                    AddLog($"写入完成：" + address);
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => StatusMsg = $"写入错误: {ex.Message}");
                }
            });
        }
        void AddLog(object txt) {
            Console.WriteLine(DateTime.Now+"  "+txt  );
        }
    }
}
