
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using DoboEngineer.code;
using System;
using System.Collections.Generic;
using System.Linq;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", nameof(DoboEngineer)+"."+nameof(DoboEngineer.Langs) )]
namespace DoboEngineer.Langs;
public class FmtExtension
{
    public FmtExtension(object arg0)
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
}

