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
    internal class JSHandle : ChannelOwnerBase, IChannelOwner<JSHandle>, IJSHandle
    {
        private readonly JSHandleChannel _channel;

        internal JSHandle(IChannelOwner parent, string guid, JSHandleInitializer initializer) : base(parent, guid)
        {
            _channel = new JSHandleChannel(guid, parent.Connection, this);
            Preview = initializer.Preview;
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<JSHandle> IChannelOwner<JSHandle>.Channel => _channel;

        internal string Preview { get; set; }

        public IElementHandle AsElement() => this as IElementHandle;

        public async Task<JsonElement?> EvaluateAsync(string expression, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false))?.Object;

        public async Task<T> EvaluateAsync<T>(string expression, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> JsonValueAsync<T>() => ScriptsHelper.ParseEvaluateResult<T>(await _channel.JsonValueAsync().ConfigureAwait(false));

        public async Task<IJSHandle> GetPropertyAsync(string propertyName) => (await _channel.GetPropertyAsync(propertyName).ConfigureAwait(false))?.Object;

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

        public Task DisposeAsync() => _channel.DisposeAsync();

        public override string ToString() => Preview;
    }
}
