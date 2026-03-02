using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace DoboEngineer.Pump;

// 主窗口逻辑
public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<PumpViewModel> Pumps { get; } = new();
    [ObservableProperty] private bool _isAutoMode;
    [ObservableProperty] private string _systemTime = string.Empty;

    public MainWindowViewModel()
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