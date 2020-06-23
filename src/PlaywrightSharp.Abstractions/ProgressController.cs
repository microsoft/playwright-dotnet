using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PlaywrightSharp
{
    internal class ProgressController
    {
        public ProgressController(LoggerFactory loggerFactory, int timeout, string apiName)
        {
            throw new NotImplementedException();
        }

        public Task<T> RunAsync<T>(Func<TaskProgress,Task<T>> task)
        {
            throw new NotImplementedException();
        }
    }
}
