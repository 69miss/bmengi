using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using Dobo.Appl.Module;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace DoboEngineer
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var services = new ServiceCollection();
            var list = new List<IModule>() { new ApplModule() };
            list.ForEach(p => p.ConfigureServices(services));
            var sp= services.BuildServiceProvider();
            Default=sp;
            Ioc.Default.ConfigureServices(sp);
            list.ForEach(p => p.OnStartup(sp));
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
        public static IServiceProvider Default { get ; private set ; }
    }
}
