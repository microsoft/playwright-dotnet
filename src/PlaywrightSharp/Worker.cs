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
    internal class Worker : IChannelOwner<Worker>, IWorker
    {
        private readonly ConnectionScope _scope;
        private readonly WorkerChannel _channel;
        private readonly WorkerInitializer _initializer;

        public Worker(ConnectionScope scope, string guid, WorkerInitializer initializer)
        {
            _scope = scope;
            _channel = new WorkerChannel(guid, scope, this);
            _initializer = initializer;

            _channel.Closed += (sender, e) =>
            {
                Closed?.Invoke(this, EventArgs.Empty);


            }
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Closed;

        /// <inheritdoc/>
        public string Url => _initializer.Url;

        /// <inheritdoc/>
        public IPage Page { get; internal set; }

        /// <inheritdoc/>
        public IBrowserContext BrowserContext { get; internal set; }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Worker> IChannelOwner<Worker>.Channel => _channel;

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
