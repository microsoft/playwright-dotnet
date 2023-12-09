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
using System.Runtime.CompilerServices;
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

    internal override void OnMessage(string method, JsonElement? serverParams)
    {
        switch (method)
        {
            case "navigated":
                var e = serverParams?.ToObject<FrameNavigatedEventArgs>(_connection.DefaultJsonSerializerOptions);

                if (serverParams.Value.TryGetProperty("newDocument", out var documentElement))
                {
                    e.NewDocument = documentElement.ToObject<NavigateDocument>(_connection.DefaultJsonSerializerOptions);
                }

                OnNavigated(e);
                break;
            case "loadstate":
                WaitUntilState? add = null;
                WaitUntilState? remove = null;
                if (serverParams.Value.TryGetProperty("add", out var addElement))
                {
                    add = addElement.ToObject<WaitUntilState>(_connection.DefaultJsonSerializerOptions);
                }
                if (serverParams.Value.TryGetProperty("remove", out var removeElement))
                {
                    remove = removeElement.ToObject<WaitUntilState>(_connection.DefaultJsonSerializerOptions);
                }
                OnLoadState(add, remove);
                break;
        }
    }

    internal void OnLoadState(WaitUntilState? add, WaitUntilState? remove)
    {
        if (add.HasValue)
        {
            _loadStates.Add(add.Value);
            LoadState?.Invoke(this, add.Value);
        }

        if (remove.HasValue)
        {
            _loadStates.Remove(remove.Value);
        }
        if (this.ParentFrame == null && add == WaitUntilState.Load && this.Page != null)
        {
            (this.Page as Page).FireLoad();
        }
        if (this.ParentFrame == null && add == WaitUntilState.DOMContentLoaded && this.Page != null)
        {
            (this.Page as Page).FireDOMContentLoaded();
        }
    }

    internal void OnNavigated(FrameNavigatedEventArgs e)
    {
        Url = e.Url;
        Name = e.Name;
        Navigated?.Invoke(this, e);

        if (string.IsNullOrEmpty(e.Error))
        {
            ((Page)Page)?.OnFrameNavigated(this);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IElementHandle> FrameElementAsync()
        => (await _channel.FrameElementAsync().ConfigureAwait(false)).Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public IFrameLocator FrameLocator(string selector)
        => new FrameLocator(this, selector);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> TitleAsync() => _channel.TitleAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task WaitForTimeoutAsync(float timeout)
        => _channel.WaitForTimeoutAsync(timeout);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string values, FrameSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, FrameSelectOptionOptions options = default)
        => SelectOptionAsync(selector, values.Select(valueOrLabel => new SelectOptionValueProtocol() { ValueOrLabel = valueOrLabel }), options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle values, FrameSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, FrameSelectOptionOptions options = default)
        => (await _channel.SelectOptionAsync(
            selector,
            values.Select(x => x as ElementHandle),
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict,
            force: options?.Force,
            timeout: options?.Timeout).ConfigureAwait(false)).ToList().AsReadOnly();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, FrameSelectOptionOptions options = default)
        => SelectOptionAsync(selector, new[] { values }, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, FrameSelectOptionOptions options = default)
        => SelectOptionAsync(selector, values.Select(value => SelectOptionValueProtocol.From(value)), options);

    internal async Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValueProtocol> values, FrameSelectOptionOptions options = default)
        => (await _channel.SelectOptionAsync(
            selector,
            values,
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict,
            force: options?.Force,
            timeout: options?.Timeout).ConfigureAwait(false)).ToList().AsReadOnly();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task WaitForLoadStateAsync(LoadState? state = default, FrameWaitForLoadStateOptions options = default)
    {
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
            waiter = SetupNavigationWaiter("frame.WaitForLoadStateAsync", options?.Timeout);

            if (_loadStates.Contains(loadState))
            {
                waiter.Log($"  not waiting, \"{state}\" event already fired");
            }
            else
            {
                await waiter.WaitForEventAsync<WaitUntilState>(this, "LoadState", s =>
                {
                    waiter.Log($"  \"{s}\" event fired");
                    return s == loadState;
                }).ConfigureAwait(false);
            }
        }
        finally
        {
            waiter?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IResponse> WaitForNavigationAsync(FrameWaitForNavigationOptions options = default)
    {
        return RunAndWaitForNavigationAsync(null, new()
        {
            Url = options?.Url,
            UrlString = options?.UrlString,
            UrlRegex = options?.UrlRegex,
            UrlFunc = options?.UrlFunc,
            WaitUntil = options?.WaitUntil,
            Timeout = options?.Timeout,
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IResponse> RunAndWaitForNavigationAsync(Func<Task> action, FrameRunAndWaitForNavigationOptions options = default)
    {
        using var waiter = SetupNavigationWaiter("frame.WaitForNavigationAsync", options?.Timeout);
        var result = WaitForNavigationInternalAsync(waiter, options?.Url, options?.UrlFunc, options?.UrlRegex, options?.UrlString, options?.WaitUntil);

        if (action != null)
        {
            await WrapApiBoundaryAsync(() => waiter.CancelWaitOnExceptionAsync(result, action))
                .ConfigureAwait(false);
        }

        return await result.ConfigureAwait(false);
    }

    private async Task<IResponse> WaitForNavigationInternalAsync(
        Waiter waiter,
        string url,
        Func<string, bool> urlFunc,
        Regex urlRegex,
        string urlString,
        WaitUntilState? waitUntil)
    {
        WaitUntilState waitUntilNormalized = waitUntil ?? WaitUntilState.Load;
        string urlStringNormalized = !string.IsNullOrEmpty(url) ? url! : urlString!;
        string toUrl = !string.IsNullOrEmpty(urlString) ? $" to \"{urlString}\"" : string.Empty;

        waiter.Log($"waiting for navigation{toUrl} until \"{waitUntilNormalized}\"");

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
                return UrlMatches(e.Url, urlStringNormalized, urlRegex, urlFunc);
            });

        var navigatedEvent = await navigatedEventTask.ConfigureAwait(false);

        if (navigatedEvent.Error != null)
        {
            var ex = new PlaywrightException(navigatedEvent.Error);
            await waiter.WaitForPromiseAsync(Task.FromException<object>(ex)).ConfigureAwait(false);
        }

        if (!_loadStates.Select(s => s.ToValueString()).Contains(waitUntilNormalized.ToValueString()))
        {
            await waiter.WaitForEventAsync<WaitUntilState>(
                this,
                "LoadState",
                e =>
                {
                    waiter.Log($"  \"{e}\" event fired");
                    return e.ToValueString() == waitUntilNormalized.ToValueString();
                }).ConfigureAwait(false);
        }

        var request = navigatedEvent.NewDocument?.Request?.Object;
        var response = request != null
            ? await waiter.WaitForPromiseAsync(request.FinalRequest.ResponseAsync()).ConfigureAwait(false)
            : null;

        return response;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> ContentAsync() => _channel.ContentAsync();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task FocusAsync(string selector, FrameFocusOptions options = default)
        => _channel.FocusAsync(selector, options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task TypeAsync(string selector, string text, FrameTypeOptions options = default)
        => _channel.TypeAsync(
            selector,
            text,
            delay: options?.Delay,
            timeout: options?.Timeout,
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> GetAttributeAsync(string selector, string name, FrameGetAttributeOptions options = default)
        => _channel.GetAttributeAsync(selector, name, options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> InnerHTMLAsync(string selector, FrameInnerHTMLOptions options = default)
        => _channel.InnerHTMLAsync(selector, options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> InnerTextAsync(string selector, FrameInnerTextOptions options = default)
        => _channel.InnerTextAsync(selector, options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> TextContentAsync(string selector, FrameTextContentOptions options = default)
        => _channel.TextContentAsync(selector, options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task HoverAsync(string selector, FrameHoverOptions options = default)
        => _channel.HoverAsync(
            selector,
            position: options?.Position,
            modifiers: options?.Modifiers,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            timeout: options?.Timeout,
            trial: options?.Trial,
            strict: options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task PressAsync(string selector, string key, FramePressOptions options = default)
        => _channel.PressAsync(
            selector,
            key,
            delay: options?.Delay,
            timeout: options?.Timeout,
            noWaitAfter: options?.NoWaitAfter,
            strict: options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task DispatchEventAsync(string selector, string type, object eventInit = default, FrameDispatchEventOptions options = default)
        => _channel.DispatchEventAsync(
                selector,
                type,
                ScriptsHelper.SerializedArgument(eventInit),
                options?.Timeout,
                options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task FillAsync(string selector, string value, FrameFillOptions options = default)
        => _channel.FillAsync(selector, value, force: options?.Force, timeout: options?.Timeout, noWaitAfter: options?.NoWaitAfter, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetInputFilesAsync(string selector, string files, FrameSetInputFilesOptions options = default)
        => SetInputFilesAsync(selector, new[] { files }, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task SetInputFilesAsync(string selector, IEnumerable<string> files, FrameSetInputFilesOptions options = default)
    {
        var converted = await SetInputFilesHelpers.ConvertInputFilesAsync(files, (BrowserContext)Page.Context).ConfigureAwait(false);
        await _channel.SetInputFilesAsync(selector, converted, options?.NoWaitAfter, options?.Timeout, options?.Strict).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetInputFilesAsync(string selector, FilePayload files, FrameSetInputFilesOptions options = default)
        => SetInputFilesAsync(selector, new[] { files }, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, FrameSetInputFilesOptions options = default)
    {
        var converted = SetInputFilesHelpers.ConvertInputFiles(files);
        await _channel.SetInputFilesAsync(selector, converted, noWaitAfter: options?.NoWaitAfter, timeout: options?.Timeout, options?.Strict).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task CheckAsync(string selector, FrameCheckOptions options = default)
        => _channel.CheckAsync(
            selector,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial,
            strict: options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task UncheckAsync(string selector, FrameUncheckOptions options = default)
        => _channel.UncheckAsync(
            selector,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial,
            strict: options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetContentAsync(string html, FrameSetContentOptions options = default)
        => _channel.SetContentAsync(html, timeout: options?.Timeout, waitUntil: options?.WaitUntil);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<string> InputValueAsync(string selector, FrameInputValueOptions options = null)
        => _channel.InputValueAsync(selector, options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IElementHandle> QuerySelectorAsync(string selector)
        => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
        => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(c => ((ElementHandleChannel)c).Object).ToList().AsReadOnly();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IJSHandle> WaitForFunctionAsync(string expression, object arg = default, FrameWaitForFunctionOptions options = default)
         => (await _channel.WaitForFunctionAsync(
            expression: expression,
            arg: ScriptsHelper.SerializedArgument(arg),
            timeout: options?.Timeout,
            polling: options?.PollingInterval).ConfigureAwait(false)).Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IElementHandle> WaitForSelectorAsync(string selector, FrameWaitForSelectorOptions options = default)
        => (await _channel.WaitForSelectorAsync(
            selector: selector,
            state: options?.State,
            timeout: options?.Timeout,
            strict: options?.Strict,
            omitReturnValue: false).ConfigureAwait(false))?.Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IJSHandle> EvaluateHandleAsync(string script, object args = null)
        => (await _channel.EvaluateExpressionHandleAsync(
            script,
            arg: ScriptsHelper.SerializedArgument(args)).ConfigureAwait(false))?.Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<JsonElement?> EvaluateAsync(string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvaluateExpressionAsync(
            script,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> EvaluateAsync<T>(string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvaluateExpressionAsync(
            script,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            script,
            arg: ScriptsHelper.SerializedArgument(arg),
            strict: null).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> EvalOnSelectorAsync<T>(string selector, string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            script,
            arg: ScriptsHelper.SerializedArgument(arg),
            strict: null).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg = null, FrameEvalOnSelectorOptions options = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            expression,
            arg: ScriptsHelper.SerializedArgument(arg),
            strict: options?.Strict).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
            selector: selector,
            script,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string script, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
            selector: selector,
            script,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator Locator(string selector, FrameLocatorOptions options = null) =>
        new Locator(this, selector, new()
        {
            Has = options?.Has,
            HasNot = options?.HasNot,
            HasText = options?.HasText,
            HasTextString = options?.HasTextString,
            HasTextRegex = options?.HasTextRegex,
            HasNotText = options?.HasNotText,
            HasNotTextString = options?.HasNotTextString,
            HasNotTextRegex = options?.HasNotTextRegex,
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IElementHandle> QuerySelectorAsync(string selector, FrameQuerySelectorOptions options = null)
        => (await _channel.QuerySelectorAsync(selector, options?.Strict).ConfigureAwait(false))?.Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IResponse> GotoAsync(string url, FrameGotoOptions options = default)
        => (await _channel.GotoAsync(
            url,
            timeout: options?.Timeout,
            waitUntil: options?.WaitUntil,
            referer: options?.Referer).ConfigureAwait(false))?.Object;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<bool> IsCheckedAsync(string selector, FrameIsCheckedOptions options = default)
        => _channel.IsCheckedAsync(selector, timeout: options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<bool> IsDisabledAsync(string selector, FrameIsDisabledOptions options = default)
        => _channel.IsDisabledAsync(selector, timeout: options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<bool> IsEditableAsync(string selector, FrameIsEditableOptions options = default)
        => _channel.IsEditableAsync(selector, timeout: options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<bool> IsEnabledAsync(string selector, FrameIsEnabledOptions options = default)
        => _channel.IsEnabledAsync(selector, timeout: options?.Timeout, options?.Strict);

#pragma warning disable CS0612 // Type or member is obsolete
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<bool> IsHiddenAsync(string selector, FrameIsHiddenOptions options = default)
        => _channel.IsHiddenAsync(selector, timeout: options?.Timeout, options?.Strict);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<bool> IsVisibleAsync(string selector, FrameIsVisibleOptions options = default)
        => _channel.IsVisibleAsync(selector, timeout: options?.Timeout, options?.Strict);
#pragma warning restore CS0612 // Type or member is obsolete

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task WaitForURLAsync(string url, FrameWaitForURLOptions options = default)
        => WaitForURLAsync(url, null, null, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task WaitForURLAsync(Regex url, FrameWaitForURLOptions options = default)
        => WaitForURLAsync(null, url, null, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task WaitForURLAsync(Func<string, bool> url, FrameWaitForURLOptions options = default)
        => WaitForURLAsync(null, null, url, options);

    [MethodImpl(MethodImplOptions.NoInlining)]
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
        if (Page.IsClosed)
        {
            waiter.RejectImmediately(((Page)Page)._closeErrorWithReason());
        }
        waiter.RejectOnEvent<IPage>(Page, PageEvent.Close.Name, () => ((Page)Page)._closeErrorWithReason());
        waiter.RejectOnEvent<IPage>(Page, PageEvent.Crash.Name, new PlaywrightException("Navigation failed because page was crashed!"));
        waiter.RejectOnEvent<IFrame>(
            Page,
            "FrameDetached",
            new PlaywrightException("Navigating frame was detached!"),
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByAltText(string text, FrameGetByAltTextOptions options = null)
        => Locator(Core.Locator.GetByAltTextSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByAltText(Regex text, FrameGetByAltTextOptions options = null)
        => Locator(Core.Locator.GetByAltTextSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByLabel(string text, FrameGetByLabelOptions options = null)
        => Locator(Core.Locator.GetByLabelSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByLabel(Regex text, FrameGetByLabelOptions options = null)
        => Locator(Core.Locator.GetByLabelSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByPlaceholder(string text, FrameGetByPlaceholderOptions options = null)
        => Locator(Core.Locator.GetByPlaceholderSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByPlaceholder(Regex text, FrameGetByPlaceholderOptions options = null)
        => Locator(Core.Locator.GetByPlaceholderSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByRole(AriaRole role, FrameGetByRoleOptions options = null)
        => Locator(Core.Locator.GetByRoleSelector(role, new(options)));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByTestId(string testId)
        => Locator(Core.Locator.GetByTestIdSelector(Core.Locator.TestIdAttributeName(), testId));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByTestId(Regex testId)
        => Locator(Core.Locator.GetByTestIdSelector(Core.Locator.TestIdAttributeName(), testId));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByText(string text, FrameGetByTextOptions options = null)
        => Locator(Core.Locator.GetByTextSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByText(Regex text, FrameGetByTextOptions options = null)
        => Locator(Core.Locator.GetByTextSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByTitle(string text, FrameGetByTitleOptions options = null)
        => Locator(Core.Locator.GetByTitleSelector(text, options?.Exact));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ILocator GetByTitle(Regex text, FrameGetByTitleOptions options = null)
        => Locator(Core.Locator.GetByTitleSelector(text, options?.Exact));
}
