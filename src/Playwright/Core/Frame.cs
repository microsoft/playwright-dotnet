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

namespace Microsoft.Playwright.Core
{
    internal partial class Frame : ChannelOwnerBase, IChannelOwner<Frame>, IFrame
    {
        private readonly FrameChannel _channel;
        private readonly List<LoadState> _loadStates = new();

        internal Frame(IChannelOwner parent, string guid, FrameInitializer initializer) : base(parent, guid)
        {
            _channel = new(guid, parent.Connection, this);
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

        public IReadOnlyList<IFrame> ChildFrames => ChildFramesList;

        public string Name { get; internal set; }

        public string Url { get; internal set; }

        IFrame IFrame.ParentFrame => ParentFrame;

        public Frame ParentFrame { get; }

        public IPage Page { get; internal set; }

        public bool IsDetached { get; internal set; }

        internal List<Frame> ChildFramesList { get; } = new();

        public async Task<IElementHandle> FrameElementAsync()
            => (await _channel.FrameElementAsync().ConfigureAwait(false)).Object;

        public Task<string> TitleAsync() => _channel.TitleAsync();

        public Task WaitForTimeoutAsync(float timeout) => Task.Delay(Convert.ToInt32(timeout));

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string values, FrameSelectOptionOptions options = default)
            => SelectOptionAsync(selector, new[] { values }, options);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, FrameSelectOptionOptions options = default)
            => SelectOptionAsync(selector, values.Select(x => new SelectOptionValue() { Value = x }), options);

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle values, FrameSelectOptionOptions options = default)
            => SelectOptionAsync(selector, new[] { values }, options);

        public async Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, FrameSelectOptionOptions options = default)
            => (await _channel.SelectOptionAsync(
                selector,
                values.Select(x => x as ElementHandle),
                noWaitAfter: options?.NoWaitAfter,
                timeout: options?.Timeout).ConfigureAwait(false)).ToList().AsReadOnly();

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, FrameSelectOptionOptions options = default)
            => SelectOptionAsync(selector, new[] { values }, options);

        public async Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, FrameSelectOptionOptions options = default)
            => (await _channel.SelectOptionAsync(
                selector,
                values,
                noWaitAfter: options?.NoWaitAfter,
                timeout: options?.Timeout).ConfigureAwait(false)).ToList().AsReadOnly();

        public async Task WaitForLoadStateAsync(LoadState? state = default, FrameWaitForLoadStateOptions options = default)
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

                    waiter = SetupNavigationWaiter("frame.WaitForLoadStateAsync", options?.Timeout);
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

        public async Task<IResponse> WaitForNavigationAsync(FrameWaitForNavigationOptions options = default)
        {
            WaitUntilState waitUntil2 = options?.WaitUntil ?? WaitUntilState.Load;
            var waiter = SetupNavigationWaiter("frame.WaitForNavigationAsync", options?.Timeout);
            string toUrl = !string.IsNullOrEmpty(options?.UrlString) ? $" to \"{options?.UrlString}\"" : string.Empty;

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
                    return UrlMatches(e.Url, options?.UrlString, options?.UrlRegex, options?.UrlFunc);
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

        public async Task<IResponse> RunAndWaitForNavigationAsync(Func<Task> action, FrameRunAndWaitForNavigationOptions options = default)
        {
            var result = WaitForNavigationAsync(options == null ? null :
                new()
                {
                    UrlString = options?.UrlString,
                    UrlRegex = options?.UrlRegex,
                    UrlFunc = options?.UrlFunc,
                    WaitUntil = options?.WaitUntil,
                    Timeout = options?.Timeout,
                });
            if (action != null)
            {
                await Task.WhenAll(result, action()).ConfigureAwait(false);
            }

            return await result.ConfigureAwait(false);
        }

        public Task TapAsync(string selector, FrameTapOptions options = default)
            => _channel.TapAsync(
                selector,
                modifiers: options?.Modifiers,
                position: options?.Position,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial);

        public Task<string> ContentAsync() => _channel.ContentAsync();

        public Task FocusAsync(string selector, FrameFocusOptions options = default)
            => _channel.FocusAsync(selector, options?.Timeout);

        public Task TypeAsync(string selector, string text, FrameTypeOptions options = default)
            => _channel.TypeAsync(
                selector,
                text,
                delay: options?.Delay,
                timeout: options?.Timeout,
                noWaitAfter: options?.NoWaitAfter);

        public Task<string> GetAttributeAsync(string selector, string name, FrameGetAttributeOptions options = default)
            => _channel.GetAttributeAsync(selector, name, options?.Timeout);

        public Task<string> InnerHTMLAsync(string selector, FrameInnerHTMLOptions options = default)
            => _channel.InnerHTMLAsync(selector, options?.Timeout);

        public Task<string> InnerTextAsync(string selector, FrameInnerTextOptions options = default)
            => _channel.InnerTextAsync(selector, options?.Timeout);

        public Task<string> TextContentAsync(string selector, FrameTextContentOptions options = default)
            => _channel.TextContentAsync(selector, options?.Timeout);

        public Task HoverAsync(string selector, FrameHoverOptions options = default)
            => _channel.HoverAsync(
                selector,
                position: options?.Position,
                modifiers: options?.Modifiers,
                force: options?.Force,
                timeout: options?.Timeout,
                trial: options?.Trial);

        public Task PressAsync(string selector, string key, FramePressOptions options = default)
            => _channel.PressAsync(
                selector,
                key,
                delay: options?.Delay,
                timeout: options?.Timeout,
                noWaitAfter: options?.NoWaitAfter);

        public Task DispatchEventAsync(string selector, string type, object eventInit = default, FrameDispatchEventOptions options = default)
            => _channel.DispatchEventAsync(
                    selector,
                    type,
                    ScriptsHelper.SerializedArgument(eventInit),
                    options?.Timeout);

        public Task FillAsync(string selector, string value, FrameFillOptions options = default)
            => _channel.FillAsync(selector, value, timeout: options?.Timeout, noWaitAfter: options?.NoWaitAfter);

        public async Task<IElementHandle> AddScriptTagAsync(FrameAddScriptTagOptions options = default)
        {
            var content = options?.Content;
            if (!string.IsNullOrEmpty(options?.Path))
            {
                content = File.ReadAllText(options.Path);
                content += "//# sourceURL=" + options.Path.Replace("\n", string.Empty);
            }

            return (await _channel.AddScriptTagAsync(options?.Url, options?.Path, content, options?.Type).ConfigureAwait(false)).Object;
        }

        public async Task<IElementHandle> AddStyleTagAsync(FrameAddStyleTagOptions options = default)
        {
            var content = options?.Content;
            if (!string.IsNullOrEmpty(options?.Path))
            {
                content = File.ReadAllText(options.Path);
                content += "//# sourceURL=" + options.Path.Replace("\n", string.Empty);
            }

            return (await _channel.AddStyleTagAsync(options?.Url, options?.Path, content).ConfigureAwait(false)).Object;
        }

        public Task SetInputFilesAsync(string selector, string files, FrameSetInputFilesOptions options = default)
            => SetInputFilesAsync(selector, new[] { files }, options);

        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, FrameSetInputFilesOptions options = default)
            => SetInputFilesAsync(selector, files.Select(x => x.ToFilePayload()), options);

        public Task SetInputFilesAsync(string selector, FilePayload files, FrameSetInputFilesOptions options = default)
            => SetInputFilesAsync(selector, new[] { files }, options);

        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, FrameSetInputFilesOptions options = default)
            => _channel.SetInputFilesAsync(selector, files, noWaitAfter: options?.NoWaitAfter, timeout: options?.Timeout);

        public Task ClickAsync(string selector, FrameClickOptions options = default)
            => _channel.ClickAsync(
                selector,
                delay: options?.Delay,
                button: options?.Button,
                clickCount: options?.ClickCount,
                modifiers: options?.Modifiers,
                position: options?.Position,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial);

        public Task DblClickAsync(string selector, FrameDblClickOptions options = default)
            => _channel.DblClickAsync(
                selector,
                delay: options?.Delay,
                button: options?.Button,
                position: options?.Position,
                modifiers: options?.Modifiers,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial);

        public Task CheckAsync(string selector, FrameCheckOptions options = default)
            => _channel.CheckAsync(
                selector,
                position: options?.Position,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial);

        public Task UncheckAsync(string selector, FrameUncheckOptions options = default)
            => _channel.UncheckAsync(
                selector,
                position: options?.Position,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial);

        public Task SetContentAsync(string html, FrameSetContentOptions options = default)
            => _channel.SetContentAsync(html, timeout: options?.Timeout, waitUntil: options?.WaitUntil);

        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        public async Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(c => ((ElementHandleChannel)c).Object).ToList().AsReadOnly();

        public async Task<IJSHandle> WaitForFunctionAsync(string expression, object arg = default, FrameWaitForFunctionOptions options = default)
             => (await _channel.WaitForFunctionAsync(
                expression: expression,
                arg: ScriptsHelper.SerializedArgument(arg),
                timeout: options?.Timeout,
                polling: options?.PollingInterval).ConfigureAwait(false)).Object;

        public async Task<IElementHandle> WaitForSelectorAsync(string selector, FrameWaitForSelectorOptions options = default)
            => (await _channel.WaitForSelectorAsync(
                selector: selector,
                state: options?.State,
                timeout: options?.Timeout).ConfigureAwait(false))?.Object;

        public async Task<IJSHandle> EvaluateHandleAsync(string script, object args = null)
            => (await _channel.EvaluateExpressionHandleAsync(
                script,
                arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false))?.Object;

        public async Task<JsonElement?> EvaluateAsync(string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
                script,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> EvaluateAsync<T>(string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
                script,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> EvalOnSelectorAsync<T>(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string script, object arg = null)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script,
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        public async Task<IResponse> GotoAsync(string url, FrameGotoOptions options = default)
            => (await _channel.GotoAsync(
                url,
                timeout: options?.Timeout,
                waitUntil: options?.WaitUntil,
                referer: options?.Referer).ConfigureAwait(false))?.Object;

        public Task<bool> IsCheckedAsync(string selector, FrameIsCheckedOptions options = default)
            => _channel.IsCheckedAsync(selector, timeout: options?.Timeout);

        public Task<bool> IsDisabledAsync(string selector, FrameIsDisabledOptions options = default)
            => _channel.IsDisabledAsync(selector, timeout: options?.Timeout);

        public Task<bool> IsEditableAsync(string selector, FrameIsEditableOptions options = default)
            => _channel.IsEditableAsync(selector, timeout: options?.Timeout);

        public Task<bool> IsEnabledAsync(string selector, FrameIsEnabledOptions options = default)
            => _channel.IsEnabledAsync(selector, timeout: options?.Timeout);

        public Task<bool> IsHiddenAsync(string selector, FrameIsHiddenOptions options = default)
            => _channel.IsHiddenAsync(selector, timeout: options?.Timeout);

        public Task<bool> IsVisibleAsync(string selector, FrameIsVisibleOptions options = default)
            => _channel.IsVisibleAsync(selector, timeout: options?.Timeout);

        public Task WaitForURLAsync(string url, FrameWaitForURLOptions options = default)
            => WaitForURLAsync(url, null, null, options);

        public Task WaitForURLAsync(Regex url, FrameWaitForURLOptions options = default)
            => WaitForURLAsync(null, url, null, options);

        public Task WaitForURLAsync(Func<string, bool> url, FrameWaitForURLOptions options = default)
            => WaitForURLAsync(null, null, url, options);

        private Task WaitForURLAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, FrameWaitForURLOptions options = default)
        {
            if (UrlMatches(Url, urlString, urlRegex, urlFunc))
            {
                return WaitForLoadStateAsync(ToLoadState(options?.WaitUntil), options != null ? new() { Timeout = options?.Timeout } : null);
            }

            return WaitForNavigationAsync(
                new()
                {
                    UrlString = urlString,
                    UrlRegex = urlRegex,
                    UrlFunc = urlFunc,
                    Timeout = options?.Timeout,
                    WaitUntil = options?.WaitUntil,
                });
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
            waiter.RejectOnEvent<IPage>(Page, PageEvent.Close.Name, new("Navigation failed because page was closed!"));
            waiter.RejectOnEvent<IPage>(Page, PageEvent.Crash.Name, new("Navigation failed because page was crashed!"));
            waiter.RejectOnEvent<IFrame>(
                Page,
                "FrameDetached",
                new("Navigating frame was detached!"),
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
                regex = new(matchUrl.GlobToRegex());
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
