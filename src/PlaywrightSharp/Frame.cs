using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
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
        public async Task<IElementHandle> GetFrameElementAsync()
            => (await _channel.GetFrameElementAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task<string> GetTitleAsync() => _channel.GetTitleAsync();

        /// <inheritdoc />
        public Task<IResponse> GoToAsync(string url, LifecycleEvent? waitUntil = null, string referer = null, int? timeout = null)
            => GoToAsync(false, url, waitUntil, referer, timeout);

        /// <inheritdoc />
        public Task SetContentAsync(string html, LifecycleEvent? waitUntil = null, int? timeout = null) => SetContentAsync(false, html, waitUntil, timeout);

        /// <inheritdoc />
        public Task<string> GetContentAsync() => GetContentAsync(false);

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(string url = null, string path = null, string content = null, string type = null)
            => AddScriptTagAsync(false, url, path, content, type);

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
        public Task FillAsync(string selector, string text, int? timeout = null, bool noWaitAfter = false) => FillAsync(false, selector, text, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForState? state = null, int? timeout = null)
            => WaitForSelectorAsync(false, selector, state, timeout);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(
            string pageFunction,
            object args,
            int? timeout = null,
            Polling? polling = null,
            int? pollingInterval = null)
            => WaitForFunctionAsync(false, pageFunction, args, timeout, polling, pollingInterval);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(
            string pageFunction,
            int? timeout = null,
            Polling? polling = null,
            int? pollingInterval = null)
            => WaitForFunctionAsync(false, pageFunction, timeout, polling, pollingInterval);

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => QuerySelectorAsync(false, selector);

        /// <inheritdoc />
        public Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector) => QuerySelectorAllAsync(false, selector);

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script, object args) => QuerySelectorEvaluateAsync(false, selector, script, args);

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script, object args) => QuerySelectorEvaluateAsync<T>(false, selector, script, args);

        /// <inheritdoc />
        public Task QuerySelectorAllEvaluateAsync(string selector, string script) => QuerySelectorAllEvaluateAsync(false, selector, script);

        /// <inheritdoc />
        public Task<T> QuerySelectorAllEvaluateAsync<T>(string selector, string script) => QuerySelectorAllEvaluateAsync<T>(false, selector, script);

        /// <inheritdoc />
        public Task ClickAsync(
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            int clickCount = 1,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool noWaitAfter = false)
            => ClickAsync(false, selector, delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task CheckAsync(string selector, int? timeout = null, bool force = false, bool noWaitAfter = false)
            => CheckAsync(false, selector, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task UncheckAsync(string selector, int? timeout = null, bool force = false, bool noWaitAfter = false)
            => UncheckAsync(false, selector, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task DoubleClickAsync(
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool noWaitAfter = false)
            => DoubleClickAsync(false, selector, delay, button, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script, object args) => QuerySelectorEvaluateAsync(false, selector, script, args);

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script, object args) => QuerySelectorEvaluateAsync<T>(false, selector, script, args);

        /// <inheritdoc />
        public Task QuerySelectorEvaluateAsync(string selector, string script) => QuerySelectorEvaluateAsync(false, selector, script);

        /// <inheritdoc />
        public Task<T> QuerySelectorEvaluateAsync<T>(string selector, string script) => QuerySelectorEvaluateAsync<T>(false, selector, script);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(LifecycleEvent? waitUntil = null, string url = null, int? timeout = null) => WaitForNavigationAsync(false, waitUntil, url, timeout);

        /// <inheritdoc />
        public Task FocusAsync(string selector, int? timeout = null) => FocusAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string file) => SetInputFilesAsync(selector, new[] { file });

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string[] files) => SetInputFilesAsync(false, selector, files);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload file) => SetInputFilesAsync(selector, new[] { file });

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload[] files) => SetInputFilesAsync(false, selector, files);

        /// <inheritdoc />
        public Task HoverAsync(
            string selector,
            Point? position = null,
            Modifier[] modifiers = null,
            bool force = false,
            int? timeout = null) => HoverAsync(false, selector, position, modifiers, force, timeout);

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, int delay = 0) => TypeAsync(false, selector, text, delay);

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(string url = null, string path = null, string content = null)
            => AddStyleTagAsync(false, url, path, content);

        /// <inheritdoc />
        public Task PressAsync(string selector, string text, int delay = 0, bool? noWaitAfter = null, int? timeout = null) => PressAsync(false, selector, text, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, null, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string value, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, SelectOption value, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle value, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string[] values, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, SelectOption[] values, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle[] values, bool? noWaitAfter = null, int? timeout = null) => SelectOptionAsync(false, selector, values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params string[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params SelectOption[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params IElementHandle[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task WaitForLoadStateAsync(LifecycleEvent? waitUntil = null, int? timeout = null)
            => WaitForLoadStateAsync(false, waitUntil, timeout);

        /// <inheritdoc />
        public Task DispatchEventAsync(string selector, string type, object eventInit = null, int? timeout = null)
            => DispatchEventAsync(false, selector, type, eventInit, timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string selector, string name, int? timeout = null)
            => GetAttributeAsync(false, selector, name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHtmlAsync(string selector, int? timeout = null)
            => GetInnerHtmlAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(string selector, int? timeout = null)
            => GetInnerTextAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(string selector, int? timeout = null)
            => GetTextContentAsync(false, selector, timeout);

        internal Task<string> GetContentAsync(bool isPageCall) => _channel.GetContentAsync(isPageCall);

        internal Task FocusAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.FocusAsync(selector, timeout, isPageCall);

        internal Task TypeAsync(bool isPageCall, string selector, string text, int delay)
            => _channel.TypeAsync(selector, text, delay, isPageCall);

        internal Task<string> GetAttributeAsync(bool isPageCall, string selector, string name, int? timeout = null)
            => _channel.GetAttributeAsync(selector, name, timeout, isPageCall);

        internal Task<string> GetInnerHtmlAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetInnerHtmlAsync(selector, timeout, isPageCall);

        internal Task<string> GetInnerTextAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetInnerTextAsync(selector, timeout, isPageCall);

        internal Task<string> GetTextContentAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetTextContentAsync(selector, timeout, isPageCall);

        internal Task HoverAsync(bool isPageCall, string selector, Point? position, Modifier[] modifiers, bool force, int? timeout)
            => _channel.HoverAsync(selector, position, modifiers, force, timeout, isPageCall);

        internal Task<string[]> PressAsync(bool isPageCall, string selector, string text, int delay, bool? noWaitAfter, int? timeout)
            => _channel.PressAsync(selector, text, delay, noWaitAfter, timeout, isPageCall);

        internal Task<string[]> SelectOptionAsync(bool isPageCall, string selector, object values, bool? noWaitAfter, int? timeout)
            => _channel.SelectOptionAsync(selector, values, noWaitAfter, timeout, isPageCall);

        internal Task DispatchEventAsync(bool isPageCall, string selector, string type, object eventInit = null, int? timeout = null)
            => _channel.DispatchEventAsync(
                    selector,
                    type,
                    eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit),
                    timeout,
                    isPageCall);

        internal async Task<IResponse> WaitForNavigationAsync(bool isPageCall, LifecycleEvent? waitUntil = null, string url = null, int? timeout = null)
            => (await _channel.WaitForNavigationAsync(waitUntil, url, timeout, isPageCall).ConfigureAwait(false))?.Object;

        internal Task FillAsync(bool isPageCall, string selector, string text, int? timeout = null, bool noWaitAfter = false)
            => _channel.FillAsync(selector, text, timeout, noWaitAfter, isPageCall);

        internal Task WaitForLoadStateAsync(bool isPageCall, LifecycleEvent? waitUntil, int? timeout = null)
            => _channel.WaitForLoadStateAsync(waitUntil, timeout, isPageCall);

        internal async Task<IElementHandle> AddScriptTagAsync(bool isPageCall, string url, string path, string content, string type)
            => (await _channel.AddScriptTagAsync(url, path, content, type, isPageCall).ConfigureAwait(false)).Object;

        internal async Task<IElementHandle> AddStyleTagAsync(bool isPageCall, string url, string path, string content)
            => (await _channel.AddStyleTagAsync(url, path, content, isPageCall).ConfigureAwait(false)).Object;

        internal Task SetInputFilesAsync(bool isPageCall, string selector, string[] files)
            => _channel.SetInputFilesAsync(selector, files.Select(f => f.ToFilePayload()).ToArray(), isPageCall);

        internal Task SetInputFilesAsync(bool isPageCall, string selector, FilePayload[] files)
            => _channel.SetInputFilesAsync(selector, files, isPageCall);

        internal Task ClickAsync(
            bool isPageCall,
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            int clickCount = 1,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool noWaitAfter = false)
            => _channel.ClickAsync(selector, delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter, isPageCall);

        internal Task DoubleClickAsync(
            bool isPageCall,
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool noWaitAfter = false)
            => _channel.DoubleClickAsync(selector, delay, button, modifiers, position, timeout, force, noWaitAfter, isPageCall);

        internal Task CheckAsync(bool isPageCall, string selector, int? timeout = null, bool force = false, bool noWaitAfter = false)
            => _channel.CheckAsync(selector, timeout, force, noWaitAfter, isPageCall);

        internal Task UncheckAsync(bool isPageCall, string selector, int? timeout = null, bool force = false, bool noWaitAfter = false)
            => _channel.UncheckAsync(selector, timeout, force, noWaitAfter, isPageCall);

        internal Task SetContentAsync(bool isPageCall, string html, LifecycleEvent? waitUntil, int? timeout)
            => _channel.SetcontentAsync(html, timeout, waitUntil, isPageCall);

        internal async Task<IElementHandle> QuerySelectorAsync(bool isPageCall, string selector)
            => (await _channel.QuerySelectorAsync(selector, isPageCall).ConfigureAwait(false))?.Object;

        internal async Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(bool isPageCall, string selector)
            => (await _channel.QuerySelectorAllAsync(selector, isPageCall).ConfigureAwait(false)).Select(c => ((ElementHandleChannel)c).Object);

        internal async Task<IJSHandle> WaitForFunctionAsync(bool isPageCall, string expression, int? timeout, Polling? polling, int? pollingInterval)
             => (await _channel.WaitForFunctionAsync(
                expression: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall,
                timeout: timeout,
                polling: (object)polling ?? pollingInterval).ConfigureAwait(false)).Object;

        internal async Task<IJSHandle> WaitForFunctionAsync(bool isPageCall, string expression, object args, int? timeout, Polling? polling, int? pollingInterval)
             => (await _channel.WaitForFunctionAsync(
                expression: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall,
                timeout: timeout,
                polling: (object)polling ?? pollingInterval).ConfigureAwait(false)).Object;

        internal async Task<IElementHandle> WaitForSelectorAsync(bool isPageCall, string selector, WaitForState? state, int? timeout)
            => (await _channel.WaitForSelector(
                selector: selector,
                state: state,
                timeout: timeout,
                isPage: isPageCall).ConfigureAwait(false))?.Object;

        internal async Task<IJSHandle> EvaluateHandleAsync(bool isPageCall, string script)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false))?.Object;

        internal async Task<IJSHandle> EvaluateHandleAsync(bool isPageCall, string script, object args)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: args,
                isPage: isPageCall,
                serializeArgument: true).ConfigureAwait(false))?.Object;

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
                arg: args,
                isPage: isPageCall,
                serializeArgument: true).ConfigureAwait(false));

        internal async Task<T> EvaluateAsync<T>(bool isPageCall, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: args,
                isPage: isPageCall,
                serializeArgument: true).ConfigureAwait(false));

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

        internal async Task<IResponse> GoToAsync(bool isPage, string url, LifecycleEvent? waitUntil, string referer, int? timeout)
            => (await _channel.GoToAsync(url, timeout, waitUntil, referer, isPage).ConfigureAwait(false))?.Object;
    }
}
