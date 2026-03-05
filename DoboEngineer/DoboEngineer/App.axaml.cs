using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DoboEngineer.ViewModels;
using DoboEngineer.Views;

namespace DoboEngineer
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this); 
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Dispatcher.UIThread.UnhandledException += UIThread_UnhandledException;
            Dispatcher.UIThread.UnhandledExceptionFilter += UIThread_UnhandledExceptionFilter;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                   // DataContext = new MainWindowViewModel(),
                };
                //desktop.MainWindow = new StandardSet();
            }
#if DEBUG
            this.AttachDevTools();
#endif
            base.OnFrameworkInitializationCompleted();
        }

        private void UIThread_UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            e.RequestCatch = true;
        }

        private void UIThread_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now} : UIThread_UnhandledException :" +e.Exception);
           e.Handled = true;
        }

        private void DisableAvaloniaDataAnnotationValidation()
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
}