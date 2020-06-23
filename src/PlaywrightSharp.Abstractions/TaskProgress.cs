using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PlaywrightSharp
{
    internal class TaskProgress
    {
        public static Task<T> RunAbortableTaskAsync<T>(
            Func<TaskProgress, Task<T>> task,
            LoggerFactory loggerFactory,
            int timeout,
            string apiName)
            => new ProgressController(loggerFactory, timeout, apiName).RunAsync(task);
    }
}
