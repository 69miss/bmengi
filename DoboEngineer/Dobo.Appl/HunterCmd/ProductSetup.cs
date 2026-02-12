using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.HunterCmd;

/// <summary>
/// 产品设置数据结构（对应PRODUCT SETUP协议）
/// </summary>
public class ProductSetup
{
    public char? CmdHead { get; set; }

    #region 基础设置字段
    /// <summary>
    /// 设置编号（4字符，零基索引，"FFFF"表示当前设置）
    /// </summary>
    public string SetupNumber { get; set; } 

    /// <summary>
    /// 设置名称（可变长度，协议编码格式：3位16进制长度前缀+内容）
    /// </summary>
    public string SetupName { get; set; } = string.Empty;

    /// <summary>
    /// 标准类型（2字符，协议定义的类型标识）
    /// '0' --> PHYSICAL 
    /// '1' --> WORKING 
    /// '2' --> NUMERIC 
    /// '3' --> HITCH
    /// </summary>
    public string StandardType { get; set; } =string.Empty;

    /// <summary>
    /// 平均测量次数（2字符16进制ASCII，对应0-255）
    /// </summary>
    public byte AverageCount { get; set; } = 1;

    /// <summary>
    /// 自动搜索模式
    ///  Exclude = 0, // 排除在自动搜索之外
    /// </summary>
    public char AutoSearch { get; set; } = '0';

    /// <summary>
    /// 读取模式,TimedRead = 0, // 定时读取;1测量即读取  
    /// </summary>
    public char ReadMode { get; set; } ='0';

    /// <summary>
    /// 报告间隔（2字符16进制ASCII，单位：秒，0表示实时传输）
    /// </summary>
    public byte ReportInterval { get; set; } = 0;

    /// <summary>
    /// 传送带距离（8字符16进制ASCII，32位浮点数，单位：mm）
    /// </summary>
    public float BeltDistance { get; set; } = 0.0f;

    /// <summary>
    /// 最小产品高度（8字符16进制ASCII，32位浮点数，单位：mm）
    /// </summary>
    public float MinHeight { get; set; } = 0.0f;

    /// <summary>
    /// 最大产品高度（8字符16进制ASCII，32位浮点数，单位：mm）
    /// </summary>
    public float MaxHeight { get; set; } = 0.0f;

    /// <summary>
    /// 触发模式
    /// Disabled = 0,      // 禁用触发
    /// HeightTriggering = 1, // 高度触发
    /// ExternalTriggering = 2 // 外部触发 
    /// </summary>
    public char Triggering { get; set; }

    /// <summary>
    /// 滤波系数（8字符16进制ASCII，32位浮点数，IIR滤波器参数）
    /// </summary>
    public float FilterCoefficient { get; set; } = 0.0f;
    #endregion

    #region 光谱与颜色数据字段
    /// <summary>
    /// 标准样品光谱数据（248字符=62个浮点数，每个8字符，400-700nm反射率）
    /// </summary>
    public float[] SpectralData { get; set; }

    /// <summary>
    /// 目标XYZ值（24字符=3个浮点数，每个8字符，X/Y/Z各一个）
    /// </summary>
    public float[] TargetXyzValues { get; set; } 

    /// <summary>
    /// 测量XYZ值（24字符=3个浮点数，用于校准）
    /// </summary>
    public float[] MeasuredXyzValues { get; set; } 

    /// <summary>
    /// 上跟踪极限（24字符=3个浮点数，颜色跟踪窗口）
    /// </summary>
    public float[] UpperTrackingLimits { get; set; } 

    /// <summary>
    /// 下跟踪极限（24字符=3个浮点数，颜色跟踪窗口）
    /// </summary>
    public float[] LowerTrackingLimits { get; set; } 
    #endregion

    #region 容差报警字段
    /// <summary>
    /// 尺度上报警限（24字符=3个浮点数）
    /// </summary>
    public float[] ScaleUpperAlarm { get; set; } 

    /// <summary>
    /// 尺度上预警限（24字符=3个浮点数）
    /// </summary>
    public float[] ScaleUpperAlert { get; set; } 

    /// <summary>
    /// 尺度下预警限（24字符=3个浮点数）
    /// </summary>
    public float[] ScaleLowerAlert { get; set; } 

    /// <summary>
    /// 尺度下报警限（24字符=3个浮点数）
    /// </summary>
    public float[] ScaleLowerAlarm { get; set; } 

    /// <summary>
    /// 索引上报警限（24字符=3个浮点数）
    /// </summary>
    public float[] IndexUpperAlarm { get; set; } 

    /// <summary>
    /// 索引上预警限（24字符=3个浮点数）
    /// </summary>
    public float[] IndexUpperAlert { get; set; } 

    /// <summary>
    /// 索引下预警限（24字符=3个浮点数）
    /// </summary>
    public float[] IndexLowerAlert { get; set; } 

    /// <summary>
    /// 索引下报警限（24字符=3个浮点数）
    /// </summary>
    public float[] IndexLowerAlarm { get; set; } 
    #endregion

    #region 数据视图字段
    /// <summary>
    /// 数据视图数量（1字符，0-9）
    /// </summary>
    public int NumDataViews { get; set; } = 1;

    /// <summary>
    /// 数据视图编号（1字符，零基索引）
    /// </summary>
    public int DataViewNumber { get; set; } = 0;

    /// <summary>
    /// 是否启用视图（1字符，0=禁用，1=启用；第一个视图不可禁用）
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 显示类型（2字符，协议定义的显示模式）;
    /// '0' --> ABSOLUTE ;'1' --> DELTA ;'2' --> SPECTRAL DATA ;'3' --> SPECTRAL DIFF ;'4' --> SPECTRAL PLOT ;'5' --> DIFFERENCE PLOT ;'6' --> COLOR PLOT
    /// </summary>
    public byte DisplayType { get; set; }

    /// <summary>
    /// 照明/观察者类型（2字符，如D65/10°等）
    /// </summary>
    /// <remarks>
    /// '0' --> A/2°  ; '1' --> C/2°  ; '2' --> D50/2°  ; '3' --> D55/2°  ; '4' --> D65/2°  ; '5' --> D75/2°  ; '6' --> F2/2°  ; '7' --> F7/2°  ; '8' --> F11/2°  ; '9' --> A/10°  ;
    /// '10' --> C/10°  ; '11' --> D50/10°  ; '12' --> D55/10°  ; '13' --> D65/10°  ; '14' --> D75/10°  ; '15' --> F2/10°  ; '16' --> F7/10°  ; '17' --> F11/10°  ; 
    /// </remarks>
    public byte IllumObserver { get; set; }

    /// <summary>
    /// 颜色尺度（2字符，协议定义的颜色尺度）
    /// </summary>
    /// <remarks>
    /// '0' --> none  ; '1' --> CIE Lab  ; '2' --> CIE LCh  ; '3' --> Hunter Lab  ; '4' --> XYZ  ; '5' --> Yxy  ; '6' --> dLCH  ; 
    /// </remarks>
    public byte ColorScale { get; set; }

    /// <summary>
    /// 颜色索引（2字符，协议定义的颜色索引）
    /// </summary>
    /// <remarks>
    /// '0' : none  ; '1' : Y  ; '2' : YI D1925  ; '3' : YI E313  ; '4' : WI E313  ; '5' : Tint  ; '6' : Z%  ; '7' : BT457  ; '8' : BCU  ; '9' : Strength (max absorption)  ;
    /// '10' : Strength (weighted)  ; '21' : Gray Scale  ; '22' : Gray Stain  ; '23' : dE*  ; '24' : dEcmc  ; '25' : dE  ; '26' : dC*  ; '27' : dC  ; '28' : Shade Number  ; '29' : MI  ; 
    /// </remarks>
    public byte ColorIndex { get; set; } 

    /// <summary>
    /// 色阶块数量（1字符，允许值：1,3,5,7,9）
    /// </summary>
    public sbyte ShadeBlocks { get; set; } = 1;

    /// <summary>
    /// CMC商业因子（8字符16进制ASCII，32位浮点数）
    /// </summary>
    public float CmcCf { get; set; } = 1.0f;

    /// <summary>
    /// CMC L:C比率（8字符16进制ASCII，32位浮点数）
    /// </summary>
    public float CmcLcRatio { get; set; } = 2.0f;
    #endregion


    #region 序列化（转换为更新命令数据）
    /// <summary>
    /// 转换为更新命令的参数数据（符合HOST更新命令格式）
    /// 格式：'0' + SETUP NUMBER + 所有字段按协议顺序拼接
    /// </summary>
    public string ToUpdateCommandData()
    {
        // 验证关键字段长度
        if (SetupNumber.Length != 4)
            throw new ArgumentException("SetupNumber必须为4个字符", nameof(SetupNumber));
        if (StandardType.Length != 2)
            throw new ArgumentException("StandardType必须为2个字符", nameof(StandardType));

        // 拼接字段（按协议更新命令顺序）
        var parts = new List<string>
        {
            "0", // 更新标识（固定'0'）
            SetupNumber,
            SpectraParser.EncodeString(SetupName), // 名称（带长度前缀）
            StandardType,
            AverageCount.ToString("X2"), // 平均次数（2字符16进制）
            AutoSearch.ToString(), // 自动搜索（1字符）
            ReadMode.ToString(), // 读取模式（1字符）
            ReportInterval.ToString("X2"), // 报告间隔（2字符16进制）
            SpectraParser.FloatToHexAscii(BeltDistance), // 传送带距离（8字符）
            SpectraParser.FloatToHexAscii(MinHeight), // 最小高度（8字符）
            SpectraParser.FloatToHexAscii(MaxHeight), // 最大高度（8字符）
            (Triggering).ToString(), // 触发模式（1字符）
            SpectraParser.FloatToHexAscii(FilterCoefficient), // 滤波系数（8字符）
            GetFloatArrayAsHex(SpectralData), // 光谱数据（248字符）
            GetFloatArrayAsHex(TargetXyzValues), // 目标XYZ（24字符）
            GetFloatArrayAsHex(MeasuredXyzValues), // 测量XYZ（24字符）
            GetFloatArrayAsHex(UpperTrackingLimits), // 上跟踪极限（24字符）
            GetFloatArrayAsHex(LowerTrackingLimits), // 下跟踪极限（24字符）
            GetFloatArrayAsHex(ScaleUpperAlarm), // 尺度上报警（24字符）
            GetFloatArrayAsHex(ScaleUpperAlert), // 尺度上预警（24字符）
            GetFloatArrayAsHex(ScaleLowerAlert), // 尺度下预警（24字符）
            GetFloatArrayAsHex(ScaleLowerAlarm), // 尺度下报警（24字符）
            GetFloatArrayAsHex(IndexUpperAlarm), // 索引上报警（24字符）
            GetFloatArrayAsHex(IndexUpperAlert), // 索引上预警（24字符）
            GetFloatArrayAsHex(IndexLowerAlert), // 索引下预警（24字符）
            GetFloatArrayAsHex(IndexLowerAlarm), // 索引下报警（24字符）
            NumDataViews.ToString(), // 数据视图数量（1字符）
            DataViewNumber.ToString(), // 数据视图编号（1字符）
            Enabled ? "1" : "0", // 是否启用（1字符）
            SpectraParser.BaseToHexAscii(DisplayType), // 显示类型（2字符）
            SpectraParser.BaseToHexAscii(IllumObserver), // 照明/观察者（2字符）
            SpectraParser.BaseToHexAscii(ColorScale), // 颜色尺度（2字符）
            SpectraParser.BaseToHexAscii(ColorIndex), // 颜色索引（2字符）
            ShadeBlocks.ToString(), // 色阶块数量（1字符）
            SpectraParser.FloatToHexAscii(CmcCf), // CMC CF（8字符）
            SpectraParser.FloatToHexAscii(CmcLcRatio) // CMC L:C（8字符）
        };

        return string.Concat(parts);
    }

    /// <summary>
    /// 将光谱数据数组转换为248字符的16进制ASCII
    /// </summary>
    private string GetSpectralDataAsHex()
    {
        
        return GetFloatArrayAsHex(SpectralData);
    }

    /// <summary>
    /// 将浮点数数组转换为16进制ASCII字符串（每个浮点数8字符）
    /// </summary>
    private string GetFloatArrayAsHex(float[] values)
    {
        var hex = new System.Text.StringBuilder();
        foreach (var val in values)
        {
            
            hex.Append(SpectraParser.FloatToHexAscii(val));
        }
        return hex.ToString();
    }
    #endregion


    #region 反序列化（从查询响应解析）
    /// <summary>
    /// 从查询响应数据解析为ProductSetup对象（符合STAP查询响应格式）
    /// </summary>
    public static ProductSetup FromQueryResponseData(string responseData)
    {
        var parser=SpectraParser.Read(responseData);
        //var cmd= parser.readCmdChar();
        var setup = new ProductSetup();
        setup.CmdHead = 'P';

        // 1. SetupNumber（4字符）
        setup.SetupNumber = parser.readCmdChar(4);
         

        // 2. SetupName（3位16进制长度前缀 + 内容）
        setup.SetupName = parser.StringItem;

        // 3. StandardType（2字符）
        setup.StandardType = parser.readCmdChar (2);

        // 4. AverageCount（2字符16进制 → int）
        setup.AverageCount = parser.ByteItem;

        // 5. AutoSearch（1字符 → 枚举）
        setup.AutoSearch =parser.readCmdChar();

        // 6. ReadMode（1字符 → 枚举）
        setup.ReadMode = parser.readCmdChar();

        // 7. ReportInterval（2字符16进制 → int）
        setup.ReportInterval =parser.ByteItem;


        // 8. BeltDistance（8字符 → float）
        setup.BeltDistance = parser.SingleItem;

        // 9. MinHeight（8字符 → float）
        setup.MinHeight = parser.SingleItem;

        // 10. MaxHeight（8字符 → float）
        setup.MaxHeight = parser.SingleItem;

        // 11. Triggering（1字符 → 枚举）
        setup.Triggering = parser.readCmdChar();

        // 12. FilterCoefficient（8字符 → float）
        setup.FilterCoefficient =parser.SingleItem;


        // 13. SpectralData（248字符 → 62个float）
        setup.SpectralData = parser.get_SingleArray(31);//  ParseFloatArray(responseData.Substring(pos, 248), 62);

        // 14. TargetXyzValues（24字符 → 3个float）
        setup.TargetXyzValues = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);
 

        // 15. MeasuredXyzValues（24字符 → 3个float）
        setup.MeasuredXyzValues = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);

        // 16. UpperTrackingLimits（24字符 → 3个float）
        setup.UpperTrackingLimits = parser.get_SingleArray(3);//    ParseFloatArray(responseData.Substring(pos, 24), 3);
     

        // 17. LowerTrackingLimits（24字符 → 3个float）
        setup.LowerTrackingLimits = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);
    

        // 18. ScaleUpperAlarm（24字符 → 3个float）
        setup.ScaleUpperAlarm = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);

        // 19. ScaleUpperAlert（24字符 → 3个float）
        setup.ScaleUpperAlert = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);

        // 20. ScaleLowerAlert（24字符 → 3个float）
        setup.ScaleLowerAlert = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);


        // 21. ScaleLowerAlarm（24字符 → 3个float）
        setup.ScaleLowerAlarm = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);

        // 22. IndexUpperAlarm（24字符 → 3个float）
        setup.IndexUpperAlarm = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);
       

        // 23. IndexUpperAlert（24字符 → 3个float）
        setup.IndexUpperAlert = parser.get_SingleArray(3);//550 ParseFloatArray(responseData.Substring(pos, 24), 3);
       

        // 24. IndexLowerAlert（24字符 → 3个float）
        setup.IndexLowerAlert = parser.get_SingleArray(3);// ParseFloatArray(responseData.Substring(pos, 24), 3);
        

        // 25. IndexLowerAlarm（24字符 → 3个float）
        setup.IndexLowerAlarm = parser.get_SingleArray(3); //ParseFloatArray(responseData.Substring(pos, 24), 3);
   

        // 26. NumDataViews（1字符 → int）
        setup.NumDataViews = parser.NibbleItem;


        // 27. DataViewNumber（1字符 → int）
        setup.DataViewNumber = parser.NibbleItem;
    

        // 28. Enabled（1字符 → bool）
        setup.Enabled = parser.readCmdChar() == '1';
    

        // 29. DisplayType（2字符）
        setup.DisplayType =parser.ByteItem;
        

        // 30. IllumObserver（2字符）
        setup.IllumObserver = parser.ByteItem;
        

        // 31. ColorScale（2字符）
        setup.ColorScale = parser.ByteItem;
        

        // 32. ColorIndex（2字符）
        setup.ColorIndex =parser.ByteItem;
        

        // 33. ShadeBlocks（1字符 → int）
        setup.ShadeBlocks =parser.NibbleItem;
      

        // 34. CmcCf（8字符 → float）
        setup.CmcCf =parser.SingleItem;
      

        // 35. CmcLcRatio（8字符 → float）
        setup.CmcLcRatio =parser.SingleItem;
      

        return setup;
    }

    /// <summary>
    /// 将16进制ASCII字符串解析为浮点数数组（每个浮点数8字符）
    /// </summary>
    private static float[] ParseFloatArray(string hexStr, int expectedCount)
    {
        if (hexStr.Length != expectedCount * 8)
            throw new ArgumentException($"浮点数数组字符串长度应为{expectedCount * 8}，实际为{hexStr.Length}");

        var values = new float[expectedCount];
        for (int i = 0; i < expectedCount; i++)
        {
            string floatHex = hexStr.Substring(i * 8, 8);
            values[i] = SpectraParser.HexAsciiToFloat(floatHex);
        }
        return values;
    }
    #endregion
}

