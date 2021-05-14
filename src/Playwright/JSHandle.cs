using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    /// <inheritdoc cref="IJSHandle" />
    public class JSHandle : ChannelOwnerBase, IChannelOwner<JSHandle>, IJSHandle
    {
        private readonly JSHandleChannel _channel;

        internal JSHandle(IChannelOwner parent, string guid, JSHandleInitializer initializer) : base(parent, guid)
        {
            _channel = new JSHandleChannel(guid, parent.Connection, this);
            Preview = initializer.Preview;
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<JSHandle> IChannelOwner<JSHandle>.Channel => _channel;

        internal string Preview { get; set; }

        /// <inheritdoc />
        public IElementHandle AsElement() => this as IElementHandle;

        /// <inheritdoc />
        public async Task<JsonElement?> EvaluateAsync(string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<T> EvaluateAsync<T>(string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> JsonValueAsync<T>() => ScriptsHelper.ParseEvaluateResult<T>(await _channel.JsonValueAsync().ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<IJSHandle> GetPropertyAsync(string propertyName) => (await _channel.GetPropertyAsync(propertyName).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<Dictionary<string, IJSHandle>> GetPropertiesAsync()
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
