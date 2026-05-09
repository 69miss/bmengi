using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dobo.Appl.Service;
using Dobo.Appl.Utility;
using PumpsSystem.Pump2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
namespace PumpsSystem.Pump;
public partial class PumpModel : ObservableObject
{
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

    // 数据回写
    public PumpModel CopyFrom(PumpModel source)
    {
        this.Assign(source);
        Id = source.Id;
        Name = source.Name;
        DisplayColor = source.DisplayColor;
        MaxFlow = source.MaxFlow;
        MaxStroke = source.MaxStroke;
        MinStroke = source.MinStroke;
        ProtectionThreshold = source.ProtectionThreshold;
        return this;
    }
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

    public PumpCfgViewModel()
    { 
    }
    public void Init() {
        // 初始化模拟数据
        var list = dataDictSvc.GetByJson<PumpModel[]>("PumpListCfg");
        if (list?.Length > 0)
        {
            foreach (var item in list)
            {
                PumpList.Add(item);
            }
        }
        else
        {
            //??  需将PumpItem信息也初始化
            PumpList.Add(new PumpModel { Id = "1", Name = "1#泵", DisplayColor = "#0078D4", MaxFlow = 120, MaxStroke = 100, MinStroke = 10, ProtectionThreshold = 150 });
            PumpList.Add(new PumpModel { Id = "2", Name = "2#泵", DisplayColor = "#107C10", MaxFlow = 80.5, MaxStroke = 80, MinStroke = 5, ProtectionThreshold = 90 });
            PumpList.Add(new PumpModel { Id = "3", Name = "3#泵", DisplayColor = "#D13438", MaxFlow = 15, MaxStroke = 40, MinStroke = 0, ProtectionThreshold = 25 });
            PumpList.Add(new PumpModel { Id = "4", Name = "4#泵", DisplayColor = "#FF8C00", MaxFlow = 15, MaxStroke = 40, MinStroke = 0, ProtectionThreshold = 25 });
        }
        if (PumpList.Count > 0) SelectedPump = PumpList[0];
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
        }
        else
        {
            if (_originalSelectedItem != null)
            {
                _originalSelectedItem.CopyFrom(EditingPump);
                dataDictSvc.SetJson("PumpListCfg",PumpList.ToArray());
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