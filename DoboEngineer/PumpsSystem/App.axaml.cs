using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PumpsSystem.AvaloniaUI;
using PumpsSystem.Pump;
using PumpsSystem.ViewModels;
using PumpsSystem.Views;
using System;
using System.Linq;

namespace PumpsSystem;

public partial class App : AppBase
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override Func<Window> MainWindowCreateFun => () => new Pump2.Window1();

}