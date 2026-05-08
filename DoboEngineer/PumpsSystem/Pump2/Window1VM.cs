using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dobo.Appl.Service;
using Microsoft.Extensions.DependencyInjection;
using PumpsSystem.Module;
using PumpsSystem.Pump; // 引用原有接口
using PumpsSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2;

public partial class Window1VM : ViewModelBase, IDisposable
{
    public ObservableCollection<PumpVM> Pumps { get; } = new();
    internal PumpCmd cmd;
    [ObservableProperty] private bool _isAutoMode;
    [ObservableProperty] private bool _isAutoModeSet; [ObservableProperty] private string _systemTime = string.Empty;
    //DataDictSvc dataDictSvc;
    internal Func<string, Task<int>> MsgBoxShowFun;
    PumpModel[] pumpCfgs;
    bool isInited = false;

    // 此地址为全局自动模式下发地址，单独保留引用以供命令使用
    private IDataItemProp globalModeReg;
    Tuple<IDataItemProp[], PumpModel[]>  mainCfg;
    public Window1VM()
    {
        if (PumpModule.IsMock) Mock();

        //dataDictSvc = new DataDictSvc();
        mainCfg = PumpsDbSet.GetCfg("Pumps6Cfg");
        if(mainCfg==null)
            return;
        globalModeReg = (mainCfg.Item2[0].AddressInfo.ModeFlow as IDataItemPropWrap).Register;
        pumpCfgs = mainCfg.Item2; //dataDictSvc.GetByJson<PumpModel[]>("PumpListCfg");
        if (pumpCfgs == null) return;

        pumpCfgs = pumpCfgs.OrderBy(p => p.Id).ToArray();
        for (int i = 0; i < pumpCfgs.Length; i++)
        {
            var cfg = pumpCfgs[i];
            Pumps.Add(new PumpVM(i + 1, ValEdit)
            {
                Name = cfg.Name,
                Cfg = cfg,
                WaveGaugeFg = SolidColorBrush.Parse(cfg.DisplayColor)
            });
        }
    }

    async Task ValEdit(IDataItemProp dataItem, IConvertible val)
    {
        await cmd.WriteValue(dataItem.Address, val);
    }

    // 【核心重构】：动态分配点位与寄存器复用
    public async Task InitConnect()
    {
        try
        {
            cmd?.Dispose();
            IDataItemProp[] pArr = mainCfg.Item1;
            cmd = new PumpCmd(pArr);

            if (!PumpModule.IsMock)
                await cmd.Connect();

            isInited = false;
            await CfgChenk();

            // 绑定事件订阅
            cmd.Items.ItemPropertyChanged += Items_ItemPropertyChanged;
            cmd.Items.ItemsPropertyChangedEnd += (s, e) =>
            {
                foreach (var p in Pumps) p.SyncFromPlc();
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            MsgBoxShowFun(L.ConnectionException + " ：" + ex.Message);
            cmd?.Dispose();
            cmd = null;
            throw;
        }
    }



    [ObservableProperty] string btnConnectionText = "连接";
    [ObservableProperty] bool isConnection = false; 
     
    [RelayCommand]
    async Task ConnectCmd()
    {
        var isConn = cmd == null ? false : cmd.IsConnection;
        if (isConn != IsConnection)
            IsConnection = isConn;
        else if (isConn == true)
        {
            cmd.Dispose();
            IsConnection = false;
        }
        else
        {
            await InitConnect();
            IsConnection = cmd.IsConnection;
        }
    }

    // 【核心重构】：利用上下文结构进行配置检查
    async Task CfgChenk()
    {
        if (pumpCfgs == null || pumpCfgs.Length == 0) return;

        foreach (var pump in Pumps)
        {
            var cfg = pump.Cfg;
            var ctx = pump.ModbusCtx;
            if (ctx == null) continue;

            if (cfg.MaxFlow != ToShort(ctx.MaxFlowSV))
                await cmd.WriteValue(ctx.MaxFlowSV.Address, (short)cfg.MaxFlow);

            if (cfg.MaxStroke != ToShort(ctx.MaxStroke))
                await cmd.WriteValue(ctx.MaxStroke.Address, (short)cfg.MaxStroke);

            if (cfg.MinStroke != ToShort(ctx.MinStroke))
                await cmd.WriteValue(ctx.MinStroke.Address, (short)cfg.MinStroke);
        }
    }

    short ToShort(IDataItemProp dataItem) => dataItem.Value.ToInt16(null);

    partial void OnIsConnectionChanged(bool value)
    {
        BtnConnectionText = value ? "断开" : "连接";
    }


    private void Items_ItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not IDataItemProp item) return;

        if (item.Address == "40004")
        {
            int val = item.Value.ToInt32(null);
            IsAutoMode = (val & (1 << 0)) != 0; // 第0位为自动模式Flow位
            if (!isInited)
            {
                IsAutoModeSet = IsAutoMode;
                isInited = true;
            }
        }
    }

    partial void OnIsAutoModeSetChanged(bool val)
    {
        if (!isInited || cmd == null || globalModeReg == null) return;

        int modeVal = 0;
        if (val)
            modeVal |= (1 << 0); // Flow mode
        else
            modeVal |= (1 << 1); // Manual mode

        cmd.WriteValue(globalModeReg.Address, (short)modeVal);
        globalModeReg.Value = modeVal;
    }

    [RelayCommand]
    public void LangChange()
    {
        var lang = L;
        if (lang == null) return;

        if (CultureInfo.CurrentCulture.Name == "en")
        {
            WeakReferenceMessenger.Default.Send(PumpLang.Instance);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-Cn");
        }
        else
        {
            WeakReferenceMessenger.Default.Send<PumpLang>(PumpEnLang.Instance);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en");
        }
    }

    private void Mock()
    {
        IsConnection = true;
        IsAutoMode = true;
        for (int i = 1; i <= 4; i++)
        {
            var p = new PumpVM(i);
            if (i == 2) p.IsRunning = true;
            if (i == 4)
            {
                p.IsRunning = true;
                p.IsRemote = false;
                p.IsFault = true;
            }
            p.CanEditFlow = true;
            p.CanEditParam = true;
            p.IsRemote = true;
            p.FlowMax = i * 11;
            p.FlowSV = i * 10;

            Pumps.Add(p);
        }

        UpdatePermissions();

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
            p.CanEditFlow = IsAutoMode && p.IsRemote;
            p.CanEditParam = !IsAutoMode && p.IsRemote;

            if (p.IsRunning)
            {
                double targetFlow = p.FlowSV;
                if (p.FlowPV < targetFlow) p.FlowPV += 1.5;
                else if (p.FlowPV > targetFlow) p.FlowPV -= 1.5;

                p.FlowPV += (rnd.NextDouble() - 0.5) * 0.5;

                p.StrokePV = p.StrokeSV;
                p.FreqPV = p.FreqSV;
            }
            else
            {
                if (p.FlowPV > 0) p.FlowPV -= 2.0;
                if (p.FlowPV < 0) p.FlowPV = 0;
            }
        }
    }

    public void Dispose()
    {
        cmd?.Dispose();
    }
}