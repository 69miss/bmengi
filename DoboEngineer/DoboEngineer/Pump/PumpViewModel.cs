using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dobo.Appl.Utility;
using System;
using System.ComponentModel;
using System.Reflection;
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
    /// <summary>
    /// 最大流量
    /// </summary>
    [ObservableProperty] private double _flowMax;
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
    public string RemoteText => IsRemote ? "远程 (Remote)" : "本地 (Local)";
    public IBrush RemoteColor => IsRemote ? SolidColorBrush.Parse("#3b82f6") : SolidColorBrush.Parse("#f97316"); // 蓝/橙
    public IBrush StatusColor => IsRunning ? SolidColorBrush.Parse("#22c55e") : SolidColorBrush.Parse("#cbd5e1"); // 绿/灰

    public string RunBtnText => IsRunning ? "停止运行" : "启动运行";
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

    

    public PumpViewModel(int id, Func<IDataItemBase, ushort,Task> fun=null)
    {
       EditValFun = fun;
        Id = id;
        Name = $"{id}# 泵";
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
           
            FlowSV = FlowSV + amount;// Math.Clamp(FlowSV + amount, 0, 100);
        }
        else if (target == "Stroke" && CanEditParam) {
            StrokeSV = StrokeSV + amount;//Math.Clamp(StrokeSV + amount, 0, 100);
        }
        else if (target == "Freq" && CanEditParam) {
            FreqSV = FreqSV + amount; // Math.Clamp(FreqSV + amount, 0, 100); 
        }
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
            pumpVM.FlowMax = PumpsInfo[index++].Value.ToInt16(null);
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
    void GetToInfo(PumpViewModel pumpVM,bool isSV ,IDataItemBase freqRaw, IDataItemBase strokeRaw, IDataItemBase flowRaw) {

        if (isSV) {
            pumpVM.FreqSV = Math.Round((ToShort(freqRaw) - 5530) / (27648d - 5530) * 100d);
            pumpVM.StrokeSV = Math.Round(ToShort(strokeRaw) / 27648d * 100d);
            pumpVM.FlowSV = Math.Round(ToShort(flowRaw) / 27648d * 100d);
            return;
        }
        pumpVM.FreqPV = Math.Round((ToShort(freqRaw) - 5530) / (27648d - 5530) * 100d);
        pumpVM.StrokePV = Math.Round(ToShort(strokeRaw) / 27648d * 100d);
        pumpVM.FlowPV = Math.Round(ToShort(flowRaw) / 27648d * 100d);
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
        if (!isUpdatingFromPlc)
            await set(e.PropertyName);
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
        else if (nameof(FlowMax) == prop)
        {
            await EditValFun?.Invoke(PumpsInfo[num+3], (ushort)FlowMax);
            //PumpsInfo[num + 6].Value = FlowMax;
        }
        else if (nameof(FreqSV) == prop)
        {
            double freqRaw = (FreqSV / 100d * (27648 - 5530) + 5530);
            await EditValFun?.Invoke(PumpsInfo[num+4], (ushort)freqRaw);
            //PumpsInfo[num + 7].Value = FreqSV;
        }
        else if (nameof(StrokeSV) == prop)
        {
            double strokeRaw = (StrokeSV / 100d * 27648);
            await EditValFun?.Invoke(PumpsInfo[num+5], (ushort)strokeRaw);
        }
        else if (nameof(FlowSV) == prop)
        {
            var flowRaw = (FlowSV / 100d * 27648);
            await EditValFun?.Invoke(PumpsInfo[num+6], (ushort)flowRaw);
        }
        else if (nameof(StrokeMax) == prop)
        {
            //PumpsInfo[num + 31].Value = StrokeMax;
            await EditValFun?.Invoke(PumpsInfo[indexArr[1]], (ushort)StrokeMax);
        }
        else if (nameof(StrokeMin) == prop)
        {
            //PumpsInfo[num + 32].Value = StrokeMin;
            await EditValFun?.Invoke(PumpsInfo[indexArr[1]+1], (ushort)StrokeMin);
        }
    }
}
