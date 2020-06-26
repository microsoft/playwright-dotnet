using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PlaywrightSharp
{
    internal class TaskProgress
    {
        public AsyncDelegate CleanupWhenAborted { get; set; }

        public int TimeUntilDeadline { get; set; }

        public static Task<T> RunAbortableTaskAsync<T>(
            Func<TaskProgress, Task<T>> task,
            ILoggerFactory loggerFactory,
            int timeout,
            string apiName)
            => new ProgressController(loggerFactory, timeout, apiName).RunAsync(task);
    }
}
