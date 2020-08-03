using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IElementHandle" />
    public class ElementHandle : JSHandle, IElementHandle, IChannelOwner<ElementHandle>
    {
        private readonly ConnectionScope _scope;
        private readonly ElementHandleChannel _channel;

        internal ElementHandle(ConnectionScope scope, string guid, ElementHandleInitializer initializer) : base(scope, guid, initializer)
        {
            _scope = scope;
            _channel = new ElementHandleChannel(guid, scope, this);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<ElementHandle> IChannelOwner<ElementHandle>.Channel => _channel;

        internal IChannel<ElementHandle> ElementChannel => _channel;

        /// <inheritdoc />
        public Task PressAsync(string key, PressOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TypeAsync(string text, int delay = 0) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FillAsync(string text, NavigatingActionWaitOptions options = null)
            => _channel.FillAsync(text, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public async Task<IFrame> GetContentFrameAsync() => (await _channel.GetContentFrameAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task HoverAsync(PointerActionOptions options = null) => _channel.HoverAsync(options ?? new PointerActionOptions());

        /// <inheritdoc />
        public Task ScrollIntoViewIfNeededAsync(int? timeout = null) => _channel.ScrollIntoViewIfNeededAsync(timeout);

        /// <inheritdoc />
        public async Task<IFrame> GetOwnerFrameAsync() => (await _channel.GetOwnerFrameAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task<Rect> GetBoundingBoxAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClickAsync(ClickOptions options = null) => _channel.ClickAsync(options ?? new ClickOptions());

        /// <inheritdoc />
        public Task DoubleClickAsync(ClickOptions options = null) => _channel.DoubleClickAsync(options ?? new ClickOptions());

        /// <inheritdoc />
        public Task SetInputFilesAsync(params string[] filePath) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FocusAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(IElementHandle[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(SelectOption[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string value) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(IElementHandle value) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(SelectOption value) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task DispatchEventAsync(string type, object eventInit = null, int? timeout = null)
            => _channel.DispatchEventAsync(
                type,
                eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit),
                timeout);

        /// <inheritdoc />
        public Task SelectTextAsync(int? timeout = null) => _channel.SelectTextAsync(timeout);
    }
}
