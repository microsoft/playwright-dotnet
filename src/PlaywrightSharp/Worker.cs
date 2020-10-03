using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
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

            _channel.Closed += (sender, e) =>
            {
                if (Page != null)
                {
                    Page.WorkersList.Remove(this);
                }

                if (BrowserContext != null)
                {
                    BrowserContext.ServiceWorkersList.Remove(this);
                }

                Close?.Invoke(this, EventArgs.Empty);
            };
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Close;

        /// <inheritdoc/>
        public string Url => _initializer.Url;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Worker> IChannelOwner<Worker>.Channel => _channel;

        internal Page Page { get; set; }

        internal BrowserContext BrowserContext { get; set; }

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string pageFunction)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string pageFunction, object arg)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false))?.Object;

        /// <inheritdoc/>
        public async Task<T> EvaluateAsync<T>(string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc/>
        public async Task<JsonElement?> EvaluateAsync(string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc/>
        public async Task<JsonElement?> EvaluateAsync(string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: arg,
                serializeArgument: true).ConfigureAwait(false));

        /// <inheritdoc/>
        public async Task<T> EvaluateAsync<T>(string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: arg,
                serializeArgument: true).ConfigureAwait(false));
    }
}
