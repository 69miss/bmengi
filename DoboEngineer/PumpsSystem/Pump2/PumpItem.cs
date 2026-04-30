using Dobo.Appl.Utility;
using Jint.Native;
using PumpsSystem.Pump; // 引用原有的基础接口如 IDataItemBase
using System;
using System.ComponentModel;

namespace PumpsSystem.Pump2;

/// <summary>
/// Modbus 寄存器位映射器：绑定具体的寄存器对象和它对应的位(Bit)
/// </summary>
public class DataItemBitMap: INotifyPropertyChangedExt2
{
    public IDataItemBase Register { get; }
    public int BitIndex { get; }

    public DataItemBitMap(IDataItemBase register, int bitIndex,string name)
    {
        Register = register;
        BitIndex = bitIndex;
        Name = name;
    }

    /// <summary>
    /// 读取当前位的 Bool 值
    /// </summary>
    public bool Value
    {
        get
        {
            if (Register?.Value == null) return false;
            int val = Register.Value.ToInt32(null);
            return (val & (1 << BitIndex)) != 0;
        }
        set
        {

            var newVal = CalculateNewValue(value);
            if (!newVal.Equals(Register.Value))
            {
                Register.Value = newVal;
                NotifyThis.OnPropertyChanged();
            }
        }
    }

    public ushort Address { get => Register.Address; set => throw new NotImplementedException(); }
    public string Name { get; set; }
    //IConvertible IDataItemBase.Value { get => Value; set => Value = (bool)value; }
    public bool CanWrite { get => Register.CanWrite; set => throw new NotImplementedException(); }
   
    private string _inputValue;  
    public string InputValue { get => _inputValue; set => NotifyThis.SetField(ref _inputValue, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
    /// <summary>
    /// 根据期望的 bool 状态，计算并返回该寄存器修改后的 ushort 完整值（用于下发指令）
    /// </summary>
    public ushort CalculateNewValue(bool targetState)
    {
        int val = Register?.Value != null ? Register.Value.ToInt32(null) : 0;
        if (targetState)
            val |= (1 << BitIndex);
        else
            val &= ~(1 << BitIndex);

        return (ushort)val;
    }
}

/// <summary>
/// 结构化的泵站 Modbus 点位上下文
/// 将单一泵的所有相关的状态、控制和模拟量全部集中管理
/// </summary>
public class PumpItem
{
    // === 状态位映射 (Bit) ===
    public DataItemBitMap IsRemote { get; set; }
    public DataItemBitMap IsFault { get; set; }
    public DataItemBitMap IsRunning { get; set; }

    // === 控制位映射 (Bit) ===
    public DataItemBitMap CtlRunning { get; set; }

    // === 模式位映射 (Bit) ===
    public DataItemBitMap ModeFlow { get; set; }
    public DataItemBitMap ModeManual { get; set; }

    // === 模拟量映射 (Word) ===
    public IDataItemBase FreqPV { get; set; }
    public IDataItemBase StrokePV { get; set; }
    public IDataItemBase FlowPV { get; set; }

    public IDataItemBase MaxFlowSV { get; set; }
    public IDataItemBase FreqSV { get; set; }
    public IDataItemBase StrokeSV { get; set; }
    public IDataItemBase FlowSV { get; set; }

    public IDataItemBase MaxStroke { get; set; }
    public IDataItemBase MinStroke { get; set; }
}