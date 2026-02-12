using Dobo.Appl.Utility;
using System.ComponentModel;
using System.Reflection.Metadata;

namespace Dobo.Appl.SPC100;

public class SpcStateInfo:INotifyPropertyChangedExt2
{
    private readonly byte b1;
    private readonly byte b2;
    private readonly byte b3High;
    private readonly byte b4Low;
    private bool paperBreakSignal = true;
    private bool lowerMachineSignal = false;

    public event PropertyChangedEventHandler? PropertyChanged;
    public INotifyPropertyChangedExt2 NotifyGet()
    {

        return this;
    }

    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
    public SpcStateInfo()
    {
    }
    public SpcStateInfo(byte b1, byte b2, byte b3high, byte b4low)
    {
        this.b1 = b1;
        this.b2 = b2;
        b3High = b3high;
        b4Low = b4low;
        //
        State1FlagsEntity(b1);
        State2FlagsEntity(b2);
        TemperatureFromByte(b3high, b4low);
    }
    public void ToSpcStateInfo(SpcStateInfo spcStateInfo) {

        spcStateInfo.State1FlagsEntity(b1);
        spcStateInfo.State2FlagsEntity(b2);
        spcStateInfo.TemperatureFromByte(b3High, b4Low);
    }
    public static SpcStateInfo FromBit(byte b1, byte b2, byte b3high, byte b4low)
    {
        return new SpcStateInfo(b1,b2,b3high,b4low);
    }
    public float Temperature { get; set; }
    /// <summary>
    /// 拟合结果，15~70度内较准确
    /// </summary>
    /// <param name="high"></param>
    /// <param name="low"></param>
    /// <returns></returns>
    public float TemperatureFromByte(byte high, byte low)
    {
        var adc = high * 256 + low;
        var v = adc * 3.3f / 1023;
        Temperature = 95.626f - 32.658f * v;

        return Temperature;
    }
    #region State2Flags
    /// <summary>
    /// 托纸臂磁开关+光机气缸磁开关 (原Bit0，对应枚举PaperArmAndCylinder=0x01)
    /// </summary>
    public bool PaperArmAndCylinder { get; set; } = false;

    /// <summary>
    /// 自动状态 (原Bit1，对应枚举AutoStatus=1《《1； 
    /// </summary>
    public bool AutoStatus { get; set; } = false;

    /// <summary>
    /// 断纸信号 (原Bit2，对应枚举PaperBreakSignal=1《《2； 
    /// </summary>
    public bool PaperBreakSignal { get => paperBreakSignal; set => NotifyGet().SetField(ref  paperBreakSignal , value); }
    /// <summary>
    /// 下机信号 (原Bit3，对应枚举LowerMachineSignal=1《《3；
    /// </summary>
    public bool LowerMachineSignal { get => lowerMachineSignal; set => NotifyGet().SetField(ref lowerMachineSignal , value); }
    /// <summary>
    /// 未定义位 (原Bit4，对应枚举s4=1<<4)
    /// </summary>
    public bool UndefinedBit4 { get; set; } = false;

    /// <summary>
    /// 托纸臂磁开关 (原Bit5，对应枚举PaperArmMagnetic=1《《5；
    /// </summary>
    public bool PaperArmMagnetic { get; set; } = false;

    /// <summary>
    /// 未定义位 (原Bit6，对应枚举s6=1<<6)
    /// </summary>
    public bool UndefinedBit6 { get; set; } = false;

    /// <summary>
    /// 光机气缸磁开关 (原Bit7，对应枚举LightCylinderMagnetic=1《《7；枚举注释标注Bit7，与实际位值一致)
    /// </summary>
    public bool LightCylinderMagnetic { get; set; } = false;
 
    /// <summary>
    /// 从byte数据初始化（解析8个Bit位到对应属性）
    /// 适配场景：Modbus读取、二进制数据解析等外部传入byte的场景
    /// </summary>
    /// <param name="data">待解析的byte数据（Bit0~Bit7对应8个属性）</param>
    public void State2FlagsEntity(byte data)
    {
        // 硬编码位掩码（与原枚举位值完全一致，不依赖枚举）
        PaperArmAndCylinder = (data & 0x01) != 0;  // Bit0: 0x01 = 0x01（枚举原值）
        AutoStatus = (data & 0x02) != 0;           // Bit1: 0x02 = 1<<1（枚举原值）
        PaperBreakSignal = (data & 0x04) != 0;     // Bit2: 0x04 = 1<<2（枚举原值）
        LowerMachineSignal = (data & 0x08) != 0;   // Bit3: 0x08 = 1<<3（枚举原值）
        UndefinedBit4 = (data & 0x10) != 0;        // Bit4: 0x10 = 1<<4（枚举原值）
        PaperArmMagnetic = (data & 0x20) != 0;     // Bit5: 0x20 = 1<<5（枚举原值）
        UndefinedBit6 = (data & 0x40) != 0;        // Bit6: 0x40 = 1<<6（枚举原值）
        LightCylinderMagnetic = (data & 0x80) != 0;// Bit7: 0x80 = 1<<7（枚举原值）
    }
    public byte ToByte1()
    {
        byte result = 0;

        // 根据属性状态设置对应Bit位（true=1，false=0）
        if (PaperArmAndCylinder) result |= 0x01;
        if (AutoStatus) result |= 0x02;
        if (PaperBreakSignal) result |= 0x04;
        if (LowerMachineSignal) result |= 0x08;
        if (UndefinedBit4) result |= 0x10;
        if (PaperArmMagnetic) result |= 0x20;
        if (UndefinedBit6) result |= 0x40;
        if (LightCylinderMagnetic) result |= 0x80;

        return result;
    }
    #endregion


    #region  State1Flags
    /// <summary>
    /// 软件兼容性需求位 (原Bit0)
    /// </summary>
    public bool SoftwareCompatBit { get; set; } = false;

    /// <summary>
    /// 码头开关 (原Bit1)
    /// </summary>
    public bool DockSwitch { get; set; } = false;

    /// <summary>
    /// 黑板位置 (原Bit2)
    /// </summary>
    public bool BlackBoardPosition { get; set; } = false;

    /// <summary>
    /// 白板位置 (原Bit3)
    /// </summary>
    public bool WhiteBoardPosition { get; set; } = false;

    /// <summary>
    /// 绿板位置 (原Bit4)
    /// </summary>
    public bool GreenBoardPosition { get; set; } = false;

    /// <summary>
    /// SUB_IN辅助开关 (原Bit5)
    /// </summary>
    public bool SubInAuxSwitch { get; set; } = false;

    /// <summary>
    /// 校准开关 (原Bit6)
    /// </summary>
    public bool CalibrationSwitch { get; set; } = false;

    /// <summary>
    /// 未定义位 (原Bit7)
    /// </summary>
    public bool UndefinedBit7 { get; set; } = false;

    /// <summary>
    /// 从byte数据初始化（解析8个Bit位到对应属性）
    /// 适配场景：Modbus读取、二进制数据解析等外部传入byte的场景
    /// </summary>
    /// <param name="data">待解析的byte数据（Bit0~Bit7对应8个属性）</param>
    public void State1FlagsEntity(byte data)
    {
        // 直接通过位运算解析，不依赖任何枚举
        SoftwareCompatBit = (data & 0x01) != 0; // Bit0: 0x01 = 1<<0
        DockSwitch = (data & 0x02) != 0;       // Bit1: 0x02 = 1<<1
#if !v2dev
        BlackBoardPosition = (data & 0x04) != 0;// Bit2: 0x04 = 1<<2
        WhiteBoardPosition = (data & 0x08) != 0;// Bit3: 0x08 = 1<<3
#endif
#if v2dev
        BlackBoardPosition = (data & 0x08) != 0;// Bit2: 0x04 = 1<<2
        WhiteBoardPosition = (data & 0x04) != 0;// Bit3: 0x08 = 1<<3
#endif
        GreenBoardPosition = (data & 0x10) != 0;// Bit4: 0x10 = 1<<4
        SubInAuxSwitch = (data & 0x20) != 0;   // Bit5: 0x20 = 1<<5
        CalibrationSwitch = (data & 0x40) != 0;// Bit6: 0x40 = 1<<6
        UndefinedBit7 = (data & 0x80) != 0;    // Bit7: 0x80 = 1<<7
    }
    /// <summary>
    /// 将所有属性状态转换为byte数据（Bit0~Bit7对应8个属性）
    /// 适配场景：Modbus写入、二进制数据存储等需要输出byte的场景
    /// </summary>
    /// <returns>包含所有状态的byte值</returns>
    public byte ToByte2()
    {
        byte result = 0;

        // 根据属性状态设置对应Bit位（true=1，false=0）
        if (SoftwareCompatBit) result |= 0x01;
        if (DockSwitch) result |= 0x02;
#if !v2dev
        if (BlackBoardPosition) result |= 0x04;
        if (WhiteBoardPosition) result |= 0x08;
#endif
#if v2dev
        if (BlackBoardPosition) result |= 0x08;
        if (WhiteBoardPosition) result |= 0x04;
#endif
        if (GreenBoardPosition) result |= 0x10;
        if (SubInAuxSwitch) result |= 0x20;
        if (CalibrationSwitch) result |= 0x40;
        if (UndefinedBit7) result |= 0x80;
        return result;
    }
#endregion

    public override string ToString()
    {
        return $"软件兼容性需求位[SoftwareCompatBit]={SoftwareCompatBit}，" +
               $"码头开关[DockSwitch]={DockSwitch}，" +
               $"黑板位置[BlackBoardPosition]={BlackBoardPosition}，" +
               $"白板位置[WhiteBoardPosition]={WhiteBoardPosition}，" +
               $"绿板位置[GreenBoardPosition]={GreenBoardPosition}，" +
               $"SUB_IN辅助开关[SubInAuxSwitch]={SubInAuxSwitch}，" +
               $"校准开关[CalibrationSwitch]={CalibrationSwitch}，" +
               $"未定义位(Bit7)[UndefinedBit7]={UndefinedBit7} | " +
               //
               $"托纸臂磁开关+光机气缸磁开关[PaperArmAndCylinder]={PaperArmAndCylinder}，" +
               $"自动状态[AutoStatus]={AutoStatus}，" +
               $"断纸信号[PaperBreakSignal]={PaperBreakSignal}，" +
               $"下机信号[LowerMachineSignal]={LowerMachineSignal}，" +
               $"未定义位(Bit4)[UndefinedBit4]={UndefinedBit4}，" +
               $"托纸臂磁开关[PaperArmMagnetic]={PaperArmMagnetic}，" +
               $"未定义位(Bit6)[UndefinedBit6]={UndefinedBit6}，" +
               $"光机气缸磁开关[LightCylinderMagnetic]={LightCylinderMagnetic}，"+
               //
               $"温度[Temperature]={Temperature}";
    }
    public SpcStateInfo By(SpcStateInfo stateStruct)
    {
        // 1. 映射 State1Flags 相关属性（原结构 State1Flags 区域）
        SoftwareCompatBit = stateStruct.SoftwareCompatBit;
        DockSwitch = stateStruct.DockSwitch;
        BlackBoardPosition = stateStruct.BlackBoardPosition;
        WhiteBoardPosition = stateStruct.WhiteBoardPosition;
        GreenBoardPosition = stateStruct.GreenBoardPosition;
        SubInAuxSwitch = stateStruct.SubInAuxSwitch;
        CalibrationSwitch = stateStruct.CalibrationSwitch;
        UndefinedBit7 = stateStruct.UndefinedBit7;

        // 2. 映射 State2Flags 相关属性（原结构 State2Flags 区域）
        PaperArmAndCylinder = stateStruct.PaperArmAndCylinder;
        AutoStatus = stateStruct.AutoStatus;
        PaperBreakSignal = stateStruct.PaperBreakSignal;
        LowerMachineSignal = stateStruct.LowerMachineSignal;
        UndefinedBit4 = stateStruct.UndefinedBit4;
        PaperArmMagnetic = stateStruct.PaperArmMagnetic;
        UndefinedBit6 = stateStruct.UndefinedBit6;
        LightCylinderMagnetic = stateStruct.LightCylinderMagnetic;

        // 3. 映射温度属性
        Temperature = stateStruct.Temperature;

        RefreshTime = DateTimeOffset.Now;
        return this;
    }
    public DateTimeOffset RefreshTime { get; set; }
    
}


#region enum


public enum IOFunctionCode : byte
{
    FlashCalibrationLight = 0x01,   // 闪校准灯
    v2 = 2,
    CalibrationLightOn = 0x03,      // 校准灯常亮
    v4 = 4,
    OpenLensCover = 0x05,           // 开镜头盖
#if !v2dev  
    CloseLensCover = 0x06,          // 关镜头盖
    MoveToWhitePosition = 0x07,     // 到白色位置
#endif
#if v2dev
    MoveToWhitePosition = 0x06,          // 关镜头盖
    CloseLensCover = 0x07,     // 到白色位置
#endif
    PowerOffLightMachine = 0x08,    // 断光机电源
    PowerOnLightMachine = 0x09,     // 合光机电源
    TriggerDetection = 10         // 触发一次检测
}
/// <summary>
/// 文档顺序错误 
/// </summary>
[Flags]
public enum State1Flags : byte
{
    PaperArmAndCylinder = 0x01, // 托纸臂磁开关+光机气缸磁开关
    AutoStatus = 1 << 1,          // 自动状态 (Bit3)
    PaperBreakSignal = 1 << 2,    // 断纸信号 (Bit4)
    LowerMachineSignal = 1 << 3,  // 下机信号 (Bit5)
    s4 = 1 << 4,
    PaperArmMagnetic = 1 << 5,    // 托纸臂磁开关 (Bit6)
    s6 = 1 << 6,
    LightCylinderMagnetic = 1 << 7, // 光机气缸磁开关 (Bit7)

    // 组合状态（文档中Bit0-2未定义，可能是预留）

}

/// <summary>
/// 文档顺序错误 
/// </summary>
[Flags]
public enum State2Flags : byte
{
    SoftwareCompatBit = 1 << 0,   // 软件兼容性需求位 (Bit0)
    DockSwitch = 1 << 1,          // 码头开关 (Bit1)
    BlackBoardPosition = 1 << 2,  // 黑板位置 (Bit2)
    WhiteBoardPosition = 1 << 3,  // 白板位置 (Bit3)
    GreenBoardPosition = 1 << 4,  // 绿板位置 (Bit4)
    SubInAuxSwitch = 1 << 5,      // SUB_IN辅助开关 (Bit5)
    CalibrationSwitch = 1 << 6,   // 校准开关 (Bit6)
    f7 = 1 << 7// Bit7 未定义
}
#endregion