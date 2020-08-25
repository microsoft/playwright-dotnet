using System;
using System.Collections.Generic;
using System.Linq;
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
        public Task PressAsync(string key, int delay = 0) => _channel.PressAsync(key, delay);

        /// <inheritdoc />
        public Task TypeAsync(string text, int delay = 0) => _channel.TypeAsync(text, delay);

        /// <inheritdoc />
        public async Task<byte[]> ScreenshotAsync(
            string path = null,
            bool omitBackground = false,
            ScreenshotFormat? type = null,
            int? quality = null,
            int? timeout = null)
            => Convert.FromBase64String(await _channel.ScreenshotAsync(path, omitBackground, type, quality, timeout).ConfigureAwait(false));

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
        public Task SetInputFilesAsync(string file) => SetInputFilesAsync(new[] { file });

        /// <inheritdoc />
        public Task SetInputFilesAsync(string[] files) => _channel.SetInputFilesAsync(files.Select(f => f.ToFilePayload()).ToArray());

        /// <inheritdoc />
        public Task SetInputFilesAsync(FilePayload file) => SetInputFilesAsync(new[] { file });

        /// <inheritdoc />
        public Task SetInputFilesAsync(FilePayload[] files) => _channel.SetInputFilesAsync(files);

        /// <inheritdoc />
        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(e => ((ElementHandleChannel)e).Object);

        /// <inheritdoc />
        public async Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> QuerySelectorEvaluateAsync(string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> QuerySelectorEvaluateAsync(string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> QuerySelectorAllEvaluateAsync(string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> QuerySelectorAllEvaluateAsync(string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false));

        /// <inheritdoc />
        public Task FocusAsync() => _channel.FocusAsync();

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
        public Task<string[]> SelectOptionAsync(string value, bool? noWaitAfter = null, int? timeout = null)
            => _channel.SelectOptionAsync(value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string[] values, bool? noWaitAfter = null, int? timeout = null)
            => _channel.SelectOptionAsync(values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(IElementHandle element, bool? noWaitAfter = null, int? timeout = null)
            => SelectOptionAsync(new[] { element }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(IElementHandle[] elements, bool? noWaitAfter = null, int? timeout = null)
            => _channel.SelectOptionAsync(elements.Cast<ElementHandle>(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(SelectOption selectOption, bool? noWaitAfter = null, int? timeout = null)
            => _channel.SelectOptionAsync(selectOption, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(SelectOption[] selectOptions, bool? noWaitAfter = null, int? timeout = null)
            => _channel.SelectOptionAsync(selectOptions, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(bool? noWaitAfter = null, int? timeout = null)
        => _channel.SelectOptionAsync(null, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params string[] values) => SelectOptionAsync(values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params SelectOption[] values) => SelectOptionAsync(values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params IElementHandle[] values) => SelectOptionAsync(values, null, null);

        /// <inheritdoc />
        public Task CheckAsync(CheckOptions options)
            => _channel.CheckAsync(options);

        /// <inheritdoc />
        public Task UncheckAsync(CheckOptions options)
            => _channel.UncheckAsync(options);
    }
}
