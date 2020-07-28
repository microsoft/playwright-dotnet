using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IFrame" />
    public class Frame : IChannelOwner<Frame>, IFrame
    {
        private readonly ConnectionScope _scope;
        private readonly FrameChannel _channel;
        private readonly FrameInitializer _initializer;

        internal Frame(ConnectionScope scope, string guid, FrameInitializer initializer)
        {
            _scope = scope;
            _channel = new FrameChannel(guid, scope, this);
            _initializer = initializer;
            Url = _initializer.Url;
            Name = _initializer.Name;
            ParentFrame = _initializer.ParentFrame;
        }

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Frame> IChannelOwner<Frame>.Channel => _channel;

        /// <inheritdoc />
        public IFrame[] ChildFrames => ChildFramesList.ToArray();

        /// <inheritdoc />
        public string Name { get; internal set; }

        /// <inheritdoc />
        public string Url { get; internal set; }

        /// <inheritdoc />
        IFrame IFrame.ParentFrame => ParentFrame;

        /// <inheritdoc cref="IFrame.ParentFrame" />
        public Frame ParentFrame { get; }

        /// <inheritdoc />
        public IPage Page { get; internal set; }

        /// <inheritdoc />
        public bool Detached { get; internal set; }

        /// <inheritdoc />
        public string Id { get; set; }

        internal List<Frame> ChildFramesList { get; } = new List<Frame>();

        /// <inheritdoc />
        public Task<string> GetTitleAsync() => _channel.GetTitleAsync();

        /// <inheritdoc />
        public async Task<IResponse> GoToAsync(string url, GoToOptions options = null)
            => (await _channel.GoToAsync(url, options).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, LifecycleEvent waitUntil) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SetContentAsync(string html, NavigationOptions options = null) => SetContentAsync(false, html, options);

        /// <inheritdoc />
        public Task<string> GetContentAsync() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(AddTagOptions options) => AddScriptTagAsync(false, options);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script) => EvaluateAsync<T>(false, script);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string script, object args) => EvaluateAsync<T>(false, script, args);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script) => EvaluateAsync(false, script);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string script, object args) => EvaluateAsync(false, script, args);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script) => EvaluateHandleAsync(false, script);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string script, object args) => EvaluateHandleAsync(false, script, args);

        /// <inheritdoc />
        public Task FillAsync(string selector, string text, NavigatingActionWaitOptions options = null) => FillAsync(false, selector, text, options);

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null)
            => WaitForSelectorAsync(false, selector, options);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForSelectorEvaluateAsync(
            string selector,
            string script,
            WaitForFunctionOptions options = null,
            params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string pageFunction, WaitForFunctionOptions options = null, params object[] args) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => QuerySelectorAsync(false, selector);

        /// <inheritdoc />
        public Task<IElementHandle[]> QuerySelectorAllAsync(string selector) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, object args) => QuerySelectorEvaluateAsync(false, selector, script, args);

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, object args) => QuerySelectorEvaluateAsync<T>(false, selector, script, args);

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script) => QuerySelectorAllEvaluateAsync(false, selector, script);

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script) => QuerySelectorAllEvaluateAsync<T>(false, selector, script);

        /// <inheritdoc />
        public Task ClickAsync(string selector, ClickOptions options = null) => ClickAsync(false, selector, options);

        /// <inheritdoc />
        public Task DoubleClickAsync(string selector, ClickOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script, object args) => QuerySelectorEvaluateAsync(false, selector, script, args);

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, object args) => QuerySelectorEvaluateAsync<T>(false, selector, script, args);

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script) => QuerySelectorEvaluateAsync(false, selector, script);

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script) => QuerySelectorEvaluateAsync<T>(false, selector, script);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitForNavigationOptions options = null) => WaitForNavigationAsync(false, options);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(LifecycleEvent waitUntil) => WaitForNavigationAsync(false, waitUntil);

        /// <inheritdoc />
        public Task FocusAsync(string selector, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task HoverAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, TypeOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(NavigationOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(AddTagOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, string value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, SelectOption value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, IElementHandle value, WaitForSelectorOptions options = null) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, string[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, SelectOption[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, IElementHandle[] values, WaitForSelectorOptions options) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params string[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params SelectOption[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task<string[]> SelectAsync(string selector, params IElementHandle[] values) => throw new NotImplementedException();

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(LifecycleEvent waitUntil, int? timeout = null)
            => WaitForLoadStateAsync(false, waitUntil, timeout);

        internal async Task<IResponse> WaitForNavigationAsync(bool isPageCall, WaitForNavigationOptions options)
            => (await _channel.WaitForNavigationAsync(
                options: options ?? new WaitForNavigationOptions(),
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal Task<IResponse> WaitForNavigationAsync(bool isPageCall, LifecycleEvent waitUntil)
            => WaitForNavigationAsync(isPageCall, new WaitForNavigationOptions { WaitUntil = waitUntil });

        internal Task FillAsync(bool isPageCall, string selector, string text, NavigatingActionWaitOptions options)
            => _channel.FillAsync(selector, text, options ?? new NavigatingActionWaitOptions(), isPageCall);

        internal Task WaitForLoadStateAsync(bool isPageCall, LifecycleEvent waitUntil, int? timeout = null)
            => _channel.WaitForLoadStateAsync(waitUntil, timeout, isPageCall);

        internal async Task<IElementHandle> AddScriptTagAsync(bool isPageCall, AddTagOptions options)
            => (await _channel.AddScriptTagAsync(
                options: options,
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal Task ClickAsync(bool isPageCall, string selector, ClickOptions options)
            => _channel.ClickAsync(selector, options, isPageCall);

        internal Task SetContentAsync(bool isPageCall, string html, NavigationOptions options)
            => _channel.SetcontentAsync(html, options, isPageCall);

        internal async Task<IElementHandle> QuerySelectorAsync(bool isPageCall, string selector)
            => (await _channel.QuerySelectorAsync(selector, isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IElementHandle> WaitForSelectorAsync(bool isPageCall, string selector, WaitForSelectorOptions options)
            => (await _channel.WaitForSelector(
                selector: selector,
                options: options ?? new WaitForSelectorOptions(),
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IJSHandle> EvaluateHandleAsync(bool isPageCall, string script)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IJSHandle> EvaluateHandleAsync(bool isPageCall, string script, object args)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false)).Object;

        internal async Task<T> EvaluateAsync<T>(bool isPageCall, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvaluateAsync(bool isPageCall, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvaluateAsync(bool isPageCall, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> EvaluateAsync<T>(bool isPageCall, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> QuerySelectorEvaluateAsync<T>(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> QuerySelectorEvaluateAsync(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> QuerySelectorEvaluateAsync(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> QuerySelectorEvaluateAsync<T>(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> QuerySelectorAllEvaluateAsync<T>(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> QuerySelectorAllEvaluateAsync(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> QuerySelectorAllEvaluateAsync(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> QuerySelectorAllEvaluateAsync<T>(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));
    }
}
