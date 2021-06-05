using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    internal partial class Worker : ChannelOwnerBase, IChannelOwner<Worker>, IWorker
    {
        private readonly WorkerChannel _channel;
        private readonly WorkerInitializer _initializer;

        public Worker(IChannelOwner parent, string guid, WorkerInitializer initializer) : base(parent, guid)
        {
            _channel = new WorkerChannel(guid, parent.Connection, this);
            _initializer = initializer;

            _channel.Closed += (_, _) =>
            {
                if (Page != null)
                {
                    Page.WorkersList.Remove(this);
                }

                if (BrowserContext != null)
                {
                    BrowserContext.ServiceWorkersList.Remove(this);
                }

                Close?.Invoke(this, this);
            };
        }

        public event EventHandler<IWorker> Close;

        public string Url => _initializer.Url;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Worker> IChannelOwner<Worker>.Channel => _channel;

        internal Page Page { get; set; }

        internal BrowserContext BrowserContext { get; set; }

        public async Task<T> EvaluateAsync<T>(string expression, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: expression,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: expression,
                arg: ScriptsHelper.SerializedArgument(arg))
            .ConfigureAwait(false))?.Object;

        public async Task<IWorker> WaitForCloseAsync(Func<Task> action = default, float? timeout = default)
        {
            using var waiter = new Waiter(_channel, "worker.WaitForCloseAsync");
            var waiterResult = waiter.GetWaitForEventTask<IWorker>(this, nameof(Close), null);
            var result = waiterResult.Task.WithTimeout(Convert.ToInt32(timeout ?? 0));
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }
            else
            {
                await result.ConfigureAwait(false);
            }

            return this;
        }
    }
}
