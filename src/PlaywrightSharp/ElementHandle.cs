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
    /// <inheritdoc cref="IElementHandle" />
    public class ElementHandle : JSHandle, IElementHandle, IChannelOwner<ElementHandle>
    {
        private readonly ConnectionScope _scope;
        private readonly ElementHandleChannel _channel;

        internal ElementHandle(ConnectionScope scope, string guid, ElementHandleInitializer initializer) : base(scope, guid, initializer)
        {
            _scope = scope;
            _channel = new ElementHandleChannel(guid, scope, this);
            _channel.PreviewUpdated += (sender, e) => Preview = e.Preview;
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<ElementHandle> IChannelOwner<ElementHandle>.Channel => _channel;

        internal IChannel<ElementHandle> ElementChannel => _channel;

        /// <inheritdoc />
        public Task PressAsync(string key, int delay = 0) => throw new NotImplementedException();

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
        public Task<Rect> GetBoundingBoxAsync() => _channel.GetBoundingBoxAsync();

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string script)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public async Task<IJSHandle> EvaluateHandleAsync(string script, object args)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false)).Object;

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
        public Task FocusAsync() => _channel.FocusAsync();

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
        public Task<string> GetAttributeAsync(string name, int? timeout = null) => _channel.GetAttributeAsync(name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHtmlAsync(int? timeout = null) => _channel.GetInnerHtmlAsync(timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(int? timeout = null) => _channel.GetInnerTextAsync(timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(int? timeout = null) => _channel.GetTextContentAsync(timeout);

        /// <inheritdoc />
        public Task SelectTextAsync(int? timeout = null) => _channel.SelectTextAsync(timeout);

        /// <inheritdoc />
        public Task SelectOptionAsync(string value, NavigatingActionWaitOptions options = null)
            => _channel.SelectOptionAsync(value, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public Task SelectOptionAsync(string[] values, NavigatingActionWaitOptions options = null)
            => _channel.SelectOptionAsync(values, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public Task SelectOptionAsync(ElementHandle element, NavigatingActionWaitOptions options = null)
            => _channel.SelectOptionAsync(element, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public Task SelectOptionAsync(ElementHandle[] elements, NavigatingActionWaitOptions options = null)
            => _channel.SelectOptionAsync(elements, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public Task SelectOptionAsync(SelectOption selectOption, NavigatingActionWaitOptions options = null)
            => _channel.SelectOptionAsync(selectOption, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public Task SelectOptionAsync(SelectOption[] selectOptions, NavigatingActionWaitOptions options = null)
            => _channel.SelectOptionAsync(selectOptions, options ?? new NavigatingActionWaitOptions());

        /// <inheritdoc />
        public Task CheckAsync(CheckOptions options)
            => _channel.CheckAsync(options);

        /// <inheritdoc />
        public Task UncheckAsync(CheckOptions options)
            => _channel.UncheckAsync(options);
    }
}
