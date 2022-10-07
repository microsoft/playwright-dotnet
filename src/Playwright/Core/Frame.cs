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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Frame : ChannelOwnerBase, IChannelOwner<Frame>, IFrame
{
    internal readonly FrameChannel _channel;
    private readonly List<WaitUntilState> _loadStates = new();
    internal readonly List<Frame> _childFrames = new();

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
                if (this.ParentFrame == null && e.Add == WaitUntilState.Load && this.Page != null)
                {
                    (this.Page as Page).FireLoad();
                }
                if (this.ParentFrame == null && e.Add == WaitUntilState.DOMContentLoaded && this.Page != null)
                {
                    (this.Page as Page).FireDOMContentLoaded();
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
    public event EventHandler<WaitUntilState> LoadState;

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Frame> IChannelOwner<Frame>.Channel => _channel;

    public IReadOnlyList<IFrame> ChildFrames => _childFrames;

    public string Name { get; internal set; }

    public string Url { get; internal set; }

    IFrame IFrame.ParentFrame => ParentFrame;

    public Frame ParentFrame { get; }

    public IPage Page { get; internal set; }

    public bool IsDetached { get; internal set; }

    public async Task<IElementHandle> FrameElementAsync()
        => (await _channel.FrameElementAsync().ConfigureAwait(false)).Object;

    public IFrameLocator FrameLocator(string selector)
        => new FrameLocator(this, selector);

    public Task<string> TitleAsync() => _channel.TitleAsync();

    public Task WaitForTimeoutAsync(float timeout)
        => _channel.WaitForTimeoutAsync(timeout);

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
            strict: options?.Strict,
            force: options?.Force,
            timeout: options?.Timeout).ConfigureAwait(false)).ToList().AsReadOnly();

    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, FrameSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    public async Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, FrameSelectOptionOptions options = default)
        => (await _channel.SelectOptionAsync(
            selector,
            values,
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict,
            force: options?.Force,
            timeout: options?.Timeout).ConfigureAwait(false)).ToList().AsReadOnly();

    public async Task WaitForLoadStateAsync(LoadState? state = default, FrameWaitForLoadStateOptions options = default)
    {
        Task<WaitUntilState> task;
        Waiter waiter = null;
        WaitUntilState loadState = Microsoft.Playwright.WaitUntilState.Load;
        switch (state)
        {
            case Microsoft.Playwright.LoadState.Load:
                loadState = Microsoft.Playwright.WaitUntilState.Load;
                break;
            case Microsoft.Playwright.LoadState.DOMContentLoaded:
                loadState = Microsoft.Playwright.WaitUntilState.DOMContentLoaded;
                break;
            case Microsoft.Playwright.LoadState.NetworkIdle:
                loadState = Microsoft.Playwright.WaitUntilState.NetworkIdle;
                break;
        }
        try
        {
            lock (_loadStates)
            {
                if (_loadStates.Contains(loadState))
                {
                    return;
                }

                waiter = SetupNavigationWaiter("frame.WaitForLoadStateAsync", options?.Timeout);
                task = waiter.WaitForEventAsync<WaitUntilState>(this, "LoadState", s =>
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
        using var waiter = SetupNavigationWaiter("frame.WaitForNavigationAsync", options?.Timeout);
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
            await waiter.WaitForPromiseAsync(Task.FromException<object>(ex)).ConfigureAwait(false);
        }

        if (!_loadStates.Select(s => s.ToValueString()).Contains(waitUntil2.ToValueString()))
        {
            await waiter.WaitForEventAsync<WaitUntilState>(
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

        return response;
    }

    public async Task<IResponse> RunAndWaitForNavigationAsync(Func<Task> action, FrameRunAndWaitForNavigationOptions options = default)
    {
        var result = WaitForNavigationAsync(new()
        {
            UrlString = options?.UrlString,
            UrlRegex = options?.UrlRegex,
            UrlFunc = options?.UrlFunc,
            WaitUntil = options?.WaitUntil,
            Timeout = options?.Timeout,
        });
        if (action != null)
        {
            await WrapApiBoundaryAsync(() => Task.WhenAll(result, action())).ConfigureAwait(false);
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
            trial: options?.Trial,
            strict: options?.Strict);

    internal Task<int> QueryCountAsync(string selector)
        => _channel.QueryCountAsync(selector);

    public Task<string> ContentAsync() => _channel.ContentAsync();

    public Task FocusAsync(string selector, FrameFocusOptions options = default)
        => _channel.FocusAsync(selector, options?.Timeout, options?.Strict);

    public Task TypeAsync(string selector, string text, FrameTypeOptions options = default)
        => _channel.TypeAsync(
            selector,
            text,
            delay: options?.Delay,
            timeout: options?.Timeout,
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict);

    public Task<string> GetAttributeAsync(string selector, string name, FrameGetAttributeOptions options = default)
        => _channel.GetAttributeAsync(selector, name, options?.Timeout, options?.Strict);

    public Task<string> InnerHTMLAsync(string selector, FrameInnerHTMLOptions options = default)
        => _channel.InnerHTMLAsync(selector, options?.Timeout, options?.Strict);

    public Task<string> InnerTextAsync(string selector, FrameInnerTextOptions options = default)
        => _channel.InnerTextAsync(selector, options?.Timeout, options?.Strict);

    public Task<string> TextContentAsync(string selector, FrameTextContentOptions options = default)
        => _channel.TextContentAsync(selector, options?.Timeout, options?.Strict);

    public Task HoverAsync(string selector, FrameHoverOptions options = default)
        => _channel.HoverAsync(
            selector,
            position: options?.Position,
            modifiers: options?.Modifiers,
            force: options?.Force,
            timeout: options?.Timeout,
            trial: options?.Trial,
            strict: options?.Strict);

    public Task PressAsync(string selector, string key, FramePressOptions options = default)
        => _channel.PressAsync(
            selector,
            key,
            delay: options?.Delay,
            timeout: options?.Timeout,
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict);

    public Task DispatchEventAsync(string selector, string type, object eventInit = default, FrameDispatchEventOptions options = default)
        => _channel.DispatchEventAsync(
                selector,
                type,
                ScriptsHelper.SerializedArgument(eventInit),
                options?.Timeout,
                options?.Strict);

    public Task FillAsync(string selector, string value, FrameFillOptions options = default)
        => _channel.FillAsync(selector, value, force: options?.Force, timeout: options?.Timeout, noWaitAfter: options?.NoWaitAfter, options?.Strict);

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

    public async Task SetInputFilesAsync(string selector, IEnumerable<string> files, FrameSetInputFilesOptions options = default)
    {
        var converted = await SetInputFilesHelpers.ConvertInputFilesAsync(files, (BrowserContext)Page.Context).ConfigureAwait(false);
        if (converted.Files != null)
        {
            await _channel.SetInputFilesAsync(selector, converted.Files, options?.NoWaitAfter, options?.Timeout, options?.Strict).ConfigureAwait(false);
        }
        else
        {
            await _channel.SetInputFilePathsAsync(selector, converted?.LocalPaths, converted?.Streams, options?.NoWaitAfter, options?.Timeout, options?.Strict).ConfigureAwait(false);
        }
    }

    public Task SetInputFilesAsync(string selector, FilePayload files, FrameSetInputFilesOptions options = default)
        => SetInputFilesAsync(selector, new[] { files }, options);

    public async Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, FrameSetInputFilesOptions options = default)
    {
        var converted = SetInputFilesHelpers.ConvertInputFiles(files);
        await _channel.SetInputFilesAsync(selector, converted.Files, noWaitAfter: options?.NoWaitAfter, timeout: options?.Timeout, options?.Strict).ConfigureAwait(false);
    }

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
            trial: options?.Trial,
            strict: options?.Strict);

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
            trial: options?.Trial,
            strict: options?.Strict);

    public Task CheckAsync(string selector, FrameCheckOptions options = default)
        => _channel.CheckAsync(
            selector,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial,
            strict: options?.Strict);

    public Task UncheckAsync(string selector, FrameUncheckOptions options = default)
        => _channel.UncheckAsync(
            selector,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial,
            strict: options?.Strict);

    public Task SetCheckedAsync(string selector, bool checkedState, FrameSetCheckedOptions options = null)
        => checkedState ?
        _channel.CheckAsync(
            selector,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial,
            strict: options?.Strict)
        : _channel.UncheckAsync(
            selector,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial,
            strict: options?.Strict);

    public Task SetContentAsync(string html, FrameSetContentOptions options = default)
        => _channel.SetContentAsync(html, timeout: options?.Timeout, waitUntil: options?.WaitUntil);

    public Task<string> InputValueAsync(string selector, FrameInputValueOptions options = null)
        => _channel.InputValueAsync(selector, options?.Timeout, options?.Strict);

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
            timeout: options?.Timeout,
            strict: options?.Strict,
            omitReturnValue: false).ConfigureAwait(false))?.Object;

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
            arg: ScriptsHelper.SerializedArgument(arg),
            strict: null).ConfigureAwait(false));

    public async Task<T> EvalOnSelectorAsync<T>(string selector, string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            script,
            arg: ScriptsHelper.SerializedArgument(arg),
            strict: null).ConfigureAwait(false));

    public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg = null, FrameEvalOnSelectorOptions options = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            expression,
            arg: ScriptsHelper.SerializedArgument(arg),
            strict: options?.Strict).ConfigureAwait(false));

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

    public ILocator Locator(string selector, FrameLocatorOptions options = null) =>
        new Locator(this, selector, new()
        {
            Has = options?.Has,
            HasTextString = options?.HasTextString,
            HasTextRegex = options?.HasTextRegex,
        });

    public async Task<IElementHandle> QuerySelectorAsync(string selector, FrameQuerySelectorOptions options = null)
        => (await _channel.QuerySelectorAsync(selector, options?.Strict).ConfigureAwait(false))?.Object;

    public async Task<IResponse> GotoAsync(string url, FrameGotoOptions options = default)
        => (await _channel.GotoAsync(
            url,
            timeout: options?.Timeout,
            waitUntil: options?.WaitUntil,
            referer: options?.Referer).ConfigureAwait(false))?.Object;

    public Task<bool> IsCheckedAsync(string selector, FrameIsCheckedOptions options = default)
        => _channel.IsCheckedAsync(selector, timeout: options?.Timeout, options?.Strict);

    public Task<bool> IsDisabledAsync(string selector, FrameIsDisabledOptions options = default)
        => _channel.IsDisabledAsync(selector, timeout: options?.Timeout, options?.Strict);

    public Task<bool> IsEditableAsync(string selector, FrameIsEditableOptions options = default)
        => _channel.IsEditableAsync(selector, timeout: options?.Timeout, options?.Strict);

    public Task<bool> IsEnabledAsync(string selector, FrameIsEnabledOptions options = default)
        => _channel.IsEnabledAsync(selector, timeout: options?.Timeout, options?.Strict);

#pragma warning disable CS0612 // Type or member is obsolete
    public Task<bool> IsHiddenAsync(string selector, FrameIsHiddenOptions options = default)
        => _channel.IsHiddenAsync(selector, timeout: options?.Timeout, options?.Strict);

    public Task<bool> IsVisibleAsync(string selector, FrameIsVisibleOptions options = default)
        => _channel.IsVisibleAsync(selector, timeout: options?.Timeout, options?.Strict);
#pragma warning restore CS0612 // Type or member is obsolete

    public Task WaitForURLAsync(string url, FrameWaitForURLOptions options = default)
        => WaitForURLAsync(url, null, null, options);

    public Task WaitForURLAsync(Regex url, FrameWaitForURLOptions options = default)
        => WaitForURLAsync(null, url, null, options);

    public Task WaitForURLAsync(Func<string, bool> url, FrameWaitForURLOptions options = default)
        => WaitForURLAsync(null, null, url, options);

    public Task DragAndDropAsync(string source, string target, FrameDragAndDropOptions options = null)
        => _channel.DragAndDropAsync(source, target, options?.Force, options?.NoWaitAfter, options?.Timeout, options?.Trial, options?.Strict, options?.SourcePosition, options?.TargetPosition);

    internal Task<FrameExpectResult> ExpectAsync(string selector, string expression, FrameExpectOptions options = null) =>
        _channel.ExpectAsync(selector, expression, expressionArg: options?.ExpressionArg, expectedText: options?.ExpectedText, expectedNumber: options?.ExpectedNumber, expectedValue: options?.ExpectedValue, useInnerText: options?.UseInnerText, isNot: options?.IsNot, timeout: options?.Timeout);

    private Task WaitForURLAsync(string urlString, Regex urlRegex, Func<string, bool> urlFunc, FrameWaitForURLOptions options = default)
    {
        if (UrlMatches(Url, urlString, urlRegex, urlFunc))
        {
            return WaitForLoadStateAsync(ToLoadState(options?.WaitUntil), new() { Timeout = options?.Timeout });
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

    private Waiter SetupNavigationWaiter(string @event, float? timeout)
    {
        var waiter = new Waiter(this.Page as Page, @event);
        if (this.Page.IsClosed)
        {
            waiter.RejectImmediately(new PlaywrightException("Navigation failed because page was closed!"));
        }
        waiter.RejectOnEvent<IPage>(Page, PageEvent.Close.Name, new("Navigation failed because page was closed!"));
        waiter.RejectOnEvent<IPage>(Page, PageEvent.Crash.Name, new("Navigation failed because page was crashed!"));
        waiter.RejectOnEvent<IFrame>(
            Page,
            "FrameDetached",
            new("Navigating frame was detached!"),
            e => e == this);
        timeout = (Page as Page)?._timeoutSettings.NavigationTimeout(timeout);
        waiter.RejectOnTimeout(Convert.ToInt32(timeout, CultureInfo.InvariantCulture), $"Timeout {timeout}ms exceeded.");

        return waiter;
    }

    private bool UrlMatches(string url, string matchUrl, Regex regex, Func<string, bool> match)
    {
        matchUrl = (Page.Context as BrowserContext)?.CombineUrlWithBase(matchUrl);

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

    internal Task HighlightAsync(string selector)
        => _channel.HighlightAsync(selector);

    public ILocator GetByAltText(string text, FrameGetByAltTextOptions options = null)
        => Locator(Core.Locator.GetByAltTextSelector(text, options?.Exact));

    public ILocator GetByAltText(Regex text, FrameGetByAltTextOptions options = null)
        => Locator(Core.Locator.GetByAltTextSelector(text, options?.Exact));

    public ILocator GetByLabel(string text, FrameGetByLabelOptions options = null)
        => Locator(Core.Locator.GetByLabelSelector(text, options?.Exact));

    public ILocator GetByLabel(Regex text, FrameGetByLabelOptions options = null)
        => Locator(Core.Locator.GetByLabelSelector(text, options?.Exact));

    public ILocator GetByPlaceholder(string text, FrameGetByPlaceholderOptions options = null)
        => Locator(Core.Locator.GetByPlaceholderSelector(text, options?.Exact));

    public ILocator GetByPlaceholder(Regex text, FrameGetByPlaceholderOptions options = null)
        => Locator(Core.Locator.GetByPlaceholderSelector(text, options?.Exact));

    public ILocator GetByRole(AriaRole role, FrameGetByRoleOptions options = null)
        => Locator(Core.Locator.GetByRoleSelector(role, new(options)));

    public ILocator GetByTestId(string testId)
        => Locator(Core.Locator.GetByTestIdSelector(testId));

    public ILocator GetByText(string text, FrameGetByTextOptions options = null)
        => Locator(Core.Locator.GetByTextSelector(text, options?.Exact));

    public ILocator GetByText(Regex text, FrameGetByTextOptions options = null)
        => Locator(Core.Locator.GetByTextSelector(text, options?.Exact));

    public ILocator GetByTitle(string text, FrameGetByTitleOptions options = null)
        => Locator(Core.Locator.GetByTitleSelector(text, options?.Exact));

    public ILocator GetByTitle(Regex text, FrameGetByTitleOptions options = null)
        => Locator(Core.Locator.GetByTitleSelector(text, options?.Exact));
}
