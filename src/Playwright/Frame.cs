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
    internal partial class Frame : ChannelOwnerBase, IChannelOwner<Frame>, IFrame
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

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<Frame> IChannelOwner<Frame>.Channel => _channel;

        public IReadOnlyCollection<IFrame> ChildFrames => ChildFramesList;

        public string Name { get; internal set; }

        public string Url { get; internal set; }

        IFrame IFrame.ParentFrame => ParentFrame;

        public Frame ParentFrame { get; }

        public IPage Page { get; internal set; }

        public bool IsDetached { get; internal set; }

        internal List<Frame> ChildFramesList { get; } = new List<Frame>();

        public async Task<IElementHandle> FrameElementAsync()
            => (await _channel.FrameElementAsync().ConfigureAwait(false)).Object;

        public Task<string> TitleAsync() => _channel.TitleAsync();

        public Task WaitForTimeoutAsync(float timeout) => Task.Delay(Convert.ToInt32(timeout));

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, string value, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { new SelectOptionValue { Value = value } }, noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<string> values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, values.Select(v => new SelectOptionValue { Value = v }), noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, SelectOptionValue value, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new[] { value }, noWaitAfter, timeout);

        public async Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, bool? noWaitAfter, float? timeout)
        {
            return (await _channel.SelectOptionAsync(selector, values, noWaitAfter, timeout).ConfigureAwait(false)).ToList().AsReadOnly();
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IElementHandle value, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(selector, new ElementHandle[] { (ElementHandle)value }, noWaitAfter, timeout);

        public async Task<IReadOnlyCollection<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout)
        {
            return (await _channel.SelectOptionAsync(selector, values.Select(v => (ElementHandle)v), noWaitAfter, timeout).ConfigureAwait(false)).ToList().AsReadOnly();
        }

        public async Task WaitForLoadStateAsync(LoadState? state, float? timeout)
        {
            Task<LoadState> task;
            Waiter waiter = null;
            LoadState loadState = state ?? Microsoft.Playwright.LoadState.Load;

            try
            {
                lock (_loadStates)
                {
                    if (_loadStates.Contains(loadState))
                    {
                        return;
                    }

                    waiter = SetupNavigationWaiter("frame.WaitForLoadStateAsync", timeout);
                    task = waiter.WaitForEventAsync<LoadState>(this, "LoadState", s =>
                    {
                        waiter.Log($"  \"{s}\" event fired");
                        return s == loadState;
                    });
                }

                await task.ConfigureAwait(false);
            }
            finally
            {
                waiter?.Dispose();
            }
        }

        public Task WaitForURLAsync(string urlString, float? timeout = default, WaitUntilState? waitUntil = default)
            => WaitForURLAsync(urlString, null, null, timeout, waitUntil);

        public Task WaitForURLAsync(Regex urlRegex, float? timeout = default, WaitUntilState? waitUntil = default)
            => WaitForURLAsync(null, urlRegex, null, timeout, waitUntil);

        public Task WaitForURLAsync(Func<string, bool> urlFunc, float? timeout = default, WaitUntilState? waitUntil = default)
            => WaitForURLAsync(null, null, urlFunc, timeout, waitUntil);

        public async Task<IResponse> WaitForNavigationAsync(
            string urlString = default,
            Regex urlRegex = default,
            Func<string, bool> urlFunc = default,
            WaitUntilState? waitUntil = default,
            float? timeout = default)
        {
            WaitUntilState waitUntil2 = waitUntil ?? WaitUntilState.Load;
            var waiter = SetupNavigationWaiter("frame.WaitForNavigationAsync", timeout);
            string toUrl = !string.IsNullOrEmpty(urlString) ? $" to \"{urlString}\"" : string.Empty;

            waiter.Log($"waiting for navigation{toUrl} until \"{waitUntil2}\"");

            var navigatedEventTask = waiter.WaitForEventAsync<FrameNavigatedEventArgs>(
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
                });

            var navigatedEvent = await navigatedEventTask.ConfigureAwait(false);

            if (navigatedEvent.Error != null)
            {
                var ex = new PlaywrightException(navigatedEvent.Error);
                var tcs = new TaskCompletionSource<bool>();
                tcs.TrySetException(ex);
                await waiter.WaitForPromiseAsync(tcs.Task).ConfigureAwait(false);
            }

            if (!_loadStates.Select(s => s.ToValueString()).Contains(waitUntil2.ToValueString()))
            {
                await waiter.WaitForEventAsync<LoadState>(
                    this,
                    "LoadState",
                    e =>
                    {
                        waiter.Log($"  \"{e}\" event fired");
                        return e.ToValueString() == waitUntil2.ToValueString();
                    }).ConfigureAwait(false);
            }

            var request = navigatedEvent.NewDocument?.Request?.Object;
            var response = request != null
                ? await waiter.WaitForPromiseAsync(request.FinalRequest.ResponseAsync()).ConfigureAwait(false)
                : null;

            waiter.Dispose();
            return response;
        }

        public Task TapAsync(
            string selector,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => _channel.TapAsync(selector, modifiers, position, timeout, force, noWaitAfter, trial);

        public Task<string> ContentAsync() => _channel.ContentAsync();

        public Task FocusAsync(string selector, float? timeout)
            => _channel.FocusAsync(selector, timeout);

        public Task TypeAsync(string selector, string text, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.TypeAsync(selector, text, delay, timeout, noWaitAfter);

        public Task<string> GetAttributeAsync(string selector, string name, float? timeout)
            => _channel.GetAttributeAsync(selector, name, timeout);

        public Task<string> InnerHTMLAsync(string selector, float? timeout)
            => _channel.InnerHTMLAsync(selector, timeout);

        public Task<string> InnerTextAsync(string selector, float? timeout)
            => _channel.InnerTextAsync(selector, timeout);

        public Task<string> TextContentAsync(string selector, float? timeout)
            => _channel.TextContentAsync(selector, timeout);

        public Task HoverAsync(string selector, Position position, IEnumerable<KeyboardModifier> modifiers, bool? force, float? timeout, bool? trial)
            => _channel.HoverAsync(selector, position, modifiers, force, timeout, trial);

        public Task<string[]> PressAsync(string selector, string key, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.PressAsync(selector, key, delay, timeout, noWaitAfter);

        public Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout)
            => _channel.DispatchEventAsync(
                    selector,
                    type,
                    eventInit = ScriptsHelper.SerializedArgument(eventInit),
                    timeout);

        public Task FillAsync(string selector, string value, bool? noWaitAfter, float? timeout)
            => _channel.FillAsync(selector, value, timeout, noWaitAfter);

        public async Task<IElementHandle> AddScriptTagAsync(string url, string path, string content, string type)
        {
            if (!string.IsNullOrEmpty(path))
            {
                content = File.ReadAllText(path);
                content += "//# sourceURL=" + path.Replace("\n", string.Empty);
            }

            return (await _channel.AddScriptTagAsync(url, path, content, type).ConfigureAwait(false)).Object;
        }

        public async Task<IElementHandle> AddStyleTagAsync(string url, string path, string content)
        {
            if (!string.IsNullOrEmpty(path))
            {
                content = File.ReadAllText(path);
                content += "//# sourceURL=" + path.Replace("\n", string.Empty);
            }

            return (await _channel.AddStyleTagAsync(url, path, content).ConfigureAwait(false)).Object;
        }

        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => _channel.SetInputFilesAsync(selector, files.Select(f => f.ToFilePayload()).ToArray(), noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, bool? noWaitAfter = null, float? timeout = null)
            => _channel.SetInputFilesAsync(selector, files, noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, string files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        public Task SetInputFilesAsync(string selector, FilePayload files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(selector, new[] { files }, noWaitAfter, timeout);

        public Task ClickAsync(
            string selector,
            float? delay,
            MouseButton? button,
            int? clickCount,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool? force,
            bool? noWaitAfter,
            bool? trial)
            => _channel.ClickAsync(selector, delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter, trial);

        public Task DblClickAsync(
            string selector,
            float? delay,
            MouseButton? button,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            float? timeout,
            bool? force,
            bool? noWaitAfter,
            bool? trial)
            => _channel.DblClickAsync(selector, delay, button, position, modifiers, timeout, force, noWaitAfter, trial);

        public Task CheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.CheckAsync(selector, position, timeout, force, noWaitAfter, trial);

        public Task UncheckAsync(string selector, Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.UncheckAsync(selector, position, timeout, force, noWaitAfter, trial);

        public Task SetContentAsync(string html, WaitUntilState? waitUntil, float? timeout)
            => _channel.SetContentAsync(html, timeout, waitUntil);

        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        public async Task<IReadOnlyCollection<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(c => ((ElementHandleChannel)c).Object).ToList().AsReadOnly();

        public async Task<IJSHandle> WaitForFunctionAsync(string expression, object args, float? pollingInterval, float? timeout)
             => (await _channel.WaitForFunctionAsync(
                expression: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args),
                timeout: timeout,
                polling: pollingInterval).ConfigureAwait(false)).Object;

        public async Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout)
            => (await _channel.WaitForSelectorAsync(
                selector: selector,
                state: state,
                timeout: timeout).ConfigureAwait(false))?.Object;

        public async Task<IJSHandle> EvaluateHandleAsync(string script, object args = null)
            => (await _channel.EvaluateExpressionHandleAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false))?.Object;

        public async Task<JsonElement?> EvaluateAsync(string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> EvaluateAsync<T>(string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> EvalOnSelectorAsync<T>(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: script,
                isFunction: script.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<IResponse> GotoAsync(string url, WaitUntilState? waitUntil, string referer, float? timeout)
            => (await _channel.GotoAsync(url, timeout, waitUntil, referer).ConfigureAwait(false))?.Object;

        public Task<bool> IsCheckedAsync(string selector, float? timeout)
            => _channel.IsCheckedAsync(selector, timeout);

        public Task<bool> IsDisabledAsync(string selector, float? timeout)
            => _channel.IsDisabledAsync(selector, timeout);

        public Task<bool> IsEditableAsync(string selector, float? timeout)
            => _channel.IsEditableAsync(selector, timeout);

        public Task<bool> IsEnabledAsync(string selector, float? timeout)
            => _channel.IsEnabledAsync(selector, timeout);

        public Task<bool> IsHiddenAsync(string selector, float? timeout)
            => _channel.IsHiddenAsync(selector, timeout);

        public Task<bool> IsVisibleAsync(string selector, float? timeout)
            => _channel.IsVisibleAsync(selector, timeout);

        private Task WaitForURLAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, float? timeout, WaitUntilState? waitUntil)
        {
            if (UrlMatches(Url, urlString, urlRegex, urlFunc))
            {
                return WaitForLoadStateAsync(ToLoadState(waitUntil), timeout);
            }

            return WaitForNavigationAsync(urlString, urlRegex, urlFunc, waitUntil, timeout);
        }

        private LoadState? ToLoadState(WaitUntilState? waitUntilState)
        {
            if (waitUntilState == null)
            {
                return null;
            }

            return waitUntilState switch
            {
                WaitUntilState.Load => Microsoft.Playwright.LoadState.Load,
                WaitUntilState.DOMContentLoaded => Microsoft.Playwright.LoadState.DOMContentLoaded,
                WaitUntilState.NetworkIdle => Microsoft.Playwright.LoadState.NetworkIdle,
                _ => null,
            };
        }

        private Waiter SetupNavigationWaiter(string apiName, float? timeout)
        {
            var waiter = new Waiter(_channel, apiName);
            waiter.RejectOnEvent<IPage>(Page, PageEvent.Close.Name, new PlaywrightException("Navigation failed because page was closed!"));
            waiter.RejectOnEvent<IPage>(Page, PageEvent.Crash.Name, new PlaywrightException("Navigation failed because page was crashed!"));
            waiter.RejectOnEvent<IFrame>(
                Page,
                "FrameDetached",
                new PlaywrightException("Navigating frame was detached!"),
                e => e == this);
            timeout ??= (Page as Page)?.DefaultNavigationTimeout ?? PlaywrightImpl.DefaultTimeout;
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
