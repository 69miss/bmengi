using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dobo.Appl.Entity;
using Dobo.Appl.Service;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace PumpsSystem.Pump2;
public partial class PumpModel : ObservableObject
{
    public long DbId { get; set; }
    // 编号
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    // 名称 
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    // 颜色 (存储 Hex 字符串)
    [ObservableProperty]
    public partial string DisplayColor { get; set; } = "#0078D4";

    // 详细参数
    [ObservableProperty]
    public partial double? MaxFlow { get; set; }

    [ObservableProperty]
    public partial double? MaxStroke { get; set; }

    [ObservableProperty]
    public partial double? MinStroke { get; set; }

    [ObservableProperty]
    public partial double? ProtectionThreshold { get; set; }



    // 深拷贝
    public PumpModel Clone()
    {
        return (PumpModel)this.MemberwiseClone();
    }
     DataDict dataDict;
    public static DataItemProp[] GetItemsByValue2(DataDict dataDict)
    {
        return FastSerialize.Instance.Deserialize<DataItemProp[]>(dataDict.Value2);
    }
    public static PumpModel From(DataDict dataDict)
    {
        var pm =FastSerialize.Instance.Deserialize<PumpModel>(dataDict.Value);
        //pm.AddressInfo = FastSerialize.Instance.Deserialize<PumpItem>(dataDict.Value2);
        pm.dataDict=dataDict;
        pm.DbId = dataDict.Id;
        return pm;
    }
    public DataDict ToDict(long? parentId=null) {
        var dict=new DataDict();
        dict.Value=FastSerialize.Instance.Serialize(this);
        //dict.Value2 = FastSerialize.Instance.Serialize(this.AddressInfo);
        if (parentId != null)
        {
            dict.ParentId = parentId;
            dataDict=dict;
        }
        else if (dataDict != null)
        {
            dict.Id = dataDict.Id;
            dict.Name = dataDict.Name;
            dict.ParentId = dataDict.ParentId;
        }
        return dict;
    }
    // 数据回写
    public PumpModel CopyFrom(PumpModel source)
    {
        this.Assign(source);
        dataDict=source.dataDict;
        //Id = source.Id;
        //Name = source.Name;
        //DisplayColor = source.DisplayColor;
        //MaxFlow = source.MaxFlow;
        //MaxStroke = source.MaxStroke;
        //MinStroke = source.MinStroke;
        //ProtectionThreshold = source.ProtectionThreshold;
        return this;
    }
    [JsonIgnore]
    public PumpItem AddressInfo { get; set; }
}


public partial class PumpCfgViewModel : ObservableObject, ISupportInitialize
{
    public ObservableCollection<PumpModel> PumpList { get; } = new();
    DataDictSvc dataDictSvc=new DataDictSvc();
    internal Func<string, Task<int>> MsgBoxShowFun;
    // 【修改点】使用元组 (名称, Hex值)
    public List<Tuple<string,string>> AvailableColors { get; } = new()
        {
            Tuple.Create("蓝", "#0078D4"),
            Tuple.Create("绿", "#107C10"),
            Tuple.Create("红", "#D13438"),
            Tuple.Create("橙", "#FF8C00"),
            //Tuple.Create("优雅紫", "#5C2D91"),
            //Tuple.Create("深青色", "#008272"),
            Tuple.Create("灰", "#69797E"),
            Tuple.Create("黑", "#333333")
        };

    private PumpModel? _originalSelectedItem;

    [ObservableProperty]
    private PumpModel? _selectedPump;

    [ObservableProperty]
    private PumpModel _editingPump = new();

    [ObservableProperty]
    private bool _isCreatingNew;

    [ObservableProperty]
    private bool _isIdEditable;

    [ObservableProperty]
    private string _detailTitle = "详细配置";
    DataDict dataDict;
    public PumpCfgViewModel()
    { 
    }
    public void Init() {
        dataDict = dataDictSvc.GetTreeByName("Pumps6Cfg");

        if (dataDict?.Childs?.Count > 0)
        {
            foreach (var item in dataDict.Childs)
            {
                var pm = PumpModel.From(item);//.ByValJson<PumpModel>();
                pm.DbId = item.Id;
                PumpList.Add(pm);
            }
        }
        else
        {
            var cfg = PumpsDbSet.Pumps6CfgDbInit();
            cfg.Item2.Each( pm => PumpList.Add(pm));
        }
        if (PumpList.Count > 0) SelectedPump = PumpList[0];
    }
    private IDataItemProp[] PumpsInit(IEnumerable<PumpModel> pumps)
    {
        // 寄存器复用字典，避免同一个 Modbus 地址被创建两次（特别针对状态位多泵共享时）
        var registerMap = new Dictionary<string, IDataItemProp>();
        IDataItemProp GetOrCreateRegister(IConvertible addr, string name, bool canWrite)
        {
            var addrStr = addr.ToString(null);
            if (!registerMap.TryGetValue(addrStr, out var reg))
            {
                reg = new DataItemProp<short>()
                {
                    Address = addrStr,
                    Name = name,
                    CanWrite = canWrite,
                };// CreateItem(addr, name, canWrite);
                registerMap[addrStr] = reg;
            }
            return reg;
        }

        // 1. 全局独立点位
        GetOrCreateRegister(40001, "心跳", false);

        ushort baseAddrPV = 40005;
        ushort baseAddrStrokeLimit = 40033;
        ushort modeAddr = 40004;
        var globalModeReg = GetOrCreateRegister(modeAddr, "控制模式", true);
        // 2. 遍历每个泵，为其构造专属的 PumpModbusContext
        foreach (var pm in pumps)
        {
            var pNum =int.Parse( pm.Id);
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


            var ctx = new PumpItem
            {
                IsRemote = new DataItemBitMap(statusReg, (pumpIndex % 4), $"{pNum}-远程模式"),
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
            pm.AddressInfo = ctx;
            //pm.InjectModbusContext(ctx);
        }

        // 3. 构建统一的点位集合交给 Cmd 管理
        var pArr = registerMap.Values.OrderBy(p => p.Address).ToArray();
        return pArr;
    }
    partial void OnSelectedPumpChanged(PumpModel? value)
    {
        if (value != null)
        {
            _originalSelectedItem = value;
            EditingPump = value.Clone();
            IsCreatingNew = false;
            IsIdEditable = false;
            DetailTitle = $"编辑配置 - {value.Id}";
        }
    }

    [RelayCommand]
    private void CreateNew()
    {
        SelectedPump = null;
        _originalSelectedItem = null;

        // 默认取列表第一个颜色的 Hex 值 (Item2)
        EditingPump = new PumpModel { DisplayColor = AvailableColors[0].Item2 };

        IsCreatingNew = true;
        IsIdEditable = true;
        DetailTitle = "新建泵组配置";
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(EditingPump.Id)) return;

        if (IsCreatingNew)
        {
            var newPump = EditingPump.Clone();
           
            PumpList.Add(newPump);
            SelectedPump = newPump;
            var newDict = newPump.ToDict(dataDict.Id);
            var re = dataDictSvc.Add(newDict);//.SetJson(newPump,parentId:dataDict.Id);
            newPump.DbId = re;
            newDict.Id = re;
        }
        else
        {
            if (_originalSelectedItem != null)
            {
                _originalSelectedItem.CopyFrom(EditingPump);
                dataDictSvc.Save(_originalSelectedItem.ToDict());//.SetJson(_originalSelectedItem,_originalSelectedItem.DbId,dataDict.ParentId);
                MsgBoxShowFun?.Invoke("修改成功!");
            }
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsCreatingNew)
        {
            if (PumpList.Count > 0) SelectedPump = PumpList[0];
            else EditingPump = new PumpModel { DisplayColor = AvailableColors[0].Item2 };
        }
        else
        {
            if (_originalSelectedItem != null)
            {
                EditingPump = _originalSelectedItem.Clone();
            }
        }
    }

    public void BeginInit()
    {
        Init();
    }

    public void EndInit()
    {
        throw new NotImplementedException();
    }
}