using Dobo.Appl.Utility;
using Jint.Native;
using PumpsSystem.Pump;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSystem.Pump2;
public interface IDataItemProp : IDataItemBase, INotifyPropertyChanged
{
    public string Address { get; set; }
    public string Name { get; set; }
    public IConvertible Value { get; set; }
    public bool CanWrite { get;  }
    public TypeCode TypeCode { get;}
}
public class DataItemProp<T> : DataItemProp where T : IConvertible
{
    TypeCode typeCode;
    public override TypeCode TypeCode => typeCode;
    public DataItemProp()
    {
        typeCode=Type.GetTypeCode(typeof(T));
    }
    
    public virtual T Value { get { return (T)Convert.ChangeType(base.Value, typeof(T));  } set { base.Value = value; } }
}
public class DataItemPropDto
{
    public string Address { get; set; }
    public string Name { get; set; }
    public object Value { get; set; }
    public bool CanWrite { get; set; }
    public TypeCode TypeCode { get; set; }

    public string Register{get;set;}


}
public class DataItemProp : IDataItemProp, INotifyPropertyChangedExt2
{

    private IConvertible _value;
    TypeCode typeCode;
    public DataItemProp() { 
    }
    public DataItemProp(TypeCode type)
    {
        typeCode = type;
    }
    public DataItemProp(IConvertible valDef)
    {
        if (valDef == null)
            throw new ArgumentNullException(nameof(valDef));
        typeCode = valDef.GetTypeCode();
        Value = valDef;
    }
  
    public virtual string Address { get; set; }
    public virtual string Name { get; set; }
    public virtual IConvertible Value { get => _value; set => NotifyThis.SetField(ref _value, value); }
    public virtual bool CanWrite { get; set; }

    private string _inputValue;
    protected INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public virtual string InputValue { get => _inputValue; set => NotifyThis.SetField(ref _inputValue, value); }

    public virtual TypeCode TypeCode => typeCode;

    ushort IDataItemBase.Address { get =>(ushort)GetHashCode(); set => Address=value.ToString(); }

    public event PropertyChangedEventHandler? PropertyChanged;
    public virtual PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
}
public interface IDataItemPropWrap
{
    public IDataItemProp Register { get; }
}
public class DataItemPropMap<T> : DataItemProp<T>, IDataItemPropWrap where T : IConvertible
{
    public override bool CanWrite { get => Register.CanWrite; set => throw new NotImplementedException(); }
    public override string Address { get => Register.Address; set => throw new NotImplementedException(); }
    public DataItemPropMap(IDataItemProp register, Func<IDataItemProp, T> getFunc, Action<IDataItemProp, T> setFunc = null)
    {
        Register = register;
        GetFunc = getFunc;
        SetFunc = setFunc;
        Register.PropertyChanged += Register_PropertyChanged;
    }

    private void Register_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Register.Value))//isInternalSet ||
            return;

        var val = Value;
        Console.WriteLine(val);
    }

    public IDataItemProp Register { get; }
    public Func<IDataItemProp, T> GetFunc { get; private set; }
    public Action<IDataItemProp, T> SetFunc { get; private set; }
    T internalVal;
    public override T Value
    {
        get
        {
            var val = GetFunc(Register);
            NotifyThis.SetField(ref internalVal, val);
            return internalVal;
        }
        set {
            if (EqualityComparer<T>.Default.Equals(internalVal, value))
                return;
              SetFunc(Register, value); 
        }
    }

}

public class DataItemToBitMap : DataItemPropMap<bool>
{
    /// <summary>
    /// 只使用单个true或falses时，赋值时要对应单个
    /// </summary>
    /// <param name="register"></param>
    /// <param name="trueVal"></param>
    /// <param name="falseVal"></param>
    public DataItemToBitMap(IDataItemProp register, int? trueVal, int? falseVal=null) : base(register,
        p => {
            if (p.Value == null)
                return false;
            if (trueVal != null && falseVal == null)
                return EqualityComparer<int>.Default.Equals(p.Value.ToInt32(null), trueVal.Value);
            else if (trueVal == null && falseVal != null)
                return !EqualityComparer<int>.Default.Equals(p.Value.ToInt32(null), falseVal.Value);
            else
            {
                if (EqualityComparer<int>.Default.Equals(p.Value.ToInt32(null), trueVal.Value))
                    return true;
                if (EqualityComparer<int>.Default.Equals(p.Value.ToInt32(null), falseVal.Value))
                    return false;
                return false;
            }
        }, (p,v) =>{
            if (v)
                p.Value = trueVal;
            else
                p.Value = falseVal;
        })
    {
        if (trueVal == null && falseVal == null)
            throw new ArgumentNullException();
    }
}
public class DataItemBitMap : DataItemProp<bool>, INotifyPropertyChangedExt2
{
    public IDataItemProp Register { get; }
    public int BitIndex { get; }

    public DataItemBitMap(IDataItemProp register, int bitIndex, string name)
    {
        Register = register;
        BitIndex = bitIndex;
        Name = name;
        Register.PropertyChanged += Register_PropertyChanged;
    }

    private void Register_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {

        if (e.PropertyName != nameof(Register.Value))//isInternalSet ||
            return;
        var val = Value;
        Console.WriteLine(val);
    }
    //bool isInternalSet=false;
    bool bitValue;

    /// <summary>
    /// 读取当前位的 Bool 值
    /// </summary>
    public override bool Value
    {
        get
        {
            var val = false;
            if (Register?.Value == null)
                val = false;
            else
                val = ExtractVal();
            NotifyThis.SetField(ref bitValue, val);
            return bitValue;
        }
        set
        {
            if (!CanWrite)
                return;
            var newVal = CalculateNewValue(value);
            if (!newVal.Equals(Register.Value))
            {
                //isInternalSet = true;
                bitValue = value;
                Register.Value = newVal;

                //isInternalSet=false;
                //NotifyThis.OnPropertyChanged();
            }

        }
    }

    private bool ExtractVal()
    {
        int val = Register.Value.ToInt32(null);
        return (val & (1 << BitIndex)) != 0;
    }

    public string Address { get => Register.Address; set => throw new NotImplementedException(); }
    public string Name { get; set; }
    //IConvertible IDataItemProp.Value { get => Value; set => Value = (bool)value; }
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
