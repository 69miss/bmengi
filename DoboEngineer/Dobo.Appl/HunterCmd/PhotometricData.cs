using System;

namespace Dobo.Appl.HunterCmd;

/// <summary>
/// 测量数据传输实体类
/// </summary>
public class PhotometricData
{
    /// <summary>
    /// 状态:'FFFF' – 光度数据不可用,'4000' – 暗扫描失败,'2000' – 信号扫描失败,'1000' – 监控信号低,'0800' – 最低信号高,'0400' – 顶级信号低电平,'0080' – 未读取刻度底部,'0040' – 未读取顶部,'0002' – 范围太近,'0001' – 范围太远
    /// </summary>
    public ushort Status { get; set; }

    /// <summary>
    /// 报告编号,0单反射率时无此数据
    /// </summary>
    public int? ReportNumber { get; set; }

    /// <summary>
    /// 测量数量
    /// </summary>
    public ushort? NumberOfMeasurements { get; set; }

    /// <summary>
    /// 颜色状态:0 --> 在公差范围内,1 -->超出警报容差,2 -->超出报警容差
    /// </summary>
    public char? ColorStatus { get; set; }

    /// <summary>
    /// 尺度数据,长度3，xyz数据
    /// </summary>
    public float[]? ScaleData { get; set; }
    /// <summary>
    /// 反射率数据，31
    /// </summary>
    public float[]? Data { get; set; }
    /// <summary>
    /// 索引数据，xyz数据才有
    /// </summary>
    public int? IndexData { get; set; }

    /// <summary>
    /// 高度,样本计算高度
    /// </summary>
    public float? Height { get; set; }
    /// <summary>
    /// 到样本的测量距离，0单次测量才有
    /// </summary>
    public float? Distance { get; set; }

    /// <summary>
    /// 扩展标记
    /// </summary>
    public string ExTag { get; set; }
    /// <summary>
    /// 判断数据类型：0单反射率（DATA,DISTANCE），1反射率(DATA)，2三刺激值(SCALE DATA,INDEX DATA)，3无数据
    /// </summary>
    /// <param name="rawStr">检测的元素数据字符，使用长度判断；null则使用实体属性判断</param>
    /// <returns></returns>
    public static sbyte DetectDataType(string rawStr)
    {
        var len = rawStr.Length;
        switch (len)
        {
            case >=273:
                return 1;
            case >=260:
                return 0;
            case >=57:
                return 2;
            case >=12:
                return 3;
            default:
                return -1;
        }
    }
    string rawDataString;
    /// <summary>
    /// 判断数据类型：0单反射率（DATA,DISTANCE），1反射率(DATA)，2三刺激值(SCALE DATA,INDEX DATA)，3无数据
    /// </summary>
    /// <returns></returns>
    public sbyte DetectDataType()
    {
        if (ReportNumber == null)
            return 0;
        if (Data != null)
            return 1;
        if (ScaleData != null)
            return 2;
        return 3;
    }
    public static PhotometricData FromStr(string str,sbyte? type=null)
    {
          type =type??DetectDataType(str);
        var parser = SpectraParser.Read(str);
        var re = new PhotometricData();
        re.Status = parser.UnsignedItem;
        re.rawDataString = str;
        if (re.Status != 0)
            return re;
        if (type == 0)
        {
            re.Data = parser.get_SingleArray(31);
            re.Distance = parser.SingleItem;
            return re;
        }
        re.ReportNumber = parser.LongItem;
        if (type == 3)
        {
            return re;
        }
        re.NumberOfMeasurements = parser.UnsignedItem;
        re.ColorStatus = parser.readCmdChar();
        if (type == 1)
        {
            re.Data = parser.get_SingleArray(31);
            re.Height = parser.SingleItem;
            return re;
        }
        if (type == 2)
        {
            re.ScaleData = parser.get_SingleArray(3);
            re.IndexData = parser.LongItem;
            re.Height = parser.SingleItem;
            return re;
        }
        throw new ArgumentException("无法解析");
    }
}