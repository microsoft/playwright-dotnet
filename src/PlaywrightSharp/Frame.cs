/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Copyright (c) 2020 Meir Blachman
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IFrame" />
    public class Frame : ChannelOwnerBase, IChannelOwner<Frame>, IFrame
    {
        private readonly FrameChannel _channel;
        private readonly List<LifecycleEvent> _loadStates = new List<LifecycleEvent>();

        internal Frame(IChannelOwner parent, string guid, FrameInitializer initializer) : base(parent, guid)
        {
            _channel = new FrameChannel(guid, parent.Connection, this);
            Url = initializer.Url;
            Name = initializer.Name;
            ParentFrame = initializer.ParentFrame;
            _loadStates = initializer.LoadStates;

            _channel.LoadState += (_, e) =>
            {
                lock (_loadStates)
                {
                    if (e.Add.HasValue)
                    {
                        _loadStates.Add(e.Add.Value);
                        LoadState?.Invoke(this, new LoadStateEventArgs { LifecycleEvent = e.Add.Value });
                    }

                    if (e.Remove.HasValue)
                    {
                        _loadStates.Remove(e.Remove.Value);
                    }
                }
            };

            _channel.Navigated += (_, e) =>
            {
                Url = e.Url;
                Name = e.Name;
                Navigated?.Invoke(this, e);

                if (string.IsNullOrEmpty(e.Error))
                {
                    ((Page)Page)?.OnFrameNavigated(this);
                }
            };
        }

        /// <summary>
        /// Raised when a navigation is received.
        /// </summary>
        public event EventHandler<FrameNavigatedEventArgs> Navigated;

        /// <summary>
        /// Raised when a new LoadState was added.
        /// </summary>
        public event EventHandler<LoadStateEventArgs> LoadState;

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
        public bool IsDetached { get; internal set; }

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
        public Task<T> EvaluateAsync<T>(string expression) => EvaluateAsync<T>(false, expression);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string expression, object arg) => EvaluateAsync<T>(false, expression, arg);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string expression) => EvaluateAsync(false, expression);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string expression, object arg) => EvaluateAsync(false, expression, arg);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string expression) => EvaluateHandleAsync(false, expression);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string expression, object arg) => EvaluateHandleAsync(false, expression, arg);

        /// <inheritdoc />
        public Task FillAsync(string selector, string value, int? timeout = null, bool? noWaitAfter = null)
            => FillAsync(false, selector, value, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task WaitForTimeoutAsync(int timeout) => Task.Delay(timeout);

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState? state = null, int? timeout = null)
            => WaitForSelectorAsync(false, selector, state, timeout);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, int? timeout = null)
            => WaitForFunctionAsync(false, expression, arg, timeout, null, null);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, Polling polling, int? timeout = null)
            => WaitForFunctionAsync(false, expression, arg, timeout, polling, null);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, int polling, int? timeout = null)
            => WaitForFunctionAsync(false, expression, arg, timeout, null, polling);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, int? timeout = null)
            => WaitForFunctionAsync(false, expression, timeout, null, null);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, Polling polling, int? timeout = null)
            => WaitForFunctionAsync(false, expression, timeout, polling, null);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, int polling, int? timeout = null)
            => WaitForFunctionAsync(false, expression, timeout, null, polling);

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => QuerySelectorAsync(false, selector);

        /// <inheritdoc />
        public Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector) => QuerySelectorAllAsync(false, selector);

        /// <inheritdoc />
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg) => EvalOnSelectorAsync(false, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg) => EvalOnSelectorAsync<T>(false, selector, expression, arg);

        /// <inheritdoc />
        public Task EvalOnSelectorAllAsync(string selector, string expression) => EvalOnSelectorAllAsync(false, selector, expression);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression) => EvalOnSelectorAllAsync<T>(false, selector, expression);

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
            bool? noWaitAfter = null)
            => ClickAsync(false, selector, delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task CheckAsync(string selector, int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => CheckAsync(false, selector, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task UncheckAsync(string selector, int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => UncheckAsync(false, selector, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task DblclickAsync(
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null)
            => DblclickAsync(false, selector, delay, button, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task EvalOnSelectorAsync(string selector, string expression, object arg) => EvalOnSelectorAsync(false, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg) => EvalOnSelectorAsync<T>(false, selector, expression, arg);

        /// <inheritdoc />
        public Task EvalOnSelectorAsync(string selector, string expression) => EvalOnSelectorAsync(false, selector, expression);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression) => EvalOnSelectorAsync<T>(false, selector, expression);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(LifecycleEvent? waitUntil = null, int? timeout = null)
            => WaitForNavigationAsync(waitUntil: waitUntil, url: null, regex: null, match: null, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(string url, LifecycleEvent? waitUntil = null, int? timeout = null)
            => WaitForNavigationAsync(waitUntil: waitUntil, url: url, regex: null, match: null, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Regex url, LifecycleEvent? waitUntil = null, int? timeout = null)
            => WaitForNavigationAsync(waitUntil: waitUntil, url: null, regex: url, match: null, timeout: timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Func<string, bool> url, LifecycleEvent? waitUntil = null, int? timeout = null)
            => WaitForNavigationAsync(waitUntil: waitUntil, url: null, regex: null, match: url, timeout: timeout);

        /// <inheritdoc />
        public Task FocusAsync(string selector, int? timeout = null) => FocusAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string file, int? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(selector, new[] { file }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string[] files, int? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(false, selector, files, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, ElementHandleFiles file, int? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(selector, new[] { file }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, ElementHandleFiles[] files, int? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(false, selector, files, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task HoverAsync(
            string selector,
            Point? position = null,
            Modifier[] modifiers = null,
            bool force = false,
            int? timeout = null) => HoverAsync(false, selector, position, modifiers, force, timeout);

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, int delay = 0, int? timeout = null, bool? noWaitAfter = null)
            => TypeAsync(false, selector, text, delay, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(string url = null, string path = null, string content = null)
            => AddStyleTagAsync(false, url, path, content);

        /// <inheritdoc />
        public Task PressAsync(string selector, string key, int delay = 0, int? timeout = null, bool? noWaitAfter = null)
            => PressAsync(false, selector, key, delay, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(false, selector, null, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string value, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(selector, new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, ElementHandleValues value, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(selector, new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle value, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(selector, new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, string[] values, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(false, selector, values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, ElementHandleValues[] values, int? timeout = null, bool? noWaitAfter = null)
        {
            if (values == null)
            {
                throw new ArgumentException("values should not be null", nameof(values));
            }

            return SelectOptionAsync(false, selector, values, timeout, noWaitAfter);
        }

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, IElementHandle[] values, int? timeout = null, bool? noWaitAfter = null)
        {
            if (values == null)
            {
                throw new ArgumentException("values should not be null", nameof(values));
            }

            return SelectOptionAsync(false, selector, values, timeout, noWaitAfter);
        }

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params string[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params ElementHandleValues[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string selector, params IElementHandle[] values) => SelectOptionAsync(selector, values, null, null);

        /// <inheritdoc />
        public async Task WaitForLoadStateAsync(LifecycleEvent state = LifecycleEvent.Load, int? timeout = null)
        {
            Task<LoadStateEventArgs> task;
            Waiter waiter = null;

            try
            {
                lock (_loadStates)
                {
                    if (_loadStates.Contains(state))
                    {
                        return;
                    }

                    waiter = SetupNavigationWaiter(timeout);
                    task = waiter.WaitForEventAsync<LoadStateEventArgs>(this, "LoadState", s =>
                    {
                        waiter.Log($"  \"{s}\" event fired");
                        return s.LifecycleEvent == state;
                    });
                }

                await task.ConfigureAwait(false);
            }
            finally
            {
                waiter?.Dispose();
            }
        }

        /// <inheritdoc />
        public Task DispatchEventAsync(string selector, string type, object eventInit = null, int? timeout = null)
            => DispatchEventAsync(false, selector, type, eventInit, timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string selector, string name, int? timeout = null)
            => GetAttributeAsync(false, selector, name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHTMLAsync(string selector, int? timeout = null)
            => GetInnerHTMLAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(string selector, int? timeout = null)
            => GetInnerTextAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(string selector, int? timeout = null)
            => GetTextContentAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task TapAsync(string selector, Modifier[] modifiers = null, Point? position = null, int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => TapAsync(false, selector, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task<bool> GetIsCheckedAsync(string selector, int? timeout = null) => GetIsCheckedAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> GetIsDisabledAsync(string selector, int? timeout = null) => GetIsDisabledAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> GetIsEditableAsync(string selector, int? timeout = null) => GetIsEditableAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> GetIsEnabledAsync(string selector, int? timeout = null) => GetIsEnabledAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> GetIsHiddenAsync(string selector, int? timeout = null) => GetIsHiddenAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> GetIsVisibleAsync(string selector, int? timeout = null) => GetIsVisibleAsync(false, selector, timeout);

        internal Task TapAsync(bool isPageCall, string selector, Modifier[] modifiers = null, Point? position = null, int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.TapAsync(selector, modifiers, position, timeout, force, noWaitAfter, isPageCall);

        internal async Task<IResponse> WaitForNavigationAsync(
            LifecycleEvent? waitUntil = null,
            string url = null,
            Regex regex = null,
            Func<string, bool> match = null,
            int? timeout = null)
        {
            waitUntil ??= LifecycleEvent.Load;
            var waiter = SetupNavigationWaiter(timeout);
            string toUrl = !string.IsNullOrEmpty(url) ? $" to \"{url}\"" : string.Empty;

            waiter.Log($"waiting for navigation{toUrl} until \"{waitUntil}\"");

            var navigatedEvent = await waiter.WaitForEventAsync<FrameNavigatedEventArgs>(
                this,
                "Navigated",
                e =>
                {
                    // Any failed navigation results in a rejection.
                    if (e.Error != null)
                    {
                        return true;
                    }

                    waiter.Log($"  navigated to \"{e.Url}\"");
                    return UrlMatches(e.Url, url, regex, match);
                }).ConfigureAwait(false);

            if (navigatedEvent.Error != null)
            {
                var ex = new NavigationException(navigatedEvent.Error);
                var tcs = new TaskCompletionSource<bool>();
                tcs.TrySetException(ex);
                await waiter.WaitForPromiseAsync(tcs.Task).ConfigureAwait(false);
            }

            if (!_loadStates.Contains(waitUntil.Value))
            {
                await waiter.WaitForEventAsync<LoadStateEventArgs>(
                    this,
                    "LoadState",
                    e =>
                    {
                        waiter.Log($"  \"{e.LifecycleEvent}\" event fired");
                        return e.LifecycleEvent == waitUntil;
                    }).ConfigureAwait(false);
            }

            var request = navigatedEvent.NewDocument?.Request?.Object;
            var response = request != null
                ? await waiter.WaitForPromiseAsync(request.FinalRequest.GetResponseAsync()).ConfigureAwait(false)
                : null;

            waiter.Dispose();
            return response;
        }

        internal Task<string> GetContentAsync(bool isPageCall) => _channel.GetContentAsync(isPageCall);

        internal Task FocusAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.FocusAsync(selector, timeout, isPageCall);

        internal Task TypeAsync(bool isPageCall, string selector, string text, int delay, int? timeout, bool? noWaitAfter)
            => _channel.TypeAsync(selector, text, delay, timeout, noWaitAfter, isPageCall);

        internal Task<string> GetAttributeAsync(bool isPageCall, string selector, string name, int? timeout = null)
            => _channel.GetAttributeAsync(selector, name, timeout, isPageCall);

        internal Task<string> GetInnerHTMLAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetInnerHTMLAsync(selector, timeout, isPageCall);

        internal Task<string> GetInnerTextAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetInnerTextAsync(selector, timeout, isPageCall);

        internal Task<string> GetTextContentAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetTextContentAsync(selector, timeout, isPageCall);

        internal Task HoverAsync(bool isPageCall, string selector, Point? position, Modifier[] modifiers, bool force, int? timeout)
            => _channel.HoverAsync(selector, position, modifiers, force, timeout, isPageCall);

        internal Task<string[]> PressAsync(bool isPageCall, string selector, string key, int delay, int? timeout, bool? noWaitAfter)
            => _channel.PressAsync(selector, key, delay, timeout, noWaitAfter, isPageCall);

        internal Task<string[]> SelectOptionAsync(bool isPageCall, string selector, object[] values, int? timeout, bool? noWaitAfter)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null)
                    {
                        throw new PlaywrightSharpException($"options[{i}]: expected object, got null");
                    }
                }
            }

            return _channel.SelectOptionAsync(selector, values, timeout, noWaitAfter, isPageCall);
        }

        internal Task DispatchEventAsync(bool isPageCall, string selector, string type, object eventInit = null, int? timeout = null)
            => _channel.DispatchEventAsync(
                    selector,
                    type,
                    eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit),
                    timeout,
                    isPageCall);

        internal Task FillAsync(bool isPageCall, string selector, string value, int? timeout = null, bool? noWaitAfter = null)
            => _channel.FillAsync(selector, value, timeout, noWaitAfter, isPageCall);

        internal async Task<IElementHandle> AddScriptTagAsync(bool isPageCall, string url, string path, string content, string type)
        {
            if (!string.IsNullOrEmpty(path))
            {
                content = File.ReadAllText(path);
                content += "//# sourceURL=" + path.Replace("\n", string.Empty);
            }

            return (await _channel.AddScriptTagAsync(url, path, content, type, isPageCall).ConfigureAwait(false)).Object;
        }

        internal async Task<IElementHandle> AddStyleTagAsync(bool isPageCall, string url, string path, string content)
        {
            if (!string.IsNullOrEmpty(path))
            {
                content = File.ReadAllText(path);
                content += "//# sourceURL=" + path.Replace("\n", string.Empty);
            }

            return (await _channel.AddStyleTagAsync(url, path, content, isPageCall).ConfigureAwait(false)).Object;
        }

        internal Task SetInputFilesAsync(bool isPageCall, string selector, string[] files, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(selector, files.Select(f => f.ToElementHandleFile()).ToArray(), timeout, noWaitAfter, isPageCall);

        internal Task SetInputFilesAsync(bool isPageCall, string selector, IEnumerable<ElementHandleFiles> files, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(selector, files, timeout, noWaitAfter, isPageCall);

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
            bool? noWaitAfter = null)
            => _channel.ClickAsync(selector, delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter, isPageCall);

        internal Task DblclickAsync(
            bool isPageCall,
            string selector,
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null)
            => _channel.DblclickAsync(selector, delay, button, modifiers, position, timeout, force, noWaitAfter, isPageCall);

        internal Task CheckAsync(bool isPageCall, string selector, int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.CheckAsync(selector, timeout, force, noWaitAfter, isPageCall);

        internal Task UncheckAsync(bool isPageCall, string selector, int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.UncheckAsync(selector, timeout, force, noWaitAfter, isPageCall);

        internal Task SetContentAsync(bool isPageCall, string html, LifecycleEvent? waitUntil, int? timeout)
            => _channel.SetContentAsync(html, timeout, waitUntil, isPageCall);

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
                polling: polling != null ? null : pollingInterval).ConfigureAwait(false)).Object;

        internal async Task<IJSHandle> WaitForFunctionAsync(bool isPageCall, string expression, object args, int? timeout, Polling? polling, int? pollingInterval)
             => (await _channel.WaitForFunctionAsync(
                expression: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall,
                timeout: timeout,
                polling: polling != null ? null : pollingInterval).ConfigureAwait(false)).Object;

        internal async Task<IElementHandle> WaitForSelectorAsync(bool isPageCall, string selector, WaitForSelectorState? state, int? timeout)
            => (await _channel.WaitForSelectorAsync(
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

        internal async Task<T> EvalOnSelectorAsync<T>(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvalOnSelectorAsync(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvalOnSelectorAsync(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> EvalOnSelectorAsync<T>(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> EvalOnSelectorAllAsync<T>(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvalOnSelectorAllAsync(bool isPageCall, string selector, string script)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined,
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<JsonElement?> EvalOnSelectorAllAsync(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<T> EvalOnSelectorAllAsync<T>(bool isPageCall, string selector, string script, object args)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall).ConfigureAwait(false));

        internal async Task<IResponse> GoToAsync(bool isPage, string url, LifecycleEvent? waitUntil, string referer, int? timeout)
            => (await _channel.GoToAsync(url, timeout, waitUntil, referer, isPage).ConfigureAwait(false))?.Object;

        internal Task<bool> GetIsCheckedAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetIsCheckedAsync(selector, timeout, isPageCall);

        internal Task<bool> GetIsDisabledAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetIsDisabledAsync(selector, timeout, isPageCall);

        internal Task<bool> GetIsEditableAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetIsEditableAsync(selector, timeout, isPageCall);

        internal Task<bool> GetIsEnabledAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetIsEnabledAsync(selector, timeout, isPageCall);

        internal Task<bool> GetIsHiddenAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetIsHiddenAsync(selector, timeout, isPageCall);

        internal Task<bool> GetIsVisibleAsync(bool isPageCall, string selector, int? timeout = null)
            => _channel.GetIsVisibleAsync(selector, timeout, isPageCall);

        private Waiter SetupNavigationWaiter(int? timeout)
        {
            var waiter = new Waiter();
            waiter.RejectOnEvent<EventArgs>(Page, PageEvent.Close.Name, new NavigationException("Navigation failed because page was closed!"));
            waiter.RejectOnEvent<EventArgs>(Page, PageEvent.Crash.Name, new NavigationException("Navigation failed because page was crashed!"));
            waiter.RejectOnEvent<FrameEventArgs>(
                Page,
                "FrameDetached",
                new NavigationException("Navigating frame was detached!"),
                e => e.Frame == this);
            timeout ??= Page?.DefaultNavigationTimeout ?? Playwright.DefaultTimeout;
            waiter.RejectOnTimeout(timeout, $"Timeout {timeout}ms exceeded.");

            return waiter;
        }

        private bool UrlMatches(string url, string matchUrl, Regex regex, Func<string, bool> match)
        {
            if (matchUrl == null && regex == null && match == null)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(matchUrl))
            {
                regex = matchUrl.GlobToRegex();
            }

            if (matchUrl != null && url == matchUrl)
            {
                return true;
            }

            if (regex != null)
            {
                return regex.IsMatch(url);
            }

            return match(url);
        }
    }
}
