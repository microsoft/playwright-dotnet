using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IJSHandle" />
    public class JSHandle : IChannelOwner<JSHandle>, IJSHandle
    {
        private readonly ConnectionScope _scope;
        private readonly JSHandleChannel _channel;
        private readonly JSHandleInitializer _initializer;

        internal JSHandle(ConnectionScope scope, string guid, JSHandleInitializer initializer)
        {
            _scope = scope;
            _channel = new JSHandleChannel(guid, scope, this);
            _initializer = initializer;
            Preview = _initializer.Preview;
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<JSHandle> IChannelOwner<JSHandle>.Channel => _channel;

        internal string Preview { get; set; }

        /// <inheritdoc />
        public async Task<T> EvaluateAsync<T>(string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvaluateAsync(string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvaluateAsync(string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvaluateAsync<T>(string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> GetJsonValueAsync<T>() => ScriptsHelper.ParseEvaluateResult<T>(await _channel.GetJsonValue().ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<IJSHandle> GetPropertyAsync(string propertyName) => (await _channel.GetPropertyAsync(propertyName).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IDictionary<string, IJSHandle>> GetPropertiesAsync()
        {
            var result = new Dictionary<string, IJSHandle>();
            var channelResult = await _channel.GetPropertiesAsync().ConfigureAwait(false);

            foreach (var kv in channelResult)
            {
                result[kv.Name] = kv.Value.Object;
            }

            return result;
        }

        /// <inheritdoc />
        public Task DisposeAsync() => _channel.DisposeAsync();

        /// <inheritdoc />
        public override string ToString() => Preview;
    }
}
