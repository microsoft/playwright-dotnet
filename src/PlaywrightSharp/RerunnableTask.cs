using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class RerunnableTask : IDisposable
    {
        private readonly ContextData _contextData;
        private readonly Func<IFrameExecutionContext, Task<IJSHandle>> _task;
        private readonly CancellationTokenSource _cts;
        private readonly Task _timeoutTimer;
        private readonly TaskCompletionSource<IJSHandle> _taskCompletion;
        private bool _terminated;
        private int _runCount;

        public RerunnableTask(ContextData data, Func<IFrameExecutionContext, Task<IJSHandle>> task, int? timeout, string title)
        {
            _contextData = data;
            _task = task;
            _cts = new CancellationTokenSource();

            if (timeout.HasValue && timeout > 0)
            {
                _timeoutTimer = System.Threading.Tasks.Task
                    .Delay(timeout.Value, _cts.Token)
                    .ContinueWith(
                        _ => Terminate(new PlaywrightSharpException($"waiting for {(string.IsNullOrEmpty(title) ? "function" : title)} failed: timeout {timeout}ms exceeded")), TaskScheduler.Default);
            }

            _taskCompletion = new TaskCompletionSource<IJSHandle>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<IJSHandle> Task => _taskCompletion.Task;

        public void Dispose() => DoCleanup();

        internal void Terminate(Exception exception)
        {
            _terminated = true;
            _taskCompletion.TrySetException(exception);
            DoCleanup();
        }

        internal async Task RerunAsync(IFrameExecutionContext context)
        {
            int runCount = Interlocked.Increment(ref _runCount);
            IJSHandle success = null;
            Exception exception = null;

            try
            {
                success = await _task(context).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types.
            {
                exception = ex;
            }

            if (_terminated || runCount != _runCount)
            {
                if (success != null)
                {
                    await success.DisposeAsync().ConfigureAwait(false);
                }

                return;
            }

            if (exception == null &&
                await context.EvaluateAsync<bool>("s => !s", success)
                    .ContinueWith(task => task.IsFaulted || task.Result, TaskScheduler.Default)
                    .ConfigureAwait(false))
            {
                if (success != null)
                {
                    await success.DisposeAsync().ConfigureAwait(false);
                }

                return;
            }

            if (exception?.Message.Contains("Execution context was destroyed") == true)
            {
                _ = RerunAsync(context);
                return;
            }

            if (exception?.Message.Contains("Cannot find context with specified id") == true)
            {
                return;
            }

            if (exception != null)
            {
                _taskCompletion.TrySetException(exception);
            }
            else
            {
                _taskCompletion.TrySetResult(success);
            }

            DoCleanup();
        }

        private void DoCleanup()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }

            _cts?.Dispose();
            _contextData.RerunnableTasks.Remove(this);
        }
    }
}
