using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Helpers
{
    internal class TaskQueue : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        internal TaskQueue() => _semaphore = new SemaphoreSlim(1, 1);

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        internal async Task EnqueueAsync(Func<Task> taskGenerator)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await taskGenerator().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
