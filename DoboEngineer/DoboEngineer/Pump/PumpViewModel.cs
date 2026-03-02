using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace DoboEngineer.Pump;

public partial class PumpViewModel : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = string.Empty;

    // --- 设定值 (SV) ---
    [ObservableProperty] private double _flowSV;
    [ObservableProperty] private double _strokeSV;
    [ObservableProperty] private double _freqSV;

    // --- 实际反馈 (PV) ---
    [ObservableProperty][NotifyPropertyChangedFor(nameof(LiquidHeight))] private double _flowPV;
    [ObservableProperty] private double _strokePV;
    [ObservableProperty] private double _freqPV;

    // --- 状态 ---
    [ObservableProperty][NotifyPropertyChangedFor(nameof(StatusColor))][NotifyPropertyChangedFor(nameof(RunBtnText))][NotifyPropertyChangedFor(nameof(RunBtnColor))] private bool _isRunning;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(RemoteText))][NotifyPropertyChangedFor(nameof(RemoteColor))] private bool _isRemote;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFaulty))] private bool _isFault;

    // --- 权限 ---
    [ObservableProperty] private bool _canEditFlow;
    [ObservableProperty] private bool _canEditParam;

    // --- 辅助属性 ---
    // 1. 液位球高度计算 (假设球体总高度 120px，最大流量 100 L/h)
    // 限制在 0-120 之间
    public double LiquidHeight => Math.Clamp((FlowPV / 100.0) * 120.0, 0, 120);

    // 2. 状态显示优化
    public string RemoteText => IsRemote ? "远程 (Remote)" : "就地 (Local)";
    public IBrush RemoteColor => IsRemote ? SolidColorBrush.Parse("#3b82f6") : SolidColorBrush.Parse("#f97316"); // 蓝/橙
    public IBrush StatusColor => IsRunning ? SolidColorBrush.Parse("#22c55e") : SolidColorBrush.Parse("#cbd5e1"); // 绿/灰

    public string RunBtnText => IsRunning ? "停止运行" : "启动运行";
    public IBrush RunBtnColor => IsRunning ? SolidColorBrush.Parse("#fee2e2") : SolidColorBrush.Parse("#dcfce7"); // 浅红背景/浅绿背景
    public IBrush RunBtnFg => IsRunning ? SolidColorBrush.Parse("#ef4444") : SolidColorBrush.Parse("#16a34a");   // 深红字/深绿字

    public bool IsFaulty => IsFault;

    public PumpViewModel(int id)
    {
        Id = id;
        Name = $"{id}# 泵";
        FlowSV = 50; StrokeSV = 45; FreqSV = 30;
        IsRemote = true;
    }

    [RelayCommand]
    public void ToggleRun()
    {
        if (IsRemote) IsRunning = !IsRunning;
    }

    [RelayCommand]
    public void AdjustValue(string args)
    {
        var parts = args.Split('|');
        string target = parts[0];
        double amount = double.Parse(parts[1]);

        if (target == "Flow" && CanEditFlow) FlowSV = Math.Clamp(FlowSV + amount, 0, 100);
        else if (target == "Stroke" && CanEditParam) StrokeSV = Math.Clamp(StrokeSV + amount, 0, 100);
        else if (target == "Freq" && CanEditParam) FreqSV = Math.Clamp(FreqSV + amount, 0, 100);
    }
}
