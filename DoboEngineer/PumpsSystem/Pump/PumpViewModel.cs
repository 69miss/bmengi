using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dobo.Appl.Utility;
using PumpsSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static Dobo.Appl.Utility.INotifyPropertyChangedExt;

namespace PumpsSystem.Pump;

public partial class PumpViewModel : ViewModelBase,INotifyPropertyChangedExt2
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = string.Empty;

    // --- 设定值 (SV) ---
     private double _flowSV;
     private double _strokeSV;
     private double _freqSV;
    /// <summary>
    /// 最大流量
    /// </summary>
    [ObservableProperty][NotifyPropertyChangedFor(nameof(LiquidHeight))] private double _flowMax;
    /// <summary>
    /// 最大冲程
    /// </summary>
    [ObservableProperty] private double _strokeMax;
    /// <summary>
    /// 最小冲程
    /// </summary>
    [ObservableProperty] private double _strokeMin;
    // --- 实际反馈 (PV) ---
    [ObservableProperty][NotifyPropertyChangedFor(nameof(LiquidHeight))] private double _flowPV;
    [ObservableProperty] private double _strokePV;
    [ObservableProperty] private double _freqPV;

    // --- 状态 ---
    [ObservableProperty][NotifyPropertyChangedFor(nameof(StatusColor))][NotifyPropertyChangedFor(nameof(RunBtnText))][NotifyPropertyChangedFor(nameof(RunBtnColor))] 
    private bool _isRunning;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(RemoteText))][NotifyPropertyChangedFor(nameof(RemoteColor))] private bool _isRemote;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFaulty))] private bool _isFault;

    // --- 权限 ---
    [ObservableProperty] private bool _canEditFlow;
    [ObservableProperty] private bool _canEditParam;

    public short FreqMax { get; } = 27648;
    public short FreqMin { get; } = 5530;

    // --- 辅助属性 ---
    // 1. 液位球高度计算 (假设球体总高度 120px，最大流量 100 L/h)
    // 限制在 0-120 之间
    public double LiquidHeight => Math.Clamp((FlowPV / FlowMax) * 100.0, 0, 100);

    // 2. 状态显示优化
    public string RemoteText => IsRemote ? L.RemoteStatusLabel :L.LocalStatusLabel ;
    public IBrush RemoteColor => IsRemote ? SolidColorBrush.Parse("#3b82f6") : SolidColorBrush.Parse("#f97316"); // 蓝/橙
    public IBrush StatusColor => IsRunning ? SolidColorBrush.Parse("#22c55e") : SolidColorBrush.Parse("#cbd5e1"); // 绿/灰

    public string RunBtnText => IsRunning ? L.StopRunningStatus:L.StartRunningStatus;
    public IBrush RunBtnColor => IsRunning ? SolidColorBrush.Parse("#fee2e2") : SolidColorBrush.Parse("#dcfce7"); // 浅红背景/浅绿背景
    public IBrush RunBtnFg => IsRunning ? SolidColorBrush.Parse("#ef4444") : SolidColorBrush.Parse("#16a34a");   // 深红字/深绿字


    [ObservableProperty]
      IBrush waveGaugeFg = SolidColorBrush.Parse("#0ea5e9");
    public bool IsFaulty => IsFault;

     bool isInited=false;


    public ObservableItemCollection<IDataItemBase> PumpsInfo
    {
        get => pumpsInfo;
        set
        {
            pumpsInfo = value;
            if (pumpsInfo != null)
            {
                pumpsInfo.ItemsPropertyChangedEnd += PumpsInfo_ItemsPropertyChangedEnd;
                isInited = false;
            }
        }
    }

    private void PumpsInfo_ItemsPropertyChangedEnd(object arg1, DateTime arg2)
    {
        get();
    }

    
    public PumpModel Cfg { get;set; }
    public INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public double FlowSV { get => _flowSV; set => NotifyThis.SetField(ref _flowSV, value); }
    public double StrokeSV
    {
        get => _strokeSV;
        set
        {
            SetFieldAndMend(ref _strokeSV,value,p=> Math.Clamp(p, 0, 100));
        }
    }

    private void SetFieldAndMend<T>(ref T field,T value,Func<T,T> checkFun, [CallerMemberName] string? propertyName = null)
    {
        var nval =  checkFun(value) ;//Math.Clamp(value, 0, 100);
        if (!EqualityComparer<T>.Default.Equals(nval, value)&& EqualityComparer<T>.Default.Equals(field, nval))
            OnPropertyChanged(new PropertyChangedEventArgsMark(propertyName, 5));
        else
            SetProperty(ref field, nval,propertyName);

        //if (nval != value && field == nval)
        //{
        //    OnPropertyChanged(new PropertyChangedEventArgsMark(nameof(StrokeSV), 5));
        //}
        //else
        //    SetProperty(ref _strokeSV, nval);
    }

    public double FreqSV { get => _freqSV; set => SetFieldAndMend(ref _freqSV,value, p => Math.Clamp(p, 0, 100)); }


    public PumpViewModel(int id, Func<IDataItemBase, ushort,Task> fun=null)
    {
       EditValFun = fun;
        Id = id;
        Name = $"{id}#";
        FlowSV = 0; StrokeSV = 0; FreqSV = 0;
        //IsRemote = true;
       
    }

    [RelayCommand]
    public async void ToggleRun()
    {
        if (!IsRemote) //IsRunning = !IsRunning;
            return;
        var num = this.Id;
        var val = PumpsInfo[2].Value.ToUInt16(null);
        if (!IsRunning)
            val |= (ushort)(1 << (num - 1));
        else
            val &= (ushort)~(1 << (num - 1));
        try
        {
            await EditValFun?.Invoke(PumpsInfo[2], val);
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

        if (target == "Flow" && CanEditFlow) {
           
            FlowSV =Math.Clamp( FlowSV + amount,0,FlowMax);// Math.Clamp(FlowSV + amount, 0, 100);
        }
        else if (target == "Stroke" && CanEditParam) {
            StrokeSV = Math.Clamp(StrokeSV + amount, 10, 100);
        }
        else if (target == "Freq" && CanEditParam) {
            FreqSV = Math.Clamp(FreqSV + amount, 10, 100); 
        }
    }
    // 1. 定义一个取消令牌源，用于管理延时任务
    private CancellationTokenSource? _debounceCts;
    string nowDebounceName;
    private void TriggerDebounceWrite(string name,params object[] args)
    {
        if (name == nowDebounceName)
        {// 1. 如果之前有正在等待的任务，取消它！
            _debounceCts?.Cancel();
            Console.WriteLine($"防抖取消执行：{name}{args[0]}");
        }
         nowDebounceName=name;
        // 2. 创建新的令牌
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        // 3. 启动后台任务
        Task.Run(async () =>
        {
            try
            {
                // 4. 等待 500毫秒 (可调整)
                // 如果在等待期间用户又点了一下，token.IsCancellationRequested 会变成 true
                await Task.Delay(500, token);

                // 5. 检查是否被取消
                if (token.IsCancellationRequested) return;

                // 6. --- 真正执行远程调用 ---
                if (args[1] is IConvertible val)
                    await EditValFun?.Invoke((IDataItemBase)args[0], val.ToUInt16(null));
                else
                    throw new ArgumentException("无法处理的数据类型");
            }
            catch (TaskCanceledException)
            {
                // 任务被取消是正常的，忽略异常
            }
            catch (Exception ex)
            {
                // 处理通讯异常
                Console.WriteLine($"写入失败: {ex}");
            }
        });
    }
    ObservableItemCollection<IDataItemBase> pumpsInfo;
    bool isUpdatingFromPlc;
    void get()
    {
        isUpdatingFromPlc = true;
        try
        {
            //todo 进行展示转换，将原始值转为用户展示值,应先做一次连接后的初始，然后再监听

            PumpViewModel pumpVM = this;
            var num = pumpVM.Id;
            var statusInt = pumpsInfo[1].Value.ToInt16(null);
            var ctlMode = (pumpsInfo[3] as PumpCtlMode);
            pumpVM.IsRemote = (statusInt & (1 << num - 1)) != 0;
            pumpVM.IsFault = (statusInt & (1 << num + 3)) != 0;
            pumpVM.IsRunning = (statusInt & (1 << num + 7)) != 0;
            pumpVM.CanEditFlow = ctlMode.Flow && pumpVM.IsRemote;
            pumpVM.CanEditParam = ctlMode.Manual && pumpVM.IsRemote;
            var indexArr = GetPumpsInfoIndex(Id);
            var index = indexArr[0];
            //short freqRaw = PumpsInfo[index++].Value.ToInt16(null); //频率 5530--27648对应0-100
            //pumpVM.FreqPV = Math.Round((freqRaw-5530)/(27648-5530)*100d);
            //pumpVM.StrokePV = Math.Round(PumpsInfo[index++].Value.ToInt16(null)/ 27648*100d);
            //pumpVM.FlowPV = Math.Round(PumpsInfo[index++].Value.ToInt16(null)/ 27648*100d);
            //GetPVInfo(pumpVM, PumpsInfo[index++].Value.ToInt16(null), PumpsInfo[index++].Value.ToInt16(null), PumpsInfo[index++].Value.ToInt16(null));
            GetToInfo(pumpVM, false, PumpsInfo[index++], PumpsInfo[index++], PumpsInfo[index++]);
            //
            if (isInited)
                return;
            pumpVM.FlowMax = GetShowValByRaw(nameof(FlowMax), PumpsInfo[index++]);// PumpsInfo[index++].Value.ToInt16(null)/100d;
            //pumpVM.FreqSV = PumpsInfo[index++].Value.ToInt16(null);
            //pumpVM.StrokeSV = PumpsInfo[index++].Value.ToInt16(null);
            //pumpVM.FlowSV = PumpsInfo[index++].Value.ToInt16(null);
            GetToInfo(pumpVM, true, PumpsInfo[index++], PumpsInfo[index++], PumpsInfo[index++]);
            var index2 = indexArr[1];
            pumpVM.StrokeMax = PumpsInfo[index2].Value.ToInt16(null);
            pumpVM.StrokeMin = PumpsInfo[index2+1].Value.ToInt16(null);
            isInited = true;
        }
        finally
        {
            isUpdatingFromPlc = false;
        }
    }
    void GetPVInfo(PumpViewModel pumpVM, double freqRaw,double strokeRaw,double flowRaw) {

        pumpVM.FreqPV = Math.Round((freqRaw - 5530) / (27648 - 5530) * 100d);

        pumpVM.StrokePV = Math.Round(strokeRaw / 27648 * 100d);
        pumpVM.FlowPV = Math.Round(flowRaw / 27648 * 100d);
    }

    public double GetShowValByRaw(string name, IDataItemBase raw) {
        return GetShowValByRaw(name,  ToShort(raw));
    }
    public double GetShowValByRaw(string name, short raw)
    {
        if (name.StartsWith("freq", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Clamp(Math.Round((float)(raw - FreqMin) / (FreqMax - FreqMin) * 100d), 0, 100);
        }
        else if (name.StartsWith("stroke", StringComparison.OrdinalIgnoreCase))
        {
            var tmpStroke = raw - Cfg.MinStroke ?? 0d;
            var tmpMaxStroke = Cfg.MaxStroke - Cfg.MinStroke ?? 1d;
            return Math.Clamp(Math.Round(tmpStroke / tmpMaxStroke * 100d), 0, 100);
        }
        else if (name.StartsWith("flow", StringComparison.OrdinalIgnoreCase))
        {
            return raw / 100d;
        }
        throw new ArgumentException();
    }
    public short ShowValToRaw(string name, double showVal)
    {
        if (name.StartsWith("freq", StringComparison.OrdinalIgnoreCase))
            return (short)(showVal / 100d * (FreqMax - FreqMin) + FreqMin); 
        else if (name.StartsWith("stroke", StringComparison.OrdinalIgnoreCase))
        {
           return (short)(showVal / 100d * (Cfg.MaxStroke.Value - Cfg.MinStroke.Value) + Cfg.MinStroke.Value);
        }
        else if (name.StartsWith("flow", StringComparison.OrdinalIgnoreCase))
        {
            return (short)(showVal * 100);
        }
        throw new ArgumentException();
    }
    void GetToInfo(PumpViewModel pumpVM,bool isSV ,IDataItemBase freqRaw, IDataItemBase strokeRaw, IDataItemBase flowRaw) {

        var sRaw = ToShort(strokeRaw);
        var tmpStroke =sRaw-Cfg.MinStroke ?? 0d;
        var tmpMaxStroke =Cfg.MaxStroke - Cfg.MinStroke??1d;
        if (isSV) {
            pumpVM.FreqSV = GetShowValByRaw("freq", freqRaw);//  Math.Round((float)(ToShort(freqRaw) -FreqMin) / (FreqMax - FreqMin) * 100d);
            if (pumpVM.FreqPV < 0)
                pumpVM.FreqPV = 0;
            pumpVM.StrokeSV = Math.Round(tmpStroke / tmpMaxStroke * 100d);
            pumpVM.StrokeSV = Math.Clamp(pumpVM.StrokeSV, 0, 100);
            pumpVM.FlowSV = GetShowValByRaw(nameof(FlowSV), flowRaw); //Math.Round(ToShort(flowRaw) / 27648d * 100d);
            return;
        }
        pumpVM.FreqPV = GetShowValByRaw("freq", freqRaw);// Math.Round((float)(ToShort(freqRaw) - FreqMin) / (FreqMax - FreqMin) * 100d);
        if (pumpVM.FreqPV < 0)
            pumpVM.FreqPV = 0;
        pumpVM.StrokePV = Math.Round(tmpStroke / tmpMaxStroke * 100d);
        pumpVM.StrokePV = Math.Clamp(pumpVM.StrokePV, 0, 100);
        pumpVM.FlowPV = GetShowValByRaw(nameof(FlowPV),flowRaw); // ToShort(flowRaw)/100;// Math.Round(ToShort(flowRaw) / 27648d * 100d);
    }
    short ToShort(IDataItemBase dataItem) {

        return dataItem.Value.ToInt16(null);
    }
    int[] GetPumpsInfoIndex(int num) {
        return [(num - 1)*7 + 4,(num-1)*2+32];
    }
    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {

        base.OnPropertyChanged(e);
        if (e.PropertyName == "L")
        {
            OnPropertyChanged(nameof(RunBtnText));
        }
        if (e is PropertyChangedEventArgsMark arg)
        {
            if (arg.Mark == 5)
                return;
        }
        if (isUpdatingFromPlc)
            return;
        if (!Module.PumpModule.IsMock)
            await set(e.PropertyName);

    }

     void INotifyPropertyChangedExt2.OnPropertyChanged(PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e);
    }

     Func<IDataItemBase, ushort,Task> EditValFun;
    async Task set(string prop) {
        Console.WriteLine($"{DateTime.Now}==>set {prop}");
        var indexArr = GetPumpsInfoIndex(Id);
        var num = indexArr[0];
        if (nameof(IsRunning) == prop)
        {
            //var val = PumpsInfo[2].Value.ToUInt16(null);
            //if (IsRunning) 
            //    val |= (ushort)(1 << (num - 1));
            //else
            //    val &= (ushort)~(1 << (num - 1));
            //await EditValFun?.Invoke(PumpsInfo[2], val);
            //PumpsInfo[2].Value = val;
        }
        //else if (nameof(FlowMax) == prop)
        //{
        //    await EditValFun?.Invoke(PumpsInfo[num+3], (ushort)FlowMax);

        //}
        else if (nameof(FreqSV) == prop)
        {
           // double freqRaw = (FreqSV / 100d * (27648 - 5530) + 5530);
            TriggerDebounceWrite(prop,PumpsInfo[num + 4], ShowValToRaw(nameof(FreqSV), FreqSV));
            //await EditValFun?.Invoke(PumpsInfo[num+4], (ushort)freqRaw);
        }
        else if (nameof(StrokeSV) == prop)
        {
            //double strokeRaw = (StrokeSV / 100d * (Cfg.MaxStroke.Value - Cfg.MinStroke.Value)+ Cfg.MinStroke.Value);
            TriggerDebounceWrite(prop,PumpsInfo[num + 5], ShowValToRaw(nameof(StrokeSV), StrokeSV));
            //await EditValFun?.Invoke(PumpsInfo[num+5], (ushort)strokeRaw);
        }
        else if (nameof(FlowSV) == prop)
        {
            //var flowRaw = FlowSV; //(FlowSV / 100d * 27648);
            TriggerDebounceWrite(prop, PumpsInfo[num + 6], ShowValToRaw(nameof(FlowSV), FlowSV));
            //await EditValFun?.Invoke(PumpsInfo[num+6], (ushort)flowRaw);
        }
        //else if (nameof(StrokeMax) == prop)
        //{
        //    //PumpsInfo[num + 31].Value = StrokeMax;
        //    await EditValFun?.Invoke(PumpsInfo[indexArr[1]], (ushort)StrokeMax);
        //}
        //else if (nameof(StrokeMin) == prop)
        //{
        //    //PumpsInfo[num + 32].Value = StrokeMin;
        //    await EditValFun?.Invoke(PumpsInfo[indexArr[1]+1], (ushort)StrokeMin);
        //}
    }

    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return null;
    }

}
