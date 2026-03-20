using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Dobo.Appl.Utility.INotifyPropertyChangedExt;

namespace Dobo.Appl.Utility;

public interface INotifyPropertyChangedExt : INotifyPropertyChanged
{

    //protected void OnPropertyChanged(DependencyPropertyChangedEventArgs e);
    void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        //OnPropertyChanged(new DependencyPropertyChangedEventArgs());
        TriggerPropertyChangedViaReflection(propertyName);
    }
    void OnPropertyChanged<T>(T oldVal, T newVal, [CallerMemberName] string propertyName = "")
    {
        TriggerPropertyChangedViaReflection(propertyName, oldVal, newVal);
    }


    public bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        //OnPropertyChanged(new DependencyPropertyChangedEventArgs( DependencyProperty.Register(propertyName,typeof(T),this.GetType()), field, value));
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false; // 值未改变，不触发通知
        var oldVal = field;
        field = value;
        OnPropertyChanged(oldVal, value, propertyName);
        return true;
    }
    private void TriggerPropertyChangedViaReflection(string propertyName, object oldVal = null, object newVal = null)
    {
        //PropertyChangedEventHandlerGet().Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (this.Ext(nameof(PropertyChangedEventHandler)) is PropertyChangedEventHandler dlg)
        {
            dlg?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return;
        }
        // 1. 获取当前实例的类型信息
        Type type = GetType();

        // 2. 获取名为 "PropertyChanged" 的事件信息
        EventInfo eventInfo = type.GetEvent("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (eventInfo != null)
        {
            // 3. 获取事件的委托字段（约定俗成的命名：事件名 + "Event"）
            FieldInfo eventField = type.GetField(eventInfo.Name, BindingFlags.Instance | BindingFlags.NonPublic);

            if (eventField != null)
            {
                // 4. 从当前实例中获取委托对象
                PropertyChangedEventHandler delegateInstance = eventField.GetValue(this) as PropertyChangedEventHandler;
                this.Ext(nameof(PropertyChangedEventHandler), delegateInstance);
                // 5. 安全地调用委托（即触发事件）
                delegateInstance?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    public class PropertyChangedEventArgsExt : PropertyChangedEventArgsMark
    {
        public PropertyChangedEventArgsExt(string? propertyName, object oldVal, object newVal,int mark=0) : base(propertyName,mark)
        {
            //PropertyName = propertyName;
            OldVal = oldVal;
            NewVal = newVal;
        }

        //public string? PropertyName { get; }
        public object OldVal { get; }
        public object NewVal { get; }
    }
    public class PropertyChangedEventArgsMark : PropertyChangedEventArgs
    {
        public PropertyChangedEventArgsMark(string? propertyName, int mark = 0) : base(propertyName)
        {
            Mark = mark;
        }
        /// <summary>
        /// 预定义值：0未设置，5内部触发
        /// </summary>
        public int Mark { get; }
    }
}
public interface INotifyPropertyChangedExt2 : INotifyPropertyChanged
{
   protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
        PropertyChangedEventHandlerGet()?.Invoke(this, e);
    }
   public virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
    public virtual void OnPropertyChanged<T>(T oldVal, T newVal, [CallerMemberName] string propertyName = "",int mark=0)
    {
        OnPropertyChanged(new PropertyChangedEventArgsExt(propertyName, oldVal, newVal,mark));
    }

    //public INotifyPropertyChangedExt2 NotifyThis { get => this; }
    public bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "",int mark=0, params string[] props)
    {
        //OnPropertyChanged(new DependencyPropertyChangedEventArgs( DependencyProperty.Register(propertyName,typeof(T),this.GetType()), field, value));
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        var oldVal = field;
        field = value;
        OnPropertyChanged(oldVal, value, propertyName,mark);
        if (props != null && props.Length > 0)
            foreach (var item in props)
            {
                OnPropertyChanged(item);
            }
        return true;
    }
     
    protected PropertyChangedEventHandler PropertyChangedEventHandlerGet();

}

public class ObservableItemCollection<T> : ObservableCollection<T>,IDisposable where T : INotifyPropertyChanged
{
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        // 取消旧元素的订阅
        if (e.OldItems != null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
                item.PropertyChanged -= Item_PropertyChanged;
        }
        // 订阅新元素的事件
        if (e.NewItems != null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
                item.PropertyChanged += Item_PropertyChanged;
        }
        base.OnCollectionChanged(e);
    }
    public event PropertyChangedEventHandler? ItemPropertyChanged;
    public event Action<object,DateTime>? ItemsPropertyChangedEnd;
    public void OnItemsPropertyChangedEnd(DateTime beginTime) {
        ItemsPropertyChangedEnd?.Invoke(this, beginTime);
    }
    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ItemPropertyChanged?.Invoke(sender, e);
    }

    // 清理时取消所有订阅
    protected override void ClearItems()
    {
        foreach (INotifyPropertyChanged item in this)
            item.PropertyChanged -= Item_PropertyChanged;
        base.ClearItems();
    }

    public void Dispose()
    {
        ClearItems();
    }
}

