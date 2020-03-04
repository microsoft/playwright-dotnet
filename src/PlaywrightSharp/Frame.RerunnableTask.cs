using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal partial class Frame
    {
        internal class RerunnableTask
        {
            private readonly ContextData _contextData;
            private readonly Func<IFrameExecutionContext, IJSHandle> _task;
            private readonly string _title;
            private readonly TaskCompletionSource<IJSHandle> _tsc = new TaskCompletionSource<IJSHandle>();

            private readonly CancellationTokenSource _timeout;

            private bool _terminated = false;
            private int _runCount = 0;

            public RerunnableTask(ContextData data, Func<IFrameExecutionContext, IJSHandle> task, int? timeout, string title)
            {
                _contextData = data;
                _task = task;
                _title = title;

                if (timeout.GetValueOrDefault() > 0)
                {
                    _timeout = new CancellationTokenSource(timeout.Value);
                    _tsc.TrySetCanceled(_timeout.Token);
                }
            }

            public Task RerunAsync(IFrameExecutionContext context)
            {
                int runCount = Interlocked.Increment(ref _runCount);
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {

                    throw;
                }
            }

            public void Terminate(Exception exception)
            {
                _terminated = true;
                _tsc.TrySetException(exception);
                DoCleanup();
            }

            private void DoCleanup()
            {
                _contextData.RerunnableTasks.Remove(this);
            }
        }
    }
}
