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
    public class JSHandle : ChannelOwnerBase, IChannelOwner<JSHandle>, IJSHandle
    {
        private readonly JSHandleChannel _channel;
        private readonly JSHandleInitializer _initializer;

        internal JSHandle(IChannelOwner parent, string guid, JSHandleInitializer initializer) : base(parent, guid)
        {
            _channel = new JSHandleChannel(guid, parent.Connection, this);
            _initializer = initializer;
            Preview = _initializer.Preview;
            _channel.PreviewUpdated += (sender, e) => Preview = e.Preview;
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<JSHandle> IChannelOwner<JSHandle>.Channel => _channel;

        internal string Preview { get; set; }

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

        /// <inheritdoc />
        public async Task<T> EvaluateAsync<T>(string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvaluateAsync(string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvaluateAsync(string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvaluateAsync<T>(string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> GetJsonValueAsync<T>() => ScriptsHelper.ParseEvaluateResult<T>(await _channel.GetJsonValueAsync().ConfigureAwait(false));

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
