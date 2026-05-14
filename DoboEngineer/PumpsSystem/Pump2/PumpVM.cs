using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dobo.Appl.Utility;
using PumpsSystem.Module;
using PumpsSystem.Pump; // 引用原有接口
using PumpsSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Dobo.Appl.Utility.INotifyPropertyChangedExt;

namespace PumpsSystem.Pump2;

public partial class PumpVM : ViewModelBase, INotifyPropertyChangedExt2
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = string.Empty;

    private double _flowSV;
    private double _strokeSV;
    private double _freqSV; 
    [ObservableProperty][NotifyPropertyChangedFor(nameof(LiquidHeight))] private double _flowMax;
    [ObservableProperty] private double _strokeMax;
    [ObservableProperty] private double _strokeMin;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(LiquidHeight))] private double _flowPV;
    [ObservableProperty] private double _strokePV;
    [ObservableProperty] private double _freqPV;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(RunBtnText))]
    [NotifyPropertyChangedFor(nameof(RunBtnColor))]
    private bool _isRunning;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(RemoteText))][NotifyPropertyChangedFor(nameof(RemoteColor))] private bool _isRemote; [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFaulty))] private bool _isFault;

    [ObservableProperty] private bool _canEditFlow;
    [ObservableProperty] private bool _canEditParam;

    public short FreqMax { get; } = 50;
    public short FreqMin { get; } = 0;
     
    public double LiquidHeight => Math.Clamp((FlowPV / FlowMax) * 100.0, 0, 100);

    public string RemoteText => IsRemote ? L.RemoteStatusLabel : L.LocalStatusLabel;
    public IBrush RemoteColor => IsRemote ? SolidColorBrush.Parse("#3b82f6") : SolidColorBrush.Parse("#f97316");
    public IBrush StatusColor => IsRunning ? SolidColorBrush.Parse("#22c55e") : SolidColorBrush.Parse("#cbd5e1");

    public string RunBtnText => IsRunning ? L.StopRunningStatus : L.StartRunningStatus;
    public IBrush RunBtnColor => IsRunning ? SolidColorBrush.Parse("#fee2e2") : SolidColorBrush.Parse("#dcfce7");
    public IBrush RunBtnFg => IsRunning ? SolidColorBrush.Parse("#ef4444") : SolidColorBrush.Parse("#16a34a");

    [ObservableProperty] IBrush waveGaugeFg = SolidColorBrush.Parse("#0ea5e9");
    public bool IsFaulty => IsFault;

    bool isInited = false;
    bool isUpdatingFromPlc = false;
    Func<IDataItemProp, IConvertible, Task> EditValFun;

    // 【核心新增】：注入的结构化 Modbus 上下文
    public PumpItem ModbusCtx { get;  set; }

    public PumpModel Cfg { get; set; }
    public INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public double FlowSV { get => _flowSV; set => SetFieldAndMend(ref _flowSV, value,p=>Math.Clamp(p, Math.Round(FlowMax * 0.01, 1, MidpointRounding.ToPositiveInfinity),FlowMax)); }
    public double StrokeSV { get => _strokeSV; set => SetFieldAndMend(ref _strokeSV, value, p => Math.Clamp(p, 0, 100)); }
    public double FreqSV { get => _freqSV; set => SetFieldAndMend(ref _freqSV, value, p => Math.Clamp(p, 0, 100)); }

    public PumpVM(int id, Func<IDataItemProp, IConvertible, Task> fun = null) 
    {
        EditValFun = fun;
        Id = id;
        Name = $"{id}#";
        FlowSV = 0; StrokeSV = 0; FreqSV = 0;

    }

    public void InjectModbusContext(PumpItem ctx)
    {
        ModbusCtx = ctx;
        isInited = false;
    }

    [RelayCommand]
    public async void ToggleRun()
    {
        if (!IsRemote || ModbusCtx == null) return;

        // 使用 BitMap 计算反转后的值并下发
        //ushort targetRegisterVal = ModbusCtx.CtlRunning.CalculateNewValue(!IsRunning);
        try
        {
            await EditValFun?.Invoke(ModbusCtx.CtlRunning, !IsRunning);
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    [RelayCommand]
    public void AdjustValue(string args)
    {
        var parts = args.Split('|');
        string target = parts[0];
        double amount = double.Parse(parts[1]);

        if (target == "Flow" && CanEditFlow)
            FlowSV = Math.Clamp(FlowSV + amount, 0, FlowMax);
        else if (target == "Stroke" && CanEditParam)
            StrokeSV = Math.Clamp(StrokeSV + amount, 10, 100);
        else if (target == "Freq" && CanEditParam)
            FreqSV = Math.Clamp(FreqSV + amount, 10, 100);
    }

    private CancellationTokenSource? _debounceCts;
    string nowDebounceName;
    private void TriggerDebounceWrite(string name, params object[] args)
    {
        if (name == nowDebounceName)
        {
            _debounceCts?.Cancel();
            Console.WriteLine($"防抖取消执行：{name}{args[0]}");
        }
        nowDebounceName = name;
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (token.IsCancellationRequested) return;

                if (args[1] is IConvertible val)
                    await EditValFun?.Invoke((IDataItemProp)args[0], val.ToUInt16(null));
                else
                    throw new ArgumentException("无法处理的数据类型");
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"写入失败: {ex}");
            }
        });
    }
    public void SyncFromPlc()
    {
        if (ModbusCtx == null) return;

        isUpdatingFromPlc = true;
        try
        {
            IsRemote = ModbusCtx.IsRemote.Value;
            IsFault = ModbusCtx.IsFault.Value;
            IsRunning = ModbusCtx.IsRunning.Value;

            CanEditFlow = ModbusCtx.ModeFlow.Value && IsRemote;
            CanEditParam = ModbusCtx.ModeManual.Value && IsRemote;

            GetToInfo(this, false, ModbusCtx.FreqPV, ModbusCtx.StrokePV, ModbusCtx.FlowPV);

            if (!isInited)
            {
                FlowMax = GetShowValByRaw(nameof(FlowMax), ModbusCtx.MaxFlowSV);
                StrokeMax = ModbusCtx.MaxStroke.Value.ToInt16(null);
                StrokeMin = ModbusCtx.MinStroke.Value.ToInt16(null);

                GetToInfo(this, true, ModbusCtx.FreqSV, ModbusCtx.StrokeSV, ModbusCtx.FlowSV);
                isInited = true;
            }
        }
        finally
        {
            isUpdatingFromPlc = false;
        }
    }

    public double GetShowValByRaw(string name, IDataItemProp raw) => GetShowValByRaw(name, ToShort(raw));

    public double GetShowValByRaw(string name, short raw)
    {
        if (name.StartsWith("freq", StringComparison.OrdinalIgnoreCase))
            return Math.Clamp(Math.Round((float)(raw - FreqMin) / (FreqMax - FreqMin) * 100d), 0, 100);
        else if (name.StartsWith("stroke", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
            var tmpStroke = raw - Cfg.MinStroke ?? 0d;
            var tmpMaxStroke = Cfg.MaxStroke - Cfg.MinStroke ?? 1d;
            return Math.Clamp(Math.Round(tmpStroke / tmpMaxStroke * 100d), 0, 100);
        }
        else if (name.StartsWith("flow", StringComparison.OrdinalIgnoreCase))
            return raw / 100d;
        throw new ArgumentException();
    }

    public short ShowValToRaw(string name, double showVal)
    {
        if (name.StartsWith("freq", StringComparison.OrdinalIgnoreCase))
            return (short)(showVal / 100d * (FreqMax - FreqMin) + FreqMin);
        else if (name.StartsWith("stroke", StringComparison.OrdinalIgnoreCase))
        {
            return (short)showVal;
            return (short)(showVal / 100d * (Cfg.MaxStroke.Value - Cfg.MinStroke.Value) + Cfg.MinStroke.Value);
        }
        else if (name.StartsWith("flow", StringComparison.OrdinalIgnoreCase))
            return (short)(showVal * 100);
        throw new ArgumentException();
    }

    void GetToInfo(PumpVM pumpVM, bool isSV, IDataItemProp freqRaw, IDataItemProp strokeRaw, IDataItemProp flowRaw)
    {
        var sRaw = ToShort(strokeRaw);
        var tmpStroke = sRaw - Cfg.MinStroke ?? 0d;
        var tmpMaxStroke = Cfg.MaxStroke - Cfg.MinStroke ?? 1d;

        if (isSV)
        {
            pumpVM.FreqSV = GetShowValByRaw("freq", freqRaw);
            pumpVM.FreqPV = Math.Max(0, pumpVM.FreqPV);
            pumpVM.StrokeSV = GetShowValByRaw(nameof(StrokeSV), sRaw);//  Math.Clamp(Math.Round(tmpStroke / tmpMaxStroke * 100d), 0, 100);
            pumpVM.FlowSV = GetShowValByRaw(nameof(FlowSV), flowRaw);
            return;
        }

        pumpVM.FreqPV = Math.Max(0, GetShowValByRaw("freq", freqRaw));
        pumpVM.StrokePV = GetShowValByRaw("stroke",sRaw);//Math.Clamp(Math.Round(tmpStroke / tmpMaxStroke * 100d), 0, 100);
        pumpVM.FlowPV = GetShowValByRaw(nameof(FlowPV), flowRaw);
    }

    short ToShort(IDataItemProp dataItem) => dataItem.Value.ToInt16(null);

    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == "L") OnPropertyChanged(nameof(RunBtnText));
        if (e is PropertyChangedEventArgsMark arg && arg.Mark == 5) return;
        if (isUpdatingFromPlc) return;

        if (!PumpModule.IsMock)
            await set(e.PropertyName);
    }

    private void SetFieldAndMend<T>(ref T field, T value, Func<T, T> checkFun, [CallerMemberName] string? propertyName = null)
    {
        var nval = checkFun(value);
        if (!EqualityComparer<T>.Default.Equals(nval, value) && EqualityComparer<T>.Default.Equals(field, nval))
            OnPropertyChanged(new PropertyChangedEventArgsMark(propertyName, 5));
        else
            SetProperty(ref field, nval, propertyName);
    }

    void INotifyPropertyChangedExt2.OnPropertyChanged(PropertyChangedEventArgs e) => OnPropertyChanged(e);

    // 【核心重构】：所有指令直接推入 Context
    async Task set(string prop)
    {
        if (ModbusCtx == null) return;
        Console.WriteLine($"{DateTime.Now}==>set {prop}");

        if (nameof(FreqSV) == prop)
            TriggerDebounceWrite(prop, ModbusCtx.FreqSV, ShowValToRaw(nameof(FreqSV), FreqSV));
        else if (nameof(StrokeSV) == prop)
            TriggerDebounceWrite(prop, ModbusCtx.StrokeSV, ShowValToRaw(nameof(StrokeSV), StrokeSV));
        else if (nameof(FlowSV) == prop)
            TriggerDebounceWrite(prop, ModbusCtx.FlowSV, ShowValToRaw(nameof(FlowSV), FlowSV));
    }

    public PropertyChangedEventHandler PropertyChangedEventHandlerGet() => null;
}