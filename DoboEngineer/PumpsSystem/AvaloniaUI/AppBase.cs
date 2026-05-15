using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Dobo.Appl.Service;
using PumpsSystem.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PumpsSystem.AvaloniaUI;

public abstract class AppBase : Application
{
    public AppBase()
    {
        Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;
        Dispatcher.UIThread.UnhandledExceptionFilter += UIThread_UnhandledExceptionFilter;
    }
    public virtual Func<Window> MainWindowCreateFun { get; }

    public override void OnFrameworkInitializationCompleted()
    {
        var dataDictSvc = new DataDictSvc();
        var langName = dataDictSvc.GetByName("LangName")?.Value??"zh-cn";
        PumpLang.Instance.ChangeLanguage(langName);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = MainWindowCreateFun?.Invoke();
        }
#if DEBUG
        this.AttachDevTools();
#endif
        base.OnFrameworkInitializationCompleted();
    }
    protected virtual void UIThread_UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
    {
        e.RequestCatch = true;
    }

    protected virtual void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Console.WriteLine($"{DateTime.Now} : UIThread_UnhandledException :" + e.Exception);
        e.Handled = true;
    }

    protected virtual void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

}
