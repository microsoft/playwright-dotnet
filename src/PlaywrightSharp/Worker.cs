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
        IPage IWorker.Page => Page;

        /// <inheritdoc cref="IWorker.Page"/>
        public Page Page { get; internal set; }

        /// <inheritdoc/>
        IBrowserContext IWorker.BrowserContext => BrowserContext;

        /// <inheritdoc cref="IWorker.BrowserContext"/>
        public BrowserContext BrowserContext { get; internal set; }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Worker> IChannelOwner<Worker>.Channel => _channel;

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string script)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string script, object args)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false))?.Object;

        /// <inheritdoc/>
        public async Task<T> EvaluateAsync<T>(string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc/>
        public async Task<JsonElement?> EvaluateAsync(string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc/>
        public async Task<JsonElement?> EvaluateAsync(string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: args,
                serializeArgument: true).ConfigureAwait(false));

        /// <inheritdoc/>
        public async Task<T> EvaluateAsync<T>(string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: args,
                serializeArgument: true).ConfigureAwait(false));
    }
}
