using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace PumpsSystem.Module;

public class PumpLang : LangBase
{
    public static PumpLang Instance { get; private set; } = new ();
    
    public virtual string AppTitle => GetString($"泵站控制系统 - DoboColor v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}.{Program.TimeVer}") ;
    /// <summary>
    /// 连接异常
    /// </summary>
    public virtual string ConnectionException => GetString("连接异常");

    #region 控制模式与顶部区域
    /// <summary>
    /// 控制模式:
    /// </summary>
    public virtual string ControlModeLabel => GetString("控制模式:");

    /// <summary>
    /// 手动参数
    /// </summary>
    public virtual string ManualParamMode => GetString("参数模式");

    /// <summary>
    /// 流量自动
    /// </summary>
    public virtual string FlowAutoMode => GetString("流量模式");

    /// <summary>
    /// 自动模式提示文本
    /// </summary>
    public virtual string AutoModeTip => GetString("*自动模式下仅需设定流量，其他参数由系统分配");

    public virtual string ManualParamModeTip => GetString("*手动模式下无需设定流量");

    /// <summary>
    /// 断开按钮文本
    /// </summary>
    public virtual string DisconnectBtnText => GetString("断开");

    public virtual string ConnectBtnText => GetString("连接");
    #endregion

    #region 泵设备标题
    /// <summary>
    /// 1#泵
    /// </summary>
    public virtual string Pump1Title => GetString("1#泵");

    /// <summary>
    /// 2#泵
    /// </summary>
    public virtual string Pump2Title => GetString("2#泵");

    /// <summary>
    /// 3#泵
    /// </summary>
    public virtual string Pump3Title => GetString("3#泵");

    /// <summary>
    /// 4#泵
    /// </summary>
    public virtual string Pump4Title => GetString("4#泵");
    #endregion

    #region 设备状态标签
    /// <summary>
    /// 远程
    /// </summary>
    public virtual string RemoteStatusLabel => GetString("远程 (Remote)");

    /// <summary>
    /// "本地 (Local)"
    /// </summary>
    public virtual string LocalStatusLabel => GetString("本地 (Local)");

    /// <summary>
    /// 停止运行
    /// </summary>
    public virtual string StopRunningStatus => GetString("停止运行");

    /// <summary>
    /// 启动运行
    /// </summary>
    public virtual string StartRunningStatus => GetString("启动运行");
    #endregion

    #region 运行参数标签
    /// <summary>
    /// 流量
    /// </summary>
    public virtual string FlowLabel => GetString("流量");

    /// <summary>
    /// 冲程
    /// </summary>
    public virtual string StrokeLabel => GetString("冲程 Stroke");

    /// <summary>
    /// 设置目标
    /// </summary>
    public virtual string SetTargetLabel => GetString("设置目标");

    /// <summary>
    /// 频率
    /// </summary>
    public virtual string FreqLabel => GetString("频率 Freq");

    /// <summary>
    /// 流量目标
    /// </summary>
    public virtual string FlowTargetLabel => GetString("流量目标");
    #endregion
}
public class PumpEnLang : PumpLang
{
    public static new PumpEnLang Instance { get; } = new ();

    public override string AppTitle => GetString("Pumps System - DoboColor");
    public override string ConnectionException => GetString("Connection Exception");

    public PumpLang Test => new PumpLang();

    #region 控制模式与顶部区域
    public override string ControlModeLabel => GetString("Control Mode:");

    public override string ManualParamMode => GetString("Parameter Mode");

    public override string FlowAutoMode => GetString("Flow Control");

    public override string AutoModeTip => GetString("*In auto mode, only set the flow rate");//other parameters are allocated by the system

    public override string ManualParamModeTip => GetString("*No flow rate setting is required in manual mode");

    public override string DisconnectBtnText => GetString("Disconnect");

    public override string ConnectBtnText => GetString("Connect");
    #endregion

    public override string LocalStatusLabel => GetString("Local");

    #region 泵设备标题
    public override string Pump1Title => GetString("Pump #1");

    public override string Pump2Title => GetString("Pump #2");

    public override string Pump3Title => GetString("Pump #3");

    public override string Pump4Title => GetString("Pump #4");
    #endregion

    #region 设备状态标签
    public override string RemoteStatusLabel => GetString("Remote");

    public override string StopRunningStatus => GetString("Stopped");

    public override string StartRunningStatus => GetString("Running");
    #endregion

    #region 运行参数标签
    public override string FlowLabel => GetString("Flow");

    public override string StrokeLabel => GetString("Stroke");

    public override string SetTargetLabel => GetString("Set Target");

    public override string FreqLabel => GetString("Frequency");

    public override string FlowTargetLabel => GetString("Flow Target");
    #endregion
}

public abstract class LangBase: INotifyPropertyChanged
{
    //public static AppLang Instance { get; } = new AppLang();

    private IStringLocalizer? _localizer;
    public event PropertyChangedEventHandler? PropertyChanged;

    

    public void Initialize(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    public virtual void ChangeLanguage2(CultureInfo culture)
    {
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
    public virtual void ChangeLanguage(CultureInfo culture)
    {
        culture = new CultureInfo(culture.Name);
        if (culture.Name.StartsWith("zh")) // 中文（简体/繁体）
        {
            culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-dd";    // 日期部分
            culture.DateTimeFormat.LongTimePattern = "HH:mm:ss";       // 时间部分
            culture.DateTimeFormat.FullDateTimePattern = "yyyy-MM-dd HH:mm:ss";
#if WPF
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
            }
#endif
        }
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
    public virtual void ChangeLanguage(string name)
    {
        ChangeLanguage(CultureInfo.GetCultureInfo(name));
    }
    /// <summary>
    /// 核心获取逻辑：带有后备默认值
    /// </summary>
    protected virtual string GetString(string defaultValue, [CallerMemberName] string key = "")
    {
        // 如果处于导出模式，直接收集字典并返回
        if (_isExporting)
        {
            _exportDict[key] = defaultValue;
            return defaultValue;
        }
      
        // 正常运行模式：尝试从外部多语言文件获取

        return GetLangVal(key) ?? defaultValue;
    }

    protected virtual string? GetLangVal(string key)
    {
        if (_localizer != null)
        {
            var localizedStr = _localizer[key];
            // ResourceNotFound 为 true 代表 JSON/Resx 文件里没配这个 key
            if (!localizedStr.ResourceNotFound)
            {
                return localizedStr.Value;
            }
        }
        return null;
    }


    private bool _isExporting = false;
    private Dictionary<string, string> _exportDict = new();
    /// <summary>
    /// 反向提取所有强类型属性及默认值，生成标准的 JSON 字符串
    /// </summary>
    public string GenerateTemplateJson()
    {
        _isExporting = true;
        _exportDict.Clear();

        // 1. 利用反射获取本类中所有 public string 类型的属性
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.PropertyType == typeof(string));

        // 2. 依次读取属性值 (这会触发属性的 Get 访问器，从而自动进入 GetString 收集字典)
        foreach (var prop in properties)
        {
            prop.GetValue(this);
        }

        _isExporting = false;

        // 3. 将收集到的字典序列化为 JSON
        // 设置: 缩进美化，并且不转义中文 (UnsafeRelaxedJsonEscaping)
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        return JsonSerializer.Serialize(_exportDict, options);
    }

}

