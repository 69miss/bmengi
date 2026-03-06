using CommunityToolkit.Mvvm.ComponentModel;
using Dobo.Appl.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoboEngineer.Pump;


public interface IDataItemBase: INotifyPropertyChanged
{
   public ushort Address{get;set;}
   public string Name { get; set;}
   public abstract IConvertible Value { get;set;}
   public bool CanWrite { get; set; }
}
public  class DataItemBase: IDataItemBase, INotifyPropertyChangedExt2
{
    private IConvertible _value;

    public ushort Address { get; set; }
    public string Name { get; set; }
    public virtual IConvertible Value { get => _value; set =>NotifyThis.SetField(ref _value,value); }
    public bool CanWrite { get; set; }
    protected INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public PropertyChangedEventHandler PropertyChangedEventHandlerGet()
    {
        return PropertyChanged;
    }
}
/// <summary>
/// 泵状态信息类（用bool表示每个状态）
/// </summary>
public class PumpStatus : DataItemBase, INotifyPropertyChangedExt2
{
    private bool _pump1Remote;   // 第0位
    private bool _pump2Remote;   // 第1位
    private bool _pump3Remote;   // 第2位
    private bool _pump4Remote;   // 第3位
    private bool _pump1Fault;    // 第4位
    private bool _pump2Fault;    // 第5位
    private bool _pump3Fault;    // 第6位
    private bool _pump4Fault;    // 第7位
    private bool _pump1Running;  // 第8位
    private bool _pump2Running;  // 第9位
    private bool _pump3Running;  // 第10位
    private bool _pump4Running;  // 第11位

    /// <summary>
    /// (40002, "状态反馈", false, "", 2)
    /// </summary>
    public PumpStatus()
    {
        Address = 40002;
        Name = "状态反馈";
        CanWrite = false;
    }

    #region 完整属性（带变更通知）
    // 第0-3位：泵远程状态
    public bool Pump1Remote
    {
        get => _pump1Remote;
        set => NotifyThis.SetField(ref _pump1Remote, value, props: "Value");
    }

    public bool Pump2Remote
    {
        get => _pump2Remote;
        set => NotifyThis.SetField(ref _pump2Remote, value, props: "Value");
    }

    public bool Pump3Remote
    {
        get => _pump3Remote;
        set => NotifyThis.SetField(ref _pump3Remote, value, props: "Value");
    }

    public bool Pump4Remote
    {
        get => _pump4Remote;
        set => NotifyThis.SetField(ref _pump4Remote, value, props: "Value");
    }

    // 第4-7位：泵故障状态
    public bool Pump1Fault
    {
        get => _pump1Fault;
        set => NotifyThis.SetField(ref _pump1Fault, value, props: "Value");
    }

    public bool Pump2Fault
    {
        get => _pump2Fault;
        set => NotifyThis.SetField(ref _pump2Fault, value, props: "Value");
    }

    public bool Pump3Fault
    {
        get => _pump3Fault;
        set => NotifyThis.SetField(ref _pump3Fault, value, props: "Value");
    }

    public bool Pump4Fault
    {
        get => _pump4Fault;
        set => NotifyThis.SetField(ref _pump4Fault, value, props: "Value");
    }

    // 第8-11位：泵运行状态
    public bool Pump1Running
    {
        get => _pump1Running;
        set => NotifyThis.SetField(ref _pump1Running, value, props: "Value");
    }

    public bool Pump2Running
    {
        get => _pump2Running;
        set => NotifyThis.SetField(ref _pump2Running, value, props: "Value");
    }

    public bool Pump3Running
    {
        get => _pump3Running;
        set => NotifyThis.SetField(ref _pump3Running, value, props: "Value");
    }

    public bool Pump4Running
    {
        get => _pump4Running;
        set => NotifyThis.SetField(ref _pump4Running, value, props: "Value");
    }
    #endregion
    //public  ushort Address { get; set; } = 40002;
    //public string Name { get; set; } = "状态反馈";
    //public bool CanWrite { get; set; }=false;
    public override IConvertible Value
    {
        get
        {
            return ToIntValue();
        }
        set
        {
            FromIntValue(value);
            NotifyThis.OnPropertyChanged();
        }
    }
    //public INotifyPropertyChangedExt2 NotifyThis { get => this; }
    //public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 可选：从按位数值（如整数）转换为bool状态（兼容原有按位逻辑）
    /// </summary>
    /// <param name="statusValue">按位计算的整数值</param>
    public void FromIntValue(IConvertible val)
    {
        int statusValue = val.ToInt32(null);
        Pump1Remote = (statusValue & (1 << 0)) != 0;
        Pump2Remote = (statusValue & (1 << 1)) != 0;
        Pump3Remote = (statusValue & (1 << 2)) != 0;
        Pump4Remote = (statusValue & (1 << 3)) != 0;

        Pump1Fault = (statusValue & (1 << 4)) != 0;
        Pump2Fault = (statusValue & (1 << 5)) != 0;
        Pump3Fault = (statusValue & (1 << 6)) != 0;
        Pump4Fault = (statusValue & (1 << 7)) != 0;

        Pump1Running = (statusValue & (1 << 8)) != 0;
        Pump2Running = (statusValue & (1 << 9)) != 0;
        Pump3Running = (statusValue & (1 << 10)) != 0;
        Pump4Running = (statusValue & (1 << 11)) != 0;
    }



    /// <summary>
    /// 可选：从bool状态转换为按位数值（便于存储/传输）
    /// </summary>
    /// <returns>按位计算的整数值</returns>
    public int ToIntValue()
    {
        int value = 0;
        if (Pump1Remote) value |= 1 << 0;
        if (Pump2Remote) value |= 1 << 1;
        if (Pump3Remote) value |= 1 << 2;
        if (Pump4Remote) value |= 1 << 3;

        if (Pump1Fault) value |= 1 << 4;
        if (Pump2Fault) value |= 1 << 5;
        if (Pump3Fault) value |= 1 << 6;
        if (Pump4Fault) value |= 1 << 7;

        if (Pump1Running) value |= 1 << 8;
        if (Pump2Running) value |= 1 << 9;
        if (Pump3Running) value |= 1 << 10;
        if (Pump4Running) value |= 1 << 11;

        return value;
    }

    /// <summary>
    /// 重写ToString，按「状态名:1/0；」格式展示所有泵状态
    /// </summary>
    /// <returns>所有状态的明细字符串</returns>
    public override string ToString()
    {
        var statusLines = new List<string>
        {
            $"1泵远程:{(Pump1Remote ? 1 : 0)}",
            $"2泵远程:{(Pump2Remote ? 1 : 0)}",
            $"3泵远程:{(Pump3Remote ? 1 : 0)}",
            $"4泵远程:{(Pump4Remote ? 1 : 0)}",
            $"1泵故障:{(Pump1Fault ? 1 : 0)}",
            $"2泵故障:{(Pump2Fault ? 1 : 0)}",
            $"3泵故障:{(Pump3Fault ? 1 : 0)}",
            $"4泵故障:{(Pump4Fault ? 1 : 0)}",
            $"1泵运行:{(Pump1Running ? 1 : 0)}",
            $"2泵运行:{(Pump2Running ? 1 : 0)}",
            $"3泵运行:{(Pump3Running ? 1 : 0)}",
            $"4泵运行:{(Pump4Running ? 1 : 0)}"
        };
        return string.Join("；", statusLines);
    }
}

public class PumpCtl : DataItemBase
{
    private bool _pump1Running;  // 第8位
    private bool _pump2Running;  // 第9位
    private bool _pump3Running;  // 第10位
    private bool _pump4Running;  // 第11位

    /// <summary>
    /// (40003, "控制字", true, "", 2)
    /// </summary>
    public PumpCtl()
    {
        Address = 40003;
        Name = "控制位";
        CanWrite = true;
    }

    // 第8-11位：泵运行状态
    public bool Pump1Running
    {
        get => _pump1Running;
        set => NotifyThis.SetField(ref _pump1Running, value, props: "Value");
    }

    public bool Pump2Running
    {
        get => _pump2Running;
        set => NotifyThis.SetField(ref _pump2Running, value, props: "Value");
    }

    public bool Pump3Running
    {
        get => _pump3Running;
        set => NotifyThis.SetField(ref _pump3Running, value, props: "Value");
    }

    public bool Pump4Running
    {
        get => _pump4Running;
        set => NotifyThis.SetField(ref _pump4Running, value, props: "Value");
    }

    public override IConvertible Value
    {
        get
        {
            int value = 0;
            if (Pump1Running) value |= 1 << 0;
            if (Pump2Running) value |= 1 << 1;
            if (Pump3Running) value |= 1 << 2;
            if (Pump4Running) value |= 1 << 3;
            return value;
        }
        set
        {
            
            var statusValue = value.ToInt32(null);
            Pump1Running = (statusValue & (1 << 0)) != 0;
            Pump2Running = (statusValue & (1 << 1)) != 0;
            Pump3Running = (statusValue & (1 << 2)) != 0;
            Pump4Running = (statusValue & (1 << 3)) != 0;
            NotifyThis.OnPropertyChanged();
        }
    }
     
}
public class PumpCtlMode: DataItemBase
{
    private bool flow;
    private bool manual;
    bool engineering;

    /// <summary>
    /// (40004, "控制模式", true, "", 2)
    /// </summary>
    public PumpCtlMode()
    {
        Address = 40004;
        Name = "控制模式";
        CanWrite = true;
    }

    public bool Flow { get => flow; set => NotifyThis.SetField(ref flow, value, props: "Value");}
    public bool Manual { get => manual; set => NotifyThis.SetField(ref manual, value, props: "Value"); }
    public bool Engineering { get => engineering; set => NotifyThis.SetField(ref engineering, value, props: "Value"); }
    public override IConvertible Value
    {
        get
        {
            int value = 0;
            if (Flow) value |= 1 << 0;
            if (Manual) value |= 1 << 1;
            if (Engineering) value |= 1 << 3;
            return value;
        }
        set
        {
            var statusValue = value.ToInt32(null);

            flow = (statusValue & (1 << 0)) != 0;
            manual = (statusValue & (1 << 1)) != 0;
            engineering=(statusValue & (1 << 3)) != 0;
            NotifyThis.OnPropertyChanged();
        }
    }

    
}

