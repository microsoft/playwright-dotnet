using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp.Helpers
{
    internal class TaskQueue : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        internal TaskQueue() => _semaphore = new SemaphoreSlim(1);

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        internal async Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return await taskGenerator().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        internal async Task Enqueue(Func<Task> taskGenerator)
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
