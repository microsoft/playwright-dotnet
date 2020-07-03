using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channel;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IElementHandle" />
    public class ElementHandle : IChannelOwner, IElementHandle
    {
        private readonly ConnectionScope _scope;
        private readonly ElementHandleChannel _channel;

        internal ElementHandle(ConnectionScope scope, string guid, ElementHandleInitializer initializer)
        {
            _scope = scope;
            _channel = new ElementHandleChannel(guid, scope);
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        Channel IChannelOwner.Channel => _channel;

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string pageFunction, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<T> GetJsonValueAsync<T>() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> GetPropertyAsync(string propertyName) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IDictionary<string, IJSHandle>> GetPropertiesAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task DisposeAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task PressAsync(string key, PressOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TypeAsync(string text, int delay = 0) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<byte[]> ScreenshotAsync(ScreenshotOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task FillAsync(string text) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IFrame> GetContentFrameAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task HoverAsync(PointerActionOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ScrollIntoViewIfNeededAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IFrame> GetOwnerFrameAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<Rect> GetBoundingBoxAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<double> GetVisibleRatioAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task ClickAsync(ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task DoubleClickAsync(ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TripleClickAsync(ClickOptions options = null) => throw new NotImplementedException();

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
    }
}
