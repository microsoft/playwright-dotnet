using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Contracts.Models;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class Worker : ChannelOwnerBase, IChannelOwner<Worker>, IWorker
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

        /// <inheritdoc/>
        public event EventHandler<IWorker> Close;

        /// <inheritdoc/>
        public string Url => _initializer.Url;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Worker> IChannelOwner<Worker>.Channel => _channel;

        internal Page Page { get; set; }

        internal BrowserContext BrowserContext { get; set; }

        public async Task<T> EvaluateAsync<T>(string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument())
            .ConfigureAwait(false))?.Object;

        public async Task<IWorker> WaitForCloseAsync(float? timeout)
        {
            using var waiter = new Waiter();
            var waiterResult = waiter.GetWaitForEventTask<IWorker>(this, nameof(Close), null);
            await waiterResult.Task.WithTimeout(Convert.ToInt32(timeout ?? 0)).ConfigureAwait(false);
            return this;
        }
    }
}
