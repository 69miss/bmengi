
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Metadata;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "UI.Avalonia")]
namespace UI.Avalonia;
    public class FmtExtension
{
    public FmtExtension(object arg0)
    {
        AppendArg(arg0);
    }
    private void AppendArg(object arg0)
    {
        if (arg0 is IBinding arg)
            Args.Add(arg);
        else
            Args.Add(new Binding() { Source = arg0 });
    }

    public FmtExtension(object arg0, object arg1) : this(arg0)
    {
        AppendArg(arg1);
    }

    public FmtExtension(object arg0, object arg1, object arg2) : this(arg0, arg1)
    {
        AppendArg(arg2);
    }

    public FmtExtension(object arg0, object arg1, object arg2, object arg3) : this(arg0, arg1, arg2)
    {
        AppendArg(arg3);
    }
    public List<IBinding> Args { get; } = new();


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
}

