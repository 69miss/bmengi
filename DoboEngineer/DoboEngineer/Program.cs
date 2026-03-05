using Avalonia;
using CommunityToolkit.Mvvm.DependencyInjection;
using Dobo.Appl.Module;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DoboEngineer
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                HandleException("主线程致命错误", ex);
            }
           
        }

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

        // 处理 Task 中未被 await 或未被捕获的异常
        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException("后台 Task 异常", e.Exception);
            e.SetObserved(); // 标记为已处理，防止进程崩溃（视 .NET 版本而定）
        }

        // 处理 AppDomain 级别的异常（通常是无法恢复的，但可以记录日志）
        private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            HandleException("AppDomain 未处理异常", ex);
            // 注意：如果是 IsTerminating 为 true，这里处理完后程序依然会退出
        }

        // 统一的日志/弹窗处理逻辑
        private static void HandleException(string source, Exception? ex)
        {
            string message = $"{DateTime.Now}:[{source}] {ex?.Message}\nStack: {ex?.StackTrace}";
            Console.WriteLine(message);
            //System.IO.File.AppendAllText("error.log", DateTime.Now + "\n" + message + "\n\n");

            // 2. 尝试显示错误弹窗 (注意：如果 UI 线程已死，这里可能无法弹出，需要原生 MessageBox)
            // 在 Windows 上可以使用 MessageBox API，跨平台建议尽量保存日志
        }
    }
}
