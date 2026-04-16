
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Jint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", nameof(AvaloniaUI) +"."+nameof(AvaloniaUI.MarkupExt) )]
namespace AvaloniaUI.MarkupExt;

//[ContentProperty(nameof(Args))]
public class FmtExtension: IMultiValueConverter
{
    [Content]
    public List<object> Args { get; set; } = new();

    public FmtExtension()
    {
    }
    public FmtExtension(object arg0) { Args.Add(arg0); }
    public FmtExtension(object arg0, object arg1) : this(arg0) { Args.Add(arg1); }
    public FmtExtension(object arg0, object arg1, object arg2) : this(arg0, arg1) { Args.Add(arg2); }
    public FmtExtension(object arg0, object arg1, object arg2, object arg3) : this(arg0, arg1, arg2) { Args.Add(arg3); }
    public FmtExtension(object arg0, object arg1, object arg2, object arg3, object arg4) : this(arg0, arg1, arg2, arg3) { Args.Add(arg4); }

    public object ProvideValue(IServiceProvider provider)
    {
        var multiBinding = new MultiBinding();
        var bindings = new List<IBinding>();

        // 5. 在提供值时，统一将传入的对象转换为 Binding
        foreach (var arg in Args)
        {
            if (arg is IBinding binding)
                bindings.Add(binding);
            else
                bindings.Add(new Binding() { Source = arg }); // 将普通字符串/对象包装成静态绑定
        }

        multiBinding.Bindings = bindings;
        multiBinding.Converter = new FuncMultiValueConverter<object, string>(convertFun);

        return multiBinding;
    }

    private static string convertFun(IEnumerable<object?> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return string.Empty;

        // 拦截 UI 尚未就绪时的 UnsetValue
        if (list.Any(x => x == AvaloniaProperty.UnsetValue))
            return string.Empty;

        var formatStr = list[0]?.ToString();
        if (string.IsNullOrEmpty(formatStr)) return string.Empty;

        if (list.Count == 1) return formatStr;

        try
        {
            // 执行格式化
            return string.Format(formatStr, list.Skip(1).ToArray());
        }
        catch (FormatException)
        {
            return "[Fmt Format Error]";
        }
    }

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        return convertFun(values);
    }

    public object? StrExec(string txt) {
        var engine = new Engine(cfg =>
        {
        });
        var re=engine.Evaluate(txt);
        return re.AsString();
    }
}

