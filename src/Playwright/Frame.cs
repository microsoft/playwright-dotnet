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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    /// <inheritdoc cref="IFrame" />
    public class Frame : ChannelOwnerBase, IChannelOwner<Frame>, IFrame
    {
        private readonly FrameChannel _channel;
        private readonly List<LoadState> _loadStates = new List<LoadState>();

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
                        LoadState?.Invoke(this, e.Add.Value);
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
        public event EventHandler<LoadState> LoadState;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Frame> IChannelOwner<Frame>.Channel => _channel;

        /// <inheritdoc />
        public IReadOnlyCollection<IFrame> ChildFrames => ChildFramesList;

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
        public async Task<IElementHandle> FrameElementAsync()
            => (await _channel.FrameElementAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task<string> TitleAsync() => _channel.TitleAsync();

        /// <inheritdoc />
        public Task<IResponse> GotoAsync(string url, WaitUntilState waitUntil, float? timeout, string referer)
            => GotoAsync(false, url, waitUntil.EnsureDefaultValue(WaitUntilState.Load), referer, timeout);

        /// <inheritdoc />
        public Task SetContentAsync(string html, float? timeout, WaitUntilState waitUntil)
            => SetContentAsync(false, html, waitUntil.EnsureDefaultValue(WaitUntilState.Load), timeout);

        /// <inheritdoc />
        public Task<string> ContentAsync() => ContentAsync(false);

        /// <inheritdoc />
        public Task<IElementHandle> AddScriptTagAsync(string url = null, string path = null, string content = null, string type = null)
            => AddScriptTagAsync(false, url, path, content, type);

        /// <inheritdoc />
        public Task<T> EvaluateAsync<T>(string expression, object arg) => EvaluateAsync<T>(false, expression, arg);

        /// <inheritdoc />
        public Task<JsonElement?> EvaluateAsync(string expression, object arg) => EvaluateAsync(false, expression, arg);

        /// <inheritdoc />
        public Task<IJSHandle> EvaluateHandleAsync(string expression, object arg) => EvaluateHandleAsync(false, expression, arg);

        /// <inheritdoc />
        public Task FillAsync(string selector, string value, bool? noWaitAfter, float? timeout)
            => FillAsync(false, selector, value, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task WaitForTimeoutAsync(float timeout) => Task.Delay(Convert.ToInt32(timeout));

        /// <inheritdoc />
        public Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState state, float? timeout)
            => WaitForSelectorAsync(false, selector, state.EnsureDefaultValue(WaitForSelectorState.Visible), timeout);

        /// <inheritdoc />
        public Task<IJSHandle> WaitForFunctionAsync(string expression, object arg, float? pollingInterval, float? timeout)
            => WaitForFunctionAsync(false, expression, arg, pollingInterval, timeout);

        /// <inheritdoc />
        public Task<IElementHandle> QuerySelectorAsync(string selector) => QuerySelectorAsync(false, selector);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<IElementHandle>> QuerySelectorAllAsync(string selector) => QuerySelectorAllAsync(false, selector);

        /// <inheritdoc />
        public Task EvalOnSelectorAllAsync(string selector, string expression, object arg)
            => EvalOnSelectorAsync(false, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg) => EvalOnSelectorAsync<T>(false, selector, expression, arg);

        /// <inheritdoc />
        public Task ClickAsync(
            string selector,
            MouseButton button,
            int? clickCount,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => ClickAsync(false, selector, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1, modifiers, position, timeout, force, noWaitAfter, trial);

        /// <inheritdoc />
        public Task CheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => CheckAsync(false, selector, position, force, noWaitAfter, timeout, trial);

        /// <inheritdoc />
        public Task UncheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => UncheckAsync(false, selector, position, force, noWaitAfter, timeout, trial);

        /// <inheritdoc />
        public Task DblClickAsync(
            string selector,
            MouseButton button,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => DblClickAsync(false, selector, delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), position, modifiers, timeout, force ?? false, noWaitAfter, trial);

        /// <inheritdoc />
        public Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg)
            => EvalOnSelectorAsync(false, selector, expression, arg);

        /// <inheritdoc />
        public Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg) => EvalOnSelectorAsync<T>(false, selector, expression, arg);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(string urlString, WaitUntilState waitUntil = default, float? timeout = default)
            => WaitForNavigationAsync(urlString, null, null, waitUntil, timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(WaitUntilState waitUntil, float? timeout)
            => WaitForNavigationAsync(null, null, null, waitUntil, timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Regex urlRegex, WaitUntilState waitUntil, float? timeout)
            => WaitForNavigationAsync(null, urlRegex, null, waitUntil, timeout);

        /// <inheritdoc />
        public Task<IResponse> WaitForNavigationAsync(Func<string, bool> urlFunc, WaitUntilState waitUntil, float? timeout)
            => WaitForNavigationAsync(null, null, urlFunc, waitUntil, timeout);

        /// <inheritdoc />
        public Task FocusAsync(string selector, float? timeout) => FocusAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(false, selector, files, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(false, selector, files, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task HoverAsync(
            string selector,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            float? timeout,
            bool? trial)
            => HoverAsync(false, selector, position, modifiers, force ?? false, timeout, trial);

        /// <inheritdoc />
        public Task TypeAsync(string selector, string text, float? delay, bool? noWaitAfter, float? timeout)
            => TypeAsync(false, selector, text, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IElementHandle> AddStyleTagAsync(string url = null, string path = null, string content = null)
            => AddStyleTagAsync(false, url, path, content);

        /// <inheritdoc />
        public Task PressAsync(string selector, string key, float? delay, bool? noWaitAfter, float? timeout)
            => PressAsync(false, selector, key, delay, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(false, selector, null, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(false, selector, values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, bool? noWaitAfter, float? timeout)
        {
            if (values == null)
            {
                throw new ArgumentException("values should not be null", nameof(values));
            }

            return SelectOptionAsync(false, selector, values.ToArray<object>(), noWaitAfter, timeout);
        }

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout)
        {
            if (values == null)
            {
                throw new ArgumentException("values should not be null", nameof(values));
            }

            return SelectOptionAsync(false, selector, values.ToArray<object>(), noWaitAfter, timeout);
        }

        /// <inheritdoc />
        public async Task WaitForLoadStateAsync(LoadState state, float? timeout)
        {
            Task<LoadState> task;
            Waiter waiter = null;
            state = state.EnsureDefaultValue(Microsoft.Playwright.LoadState.Load);

            try
            {
                lock (_loadStates)
                {
                    if (_loadStates.Contains(state))
                    {
                        return;
                    }

                    waiter = SetupNavigationWaiter(timeout);
                    task = waiter.WaitForEventAsync<LoadState>(this, "LoadState", s =>
                    {
                        waiter.Log($"  \"{s}\" event fired");
                        return s == state;
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
        public Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout)
            => DispatchEventAsync(false, selector, type, eventInit, timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string selector, string name, float? timeout)
            => GetAttributeAsync(false, selector, name, timeout);

        /// <inheritdoc />
        public Task<string> InnerHTMLAsync(string selector, float? timeout)
            => InnerHTMLAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<string> InnerTextAsync(string selector, float? timeout)
            => InnerTextAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<string> TextContentAsync(string selector, float? timeout)
            => GetTextContentAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task TapAsync(string selector, Position position, IEnumerable<KeyboardModifier> modifiers, bool? noWaitAfter, bool? force, float? timeout, bool? trial)
            => TapAsync(false, selector, modifiers, position, force, noWaitAfter, timeout, trial);

        /// <inheritdoc />
        public Task<bool> IsCheckedAsync(string selector, float? timeout) => IsCheckedAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsDisabledAsync(string selector, float? timeout) => IsDisabledAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsEditableAsync(string selector, float? timeout) => IsEditableAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync(string selector, float? timeout) => IsEnabledAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsHiddenAsync(string selector, float? timeout) => IsHiddenAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task<bool> IsVisibleAsync(string selector, float? timeout) => IsVisibleAsync(false, selector, timeout);

        /// <inheritdoc />
        public Task WaitForURLAsync(string urlString, float? timeout = default, WaitUntilState waitUntil = default)
            => WaitForURLAsync(urlString, null, null, timeout, waitUntil);

        /// <inheritdoc />
        public Task WaitForURLAsync(Regex urlRegex, float? timeout = default, WaitUntilState waitUntil = default)
            => WaitForURLAsync(null, urlRegex, null, timeout, waitUntil);

        /// <inheritdoc />
        public Task WaitForURLAsync(Func<string, bool> urlFunc, float? timeout = default, WaitUntilState waitUntil = default)
            => WaitForURLAsync(null, null, urlFunc, timeout, waitUntil);

        internal async Task<IResponse> WaitForNavigationAsync(
            string urlString,
            Regex urlRegex,
            Func<string, bool> urlFunc,
            WaitUntilState waitUntil,
            float? timeout)
        {
            waitUntil = waitUntil.EnsureDefaultValue(WaitUntilState.Load);
            var waiter = SetupNavigationWaiter(timeout);
            string toUrl = !string.IsNullOrEmpty(urlString) ? $" to \"{urlString}\"" : string.Empty;

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
                    return UrlMatches(e.Url, urlString, urlRegex, urlFunc);
                }).ConfigureAwait(false);

            if (navigatedEvent.Error != null)
            {
                var ex = new NavigationException(navigatedEvent.Error);
                var tcs = new TaskCompletionSource<bool>();
                tcs.TrySetException(ex);
                await waiter.WaitForPromiseAsync(tcs.Task).ConfigureAwait(false);
            }

            if (!_loadStates.Select(s => s.ToValueString()).Contains(waitUntil.ToValueString()))
            {
                await waiter.WaitForEventAsync<LoadState>(
                    this,
                    "LoadState",
                    e =>
                    {
                        waiter.Log($"  \"{e}\" event fired");
                        return e.ToValueString() == waitUntil.ToValueString();
                    }).ConfigureAwait(false);
            }

            var request = navigatedEvent.NewDocument?.Request?.Object;
            var response = request != null
                ? await waiter.WaitForPromiseAsync(request.FinalRequest.ResponseAsync()).ConfigureAwait(false)
                : null;

            waiter.Dispose();
            return response;
        }

        internal Task TapAsync(
            bool isPageCall,
            string selector,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => _channel.TapAsync(selector, modifiers, position, timeout, force ?? false, noWaitAfter, isPageCall, trial);

        internal Task<string> ContentAsync(bool isPageCall) => _channel.ContentAsync(isPageCall);

        internal Task FocusAsync(bool isPageCall, string selector, float? timeout)
            => _channel.FocusAsync(selector, timeout, isPageCall);

        internal Task TypeAsync(bool isPageCall, string selector, string text, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.TypeAsync(selector, text, delay ?? 0, timeout, noWaitAfter, isPageCall);

        internal Task<string> GetAttributeAsync(bool isPageCall, string selector, string name, float? timeout)
            => _channel.GetAttributeAsync(selector, name, timeout, isPageCall);

        internal Task<string> InnerHTMLAsync(bool isPageCall, string selector, float? timeout)
            => _channel.InnerHTMLAsync(selector, timeout, isPageCall);

        internal Task<string> InnerTextAsync(bool isPageCall, string selector, float? timeout)
            => _channel.InnerTextAsync(selector, timeout, isPageCall);

        internal Task<string> GetTextContentAsync(bool isPageCall, string selector, float? timeout)
            => _channel.GetTextContentAsync(selector, timeout, isPageCall);

        internal Task HoverAsync(bool isPageCall, string selector, Position position, IEnumerable<KeyboardModifier> modifiers, bool force, float? timeout, bool? trial)
            => _channel.HoverAsync(selector, position, modifiers, force, timeout, isPageCall, trial);

        internal Task<string[]> PressAsync(bool isPageCall, string selector, string key, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.PressAsync(selector, key, delay ?? 0, timeout, noWaitAfter, isPageCall);

        internal async Task<IReadOnlyCollection<string>> SelectOptionAsync(bool isPageCall, string selector, object[] values, bool? noWaitAfter, float? timeout)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] == null)
                    {
                        throw new PlaywrightException($"options[{i}]: expected object, got null");
                    }
                }
            }

            return (await _channel.SelectOptionAsync(selector, values, timeout, noWaitAfter, isPageCall).ConfigureAwait(false)).ToList().AsReadOnly();
        }

        internal Task DispatchEventAsync(bool isPageCall, string selector, string type, object eventInit, float? timeout)
            => _channel.DispatchEventAsync(
                    selector,
                    type,
                    eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit),
                    timeout,
                    isPageCall);

        internal Task FillAsync(bool isPageCall, string selector, string value, bool? noWaitAfter, float? timeout)
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

        internal Task SetInputFilesAsync(bool isPageCall, string selector, IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => _channel.SetInputFilesAsync(selector, files.Select(f => f.ToFilePayload()).ToArray(), timeout, noWaitAfter, isPageCall);

        internal Task SetInputFilesAsync(bool isPageCall, string selector, IEnumerable<FilePayload> files, float? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(selector, files, timeout, noWaitAfter, isPageCall);

        internal Task ClickAsync(
            bool isPageCall,
            string selector,
            float delay,
            MouseButton button,
            int clickCount,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool? force,
            bool? noWaitAfter,
            bool? trial)
            => _channel.ClickAsync(selector, delay, button, clickCount, modifiers, position, timeout, force ?? false, noWaitAfter, isPageCall, trial);

        internal Task DblClickAsync(
            bool isPageCall,
            string selector,
            float delay,
            MouseButton button,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            float? timeout,
            bool force,
            bool? noWaitAfter,
            bool? trial)
            => _channel.DblClickAsync(selector, delay, button, position, modifiers, timeout, force, noWaitAfter, isPageCall, trial);

        internal Task CheckAsync(bool isPageCall, string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.CheckAsync(selector, position, timeout, force ?? false, noWaitAfter, isPageCall, trial);

        internal Task UncheckAsync(bool isPageCall, string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.UncheckAsync(selector, position, timeout, force ?? false, noWaitAfter, isPageCall, trial);

        internal Task SetContentAsync(bool isPageCall, string html, WaitUntilState waitUntil, float? timeout)
            => _channel.SetContentAsync(html, timeout, waitUntil, isPageCall);

        internal async Task<IElementHandle> QuerySelectorAsync(bool isPageCall, string selector)
            => (await _channel.QuerySelectorAsync(selector, isPageCall).ConfigureAwait(false))?.Object;

        internal async Task<IReadOnlyCollection<IElementHandle>> QuerySelectorAllAsync(bool isPageCall, string selector)
            => (await _channel.QuerySelectorAllAsync(selector, isPageCall).ConfigureAwait(false)).Select(c => ((ElementHandleChannel)c).Object).ToList().AsReadOnly();

        internal async Task<IJSHandle> WaitForFunctionAsync(bool isPageCall, string expression, object args, float? pollingInterval, float? timeout)
             => (await _channel.WaitForFunctionAsync(
                expression: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                isPage: isPageCall,
                timeout: timeout,
                polling: pollingInterval).ConfigureAwait(false)).Object;

        internal async Task<IElementHandle> WaitForSelectorAsync(bool isPageCall, string selector, WaitForSelectorState? state, float? timeout)
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

        internal async Task<IResponse> GotoAsync(bool isPage, string url, WaitUntilState? waitUntil, string referer, float? timeout)
            => (await _channel.GotoAsync(url, timeout, waitUntil, referer, isPage).ConfigureAwait(false))?.Object;

        internal Task<bool> IsCheckedAsync(bool isPageCall, string selector, float? timeout)
            => _channel.IsCheckedAsync(selector, timeout, isPageCall);

        internal Task<bool> IsDisabledAsync(bool isPageCall, string selector, float? timeout)
            => _channel.IsDisabledAsync(selector, timeout, isPageCall);

        internal Task<bool> IsEditableAsync(bool isPageCall, string selector, float? timeout)
            => _channel.IsEditableAsync(selector, timeout, isPageCall);

        internal Task<bool> IsEnabledAsync(bool isPageCall, string selector, float? timeout)
            => _channel.IsEnabledAsync(selector, timeout, isPageCall);

        internal Task<bool> IsHiddenAsync(bool isPageCall, string selector, float? timeout)
            => _channel.IsHiddenAsync(selector, timeout, isPageCall);

        internal Task<bool> IsVisibleAsync(bool isPageCall, string selector, float? timeout)
            => _channel.IsVisibleAsync(selector, timeout, isPageCall);

        private Task WaitForURLAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, float? timeout, WaitUntilState waitUntil)
        {
            if (UrlMatches(Url, urlString, urlRegex, urlFunc))
            {
                return WaitForLoadStateAsync(waitUntil.EnsureDefaultValue(WaitUntilState.Load).ToLoadState(), timeout);
            }

            return WaitForNavigationAsync(urlString, urlRegex, urlFunc, waitUntil, timeout);
        }

        private Waiter SetupNavigationWaiter(float? timeout)
        {
            var waiter = new Waiter();
            waiter.RejectOnEvent<IPage>(Page, PageEvent.Close.Name, new NavigationException("Navigation failed because page was closed!"));
            waiter.RejectOnEvent<IPage>(Page, PageEvent.Crash.Name, new NavigationException("Navigation failed because page was crashed!"));
            waiter.RejectOnEvent<IFrame>(
                Page,
                "FrameDetached",
                new NavigationException("Navigating frame was detached!"),
                e => e == this);
            timeout ??= (Page as Page)?.DefaultNavigationTimeout ?? Playwright.DefaultTimeout;
            waiter.RejectOnTimeout(Convert.ToInt32(timeout), $"Timeout {timeout}ms exceeded.");

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
