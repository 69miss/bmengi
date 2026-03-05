using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.ObjectModel;
using System.Linq;
namespace DoboEngineer.Pump;
public partial class PumpModel : ObservableObject
{
    // 编号
    [ObservableProperty] private string _id = string.Empty;

    // 名称
    [ObservableProperty] private string _name = string.Empty;

    // 颜色 (存储 Hex 字符串)
    [ObservableProperty] private string _displayColor = "#0078D4";

    // 详细参数
    [ObservableProperty] private double? _maxFlow;          // 最大流量
    [ObservableProperty] private double? _maxStroke;        // 最大冲程
    [ObservableProperty] private double? _minStroke;        // 最小冲程
    [ObservableProperty] private double? _protectionThreshold; // 保护阈值

    // 深拷贝
    public PumpModel Clone()
    {
        return (PumpModel)this.MemberwiseClone();
    }

    // 数据回写
    public void CopyFrom(PumpModel source)
    {
        Id = source.Id;
        Name = source.Name;
        DisplayColor = source.DisplayColor;
        MaxFlow = source.MaxFlow;
        MaxStroke = source.MaxStroke;
        MinStroke = source.MinStroke;
        ProtectionThreshold = source.ProtectionThreshold;
    }
}

public partial class PumpCfgViewModel : ObservableObject
{
    public ObservableCollection<PumpModel> PumpList { get; } = new();

    // 【修改点】使用元组 (名称, Hex值)
    public List<Tuple<string,string>> AvailableColors { get; } = new()
        {
            Tuple.Create("科技蓝", "#0078D4"),
            Tuple.Create("成功绿", "#107C10"),
            Tuple.Create("警示红", "#D13438"),
            Tuple.Create("警告橙", "#FF8C00"),
            Tuple.Create("优雅紫", "#5C2D91"),
            Tuple.Create("深青色", "#008272"),
            Tuple.Create("工业灰", "#69797E"),
            Tuple.Create("暗夜黑", "#333333")
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
        // 初始化模拟数据
        PumpList.Add(new PumpModel { Id = "1", Name = "1#泵", DisplayColor = "#0078D4", MaxFlow = 120, MaxStroke = 100, MinStroke = 10, ProtectionThreshold = 150 });
        PumpList.Add(new PumpModel { Id = "2", Name = "2#泵", DisplayColor = "#107C10", MaxFlow = 80.5, MaxStroke = 80, MinStroke = 5, ProtectionThreshold = 90 });
        PumpList.Add(new PumpModel { Id = "3", Name = "3#泵", DisplayColor = "#D13438", MaxFlow = 15, MaxStroke = 40, MinStroke = 0, ProtectionThreshold = 25 });
        PumpList.Add(new PumpModel { Id = "4", Name = "4#泵", DisplayColor = "#D13438", MaxFlow = 15, MaxStroke = 40, MinStroke = 0, ProtectionThreshold = 25 });


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
}