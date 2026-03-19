using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using System;

namespace PumpsSystem.Controls;

public partial class WaveGauge : UserControl
{
    // 获取 XAML 中的波浪容器
    private Grid? _waveContainer;

    public WaveGauge()
    {
        InitializeComponent();
    }
    // 定义报警状态的 StyledProperty
    public static readonly StyledProperty<bool> IsAlarmProperty =
        AvaloniaProperty.Register<WaveGauge, bool>(nameof(IsAlarm), defaultValue: false);

    // 报警属性包装器
    public bool IsAlarm
    {
        get => GetValue(IsAlarmProperty);
        set => SetValue(IsAlarmProperty, value);
    }
    // 1. 数值属性
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<WaveGauge, double>(nameof(Value), 0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    // 2. 颜色属性
    public static readonly StyledProperty<IBrush> WaveColorProperty =
        AvaloniaProperty.Register<WaveGauge, IBrush>(nameof(WaveColor), SolidColorBrush.Parse("#0ea5e9"));

    public IBrush WaveColor
    {
        get => GetValue(WaveColorProperty);
        set => SetValue(WaveColorProperty, value);
    }

    // 3. 文本属性
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<WaveGauge, string>(nameof(Text), "0.0");

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly StyledProperty<string> SubTextProperty =
        AvaloniaProperty.Register<WaveGauge, string>(nameof(Text), "子文本");

    public string SubText
    {
        get => GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }
    // --- 核心逻辑：无需 Converter，直接监听属性变化 ---
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty || change.Property == BoundsProperty)
        {
            UpdateWaveHeight();
        }
    }

    // ApplyTemplate 是 Avalonia 加载 XAML 控件时的回调
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // 找到 XAML 中名为 PART_WaveContainer 的控件
        _waveContainer = PART_WaveContainer;// e.NameScope.Find<Grid>("PART_WaveContainer"); 
        UpdateWaveHeight();
    }

    private void UpdateWaveHeight()
    {
        if (_waveContainer == null) return;

        // 计算高度：总高度 * (百分比 / 100)
        // +5 是为了保留一点底部波浪
        double targetHeight = (Bounds.Height * (Math.Clamp(Value, 0, 100) / 100.0)) ;//+ 5
        path1.IsVisible = targetHeight >= 1;
        path2.IsVisible = path1.IsVisible;
        if (path1.IsVisible && targetHeight < 5)
            targetHeight = 5;
        // 直接设置控件高度
        _waveContainer.Height = targetHeight;
    }
}