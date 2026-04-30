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
    DataDictSvc dataDictSvc;
    internal Func<string, Task<int>> MsgBoxShowFun;
    PumpModel[] pumpCfgs;
    bool isInited = false;

    // 此地址为全局自动模式下发地址，单独保留引用以供命令使用
    private IDataItemBase globalModeReg;

    public Window1VM()
    {
        if (PumpModule.IsMock) Mock();

        dataDictSvc = new DataDictSvc();
        pumpCfgs = dataDictSvc.GetByJson<PumpModel[]>("PumpListCfg");
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

    async Task ValEdit(IDataItemBase dataItem, ushort val)
    {
        await cmd.WriteValue(dataItem.Address, (short)val);
    }

    // 【核心重构】：动态分配点位与寄存器复用
    public async Task InitConnect()
    {
        try
        {
            cmd?.Dispose();

            // 寄存器复用字典，避免同一个 Modbus 地址被创建两次（特别针对状态位多泵共享时）
            var registerMap = new Dictionary<ushort, IDataItemBase>();
            IDataItemBase GetOrCreateRegister(ushort addr, string name, bool canWrite)
            {
                if (!registerMap.TryGetValue(addr, out var reg))
                {
                    reg = CreateItem(addr, name, canWrite);
                    registerMap[addr] = reg;
                }
                return reg;
            }

            // 1. 全局独立点位
            GetOrCreateRegister(40001, "心跳", false);

            ushort baseAddrPV = 40005;
            ushort baseAddrStrokeLimit = 40033;

            // 2. 遍历每个泵，为其构造专属的 PumpModbusContext
            foreach (var pumpVM in Pumps)
            {
                var pNum = pumpVM.Id;
                var pumpIndex = pNum - 1;

                // 备注：如果以后需要在 PumpModel 中配置任意点位，可替换如下计算方式
                // 例如：ushort statusAddr = pumpVM.Cfg.StatusAddress ?? (ushort)(40002 + pumpIndex / 4);

                // --- 状态位 (兼容旧版：每4个泵占1个寄存器) ---
                ushort statusAddr = (ushort)(40002 + pumpIndex / 4);
                var statusReg = GetOrCreateRegister(statusAddr, $"状态反馈_{statusAddr}", false);

                // --- 控制位 (兼容旧版：每16个泵占1个寄存器) ---
                ushort ctlAddr = (ushort)(40003 + pumpIndex / 16);
                var ctlReg = GetOrCreateRegister(ctlAddr, $"控制字_{ctlAddr}", true);

                // --- 模式位 (兼容旧版：共用40004) ---
                ushort modeAddr = 40004;
                globalModeReg = GetOrCreateRegister(modeAddr, "控制模式", true);

                var ctx = new PumpItem
                {
                    IsRemote = new DataItemBitMap(statusReg, (pumpIndex % 4),$"{pNum}-远程模式"),
                    IsFault = new DataItemBitMap(statusReg, (pumpIndex % 4) + 4, $"{pNum}-错误状态"),
                    IsRunning = new DataItemBitMap(statusReg, (pumpIndex % 4) + 8, $"{pNum}-运行状态"),
                    CtlRunning = new DataItemBitMap(ctlReg, pumpIndex % 16, $"{pNum}-运行状态设置"),
                    ModeFlow = new DataItemBitMap(globalModeReg, 0, $"{pNum}-流量模式"),
                    ModeManual = new DataItemBitMap(globalModeReg, 1, $"{pNum}-手动模式"),

                    FreqPV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-频率", false),
                    StrokePV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-冲程", false),
                    FlowPV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-流量", false),
                    MaxFlowSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-最大流量", true),
                    FreqSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-频率设定", true),
                    StrokeSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-冲程设定", true),
                    FlowSV = GetOrCreateRegister(baseAddrPV++, $"泵{pNum}-流量设定", true),

                    MaxStroke = GetOrCreateRegister(baseAddrStrokeLimit++, $"泵{pNum}-最大冲程", true),
                    MinStroke = GetOrCreateRegister(baseAddrStrokeLimit++, $"泵{pNum}-最小冲程", true)
                };

                pumpVM.InjectModbusContext(ctx);
            }

            // 3. 构建统一的点位集合交给 Cmd 管理
            var pArr = registerMap.Values.OrderBy(p => p.Address).ToArray();
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

    short ToShort(IDataItemBase dataItem) => dataItem.Value.ToInt16(null);

    partial void OnIsConnectionChanged(bool value)
    {
        BtnConnectionText = value ? "断开" : "连接";
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
        if (sender is not IDataItemBase item) return;

        if (item.Address == 40004)
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