
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Dobo.Appl.Utility;
public static class AsyncHelper
{
    private static readonly TaskFactory _taskFactory = new TaskFactory(
        CancellationToken.None,
        TaskCreationOptions.None,
        TaskContinuationOptions.None,
        TaskScheduler.Default); // 强制使用默认调度器，脱离UI上下文

    /// <summary>
    /// 将异步方法安全地转换为同步方法运行 (有返回值)
    /// </summary>
    public static TResult RunSync<TResult>(Func<Task<TResult>> func)
    {
        return _taskFactory
            .StartNew(func)
            .Unwrap()
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 将异步方法安全地转换为同步方法运行 (无返回值)
    /// </summary>
    public static void RunSync(Func<Task> func)
    {
        _taskFactory
            .StartNew(func)
            .Unwrap()
            .GetAwaiter()
            .GetResult();
    }
}
