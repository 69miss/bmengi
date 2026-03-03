using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static FreeSql.Internal.GlobalFilter;

namespace DoboEngineer.Pump;

// 主窗口逻辑
public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<PumpViewModel> Pumps { get; } = new();
    PumpCmd cmd;
    [ObservableProperty] private bool _isAutoMode;
    [ObservableProperty] private string _systemTime = string.Empty;

    public MainWindowViewModel()
    {
        Mock();    
    }
    public async Task Init()
    {
        var Items = new List<IDataItemBase>
        {
            CreateItem(40001, "心跳", false),
            new PumpStatus(),
            new PumpCtl(),
            new PumpCtlMode()
        };
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
        cmd = new PumpCmd(Items.ToArray());
        await cmd.Connect();
        cmd.Items.ItemPropertyChanged += Items_ItemPropertyChanged;
    }


    private IDataItemBase CreateItem(ushort addr, string name, bool canWrite, string remark = "", byte? fmtRadix = null)
    {
        return new DataItemBase()
        {
            Address = addr,
            Name = name,
            CanWrite = canWrite,
        };
    }
    private void Items_ItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not IDataItemBase item)
            return;
        switch (item.Address)
        {
            case 40002:
                break;
            default:
                break;
        }
    }

    private void Mock()
    {
        for (int i = 1; i <= 4; i++)
        {
            var p = new PumpViewModel(i);
            if (i == 2) p.IsRunning = true;
            if (i == 4) p.IsRemote = false;
            Pumps.Add(p);
        }

        UpdatePermissions();

        // 模拟数据循环
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (s, e) => SimLoop();
        timer.Start();

        var tTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        tTimer.Tick += (s, e) => SystemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        tTimer.Start();
    }

    partial void OnIsAutoModeChanged(bool value) => UpdatePermissions();

    private void UpdatePermissions()
    {
        foreach (var p in Pumps)
        {
            p.CanEditFlow = IsAutoMode && p.IsRemote;
            p.CanEditParam = !IsAutoMode && p.IsRemote;
        }
    }

    private void SimLoop()
    {
        var rnd = new Random();
        foreach (var p in Pumps)
        {
            // 持续刷新权限
            p.CanEditFlow = IsAutoMode && p.IsRemote;
            p.CanEditParam = !IsAutoMode && p.IsRemote;

            if (p.IsRunning)
            {
                double targetFlow = IsAutoMode ? p.FlowSV : (p.StrokeSV * p.FreqSV / 100.0);
                // 模拟液位缓慢升降
                if (p.FlowPV < targetFlow) p.FlowPV += 1.5;
                else if (p.FlowPV > targetFlow) p.FlowPV -= 1.5;

                // 添加轻微波动，让液面看起来在动
                p.FlowPV += (rnd.NextDouble() - 0.5) * 0.5;

                // 简单的参数反馈
                p.StrokePV = p.StrokeSV;
                p.FreqPV = p.FreqSV;
            }
            else
            {
                if (p.FlowPV > 0) p.FlowPV -= 2.0; // 停机后液位下降
                if (p.FlowPV < 0) p.FlowPV = 0;
            }
        }
    }
}