using Dobo.Appl.Utility;
using Jint.Native;
using PumpsSystem.Pump; // 引用原有的基础接口如 IDataItemProp
using System;
using System.ComponentModel;

namespace PumpsSystem.Pump2;

/// <summary>
/// 结构化的泵站 Modbus 点位上下文
/// 将单一泵的所有相关的状态、控制和模拟量全部集中管理
/// </summary>
public class PumpItem
{
    public string PumpId {  get; set; }
    // === 状态位映射 (Bit) ===
    public DataItemProp<bool> IsRemote { get; set; }
    public DataItemProp<bool> IsFault { get; set; }
    public DataItemProp<bool> IsRunning { get; set; }

    // === 控制位映射 (Bit) ===
    public DataItemProp<bool> CtlRunning { get; set; }

    // === 模式位映射 (Bit) ===
    public DataItemProp<bool> ModeFlow { get; set; }
    public DataItemProp<bool> ModeManual { get; set; }

    // === 模拟量映射 (Word) ===
    public IDataItemProp FreqPV { get; set; }
    public IDataItemProp StrokePV { get; set; }
    public IDataItemProp FlowPV { get; set; }

    public IDataItemProp MaxFlowSV { get; set; }
    public IDataItemProp FreqSV { get; set; }
    public IDataItemProp StrokeSV { get; set; }
    public IDataItemProp FlowSV { get; set; }

    public IDataItemProp MaxStroke { get; set; }
    public IDataItemProp MinStroke { get; set; }
}
