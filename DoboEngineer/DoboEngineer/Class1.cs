
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "MyApp.Extensions")]
namespace DoboEngineer;
public class TranTest : AvaloniaObject
{
    // 1. 注册附加属性 "Key"
    // <TextBlock local:Translate.Key="HomeTitle" />
    public static readonly AttachedProperty<string> KeyProperty =
        AvaloniaProperty.RegisterAttached<TranTest, Control, string>("Key");

    // 标准的 Getter 和 Setter
    public static string GetKey(Control element)
    {
        //MarkupExtension
        //IMarkupExtension
        return element.GetValue(KeyProperty);
    }



    public TranTest(object arg0)
    {
        AppendArg(arg0);
    }
    private void AppendArg(object arg0)
    {
        if (arg0 is BindingBase arg)
            Args.Add(arg);
        else
            Args.Add(new Binding() { Source = arg0 });
    }

    public TranTest(object arg0, object arg1):this(arg0) 
    {
        AppendArg(arg1);
    }

    public TranTest(object arg0, object arg1, object arg2) :this(arg0,arg1)
    {
        AppendArg(arg2);
    }

    public TranTest(object arg0, object arg1, object arg2, object arg3) : this(arg0, arg1,arg2)
    {
        AppendArg(arg3);
    }
    public List<BindingBase> Args { get; } = new();


    public object ProvideValue(IServiceProvider provider)
    {
        var mb = new MultiBinding()
        {
            Bindings = Args,
            Converter = new FuncMultiValueConverter<string, string>(strs =>
            {
                if (strs.Count() == 1)
                    return strs.First();
                else
                    return string.Format(strs.FirstOrDefault(), strs.Skip(1).Cast<object>().ToArray());
            })
        };
        return mb;
    }
    public static void SetKey(Control element, string value)
    {
        element.SetValue(KeyProperty, value);
    }

    // 2. 静态构造函数：监听属性变化
    static TranTest()
    {
        KeyProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs<string>>(OnKeyChanged));
    }

    // 3. 当 XAML 中 Key 发生变化时的回调
    private static void OnKeyChanged(AvaloniaPropertyChangedEventArgs<string> args)
    {
        if (args.Sender is Control control && !string.IsNullOrEmpty(args.NewValue.Value))
        {
            // 这里是核心逻辑：自动生成 Binding
            // 假设你的单例服务是 LanguageService.Instance
           
            var binding = new Binding
            {
               // Source = LanguageService.Instance, // 绑定源：你的语言服务单例
                Path = $"[{args.NewValue.Value}]", // Path 变成索引器形式: [Key]
                Mode = BindingMode.OneWay
            };

            // 判断控件类型，绑定到正确的属性上
            if (control is TextBlock textBlock)
            {
                textBlock.Bind(TextBlock.TextProperty, binding);
            }
            else if (control is ContentControl contentControl)
            {
                // 比如 Button, Label
                contentControl.Bind(ContentControl.ContentProperty, binding);
            }
            else if (control is TextBox textBox)
            {
                // 比如水印
                textBox.Bind(TextBox.WatermarkProperty, binding);
            }
            // 可以继续扩展其他控件...
        }
    }
}

