/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal partial class ElementHandle : JSHandle, IElementHandle, IChannelOwner<ElementHandle>
{
    private readonly ElementHandleChannel _channel;

    internal ElementHandle(IChannelOwner parent, string guid, ElementHandleInitializer initializer) : base(parent, guid, initializer)
    {
        _channel = new(guid, parent.Connection, this);
        _channel.PreviewUpdated += (_, newPreview) => Preview = newPreview;
    }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<ElementHandle> IChannelOwner<ElementHandle>.Channel => _channel;

    internal IChannel<ElementHandle> ElementChannel => _channel;

    public async Task<IElementHandle> WaitForSelectorAsync(string selector, ElementHandleWaitForSelectorOptions options = default)
        => (await _channel.WaitForSelectorAsync(
            selector: selector,
            state: options?.State,
            timeout: options?.Timeout,
            strict: options?.Strict).ConfigureAwait(false))?.Object;

    public Task WaitForElementStateAsync(ElementState state, ElementHandleWaitForElementStateOptions options = default)
        => _channel.WaitForElementStateAsync(state, timeout: options?.Timeout);

    public Task PressAsync(string key, ElementHandlePressOptions options = default)
        => _channel.PressAsync(
            key,
            delay: options?.Delay,
            timeout: options?.Timeout,
            noWaitAfter: options?.NoWaitAfter);

    public Task TypeAsync(string text, ElementHandleTypeOptions options = default)
        => _channel.TypeAsync(text, delay: options?.Delay, timeout: options?.Timeout, noWaitAfter: options?.NoWaitAfter);

    public async Task<byte[]> ScreenshotAsync(ElementHandleScreenshotOptions options = default)
    {
        options ??= new();
        if (options.Type == null && !string.IsNullOrEmpty(options.Path))
        {
            options.Type = DetermineScreenshotType(options.Path);
        }

        byte[] result = await _channel.ScreenshotAsync(
            options.Path,
            options.OmitBackground,
            options.Type,
            options.Quality,
            options.Mask,
            options.Animations,
            options.Caret,
            options.Scale,
            options.Timeout).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(options.Path))
        {
            Directory.CreateDirectory(new FileInfo(options.Path).Directory.FullName);
            File.WriteAllBytes(options.Path, result);
        }

        return result;
    }

    public Task FillAsync(string value, ElementHandleFillOptions options = default)
        => _channel.FillAsync(
            value,
            noWaitAfter: options?.NoWaitAfter,
            force: options?.Force,
            timeout: options?.Timeout);

    public async Task<IFrame> ContentFrameAsync() => (await _channel.ContentFrameAsync().ConfigureAwait(false))?.Object;

    public Task HoverAsync(ElementHandleHoverOptions options = default)
        => _channel.HoverAsync(
            modifiers: options?.Modifiers,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            trial: options?.Trial);

    public Task ScrollIntoViewIfNeededAsync(ElementHandleScrollIntoViewIfNeededOptions options = default)
        => _channel.ScrollIntoViewIfNeededAsync(options?.Timeout);

    public async Task<IFrame> OwnerFrameAsync() => (await _channel.OwnerFrameAsync().ConfigureAwait(false)).Object;

    public Task<ElementHandleBoundingBoxResult> BoundingBoxAsync() => _channel.BoundingBoxAsync();

    public Task ClickAsync(ElementHandleClickOptions options = default)
        => _channel.ClickAsync(
            delay: options?.Delay,
            button: options?.Button,
            clickCount: options?.ClickCount,
            modifiers: options?.Modifiers,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial);

    public Task DblClickAsync(ElementHandleDblClickOptions options = default)
        => _channel.DblClickAsync(
            delay: options?.Delay,
            button: options?.Button,
            modifiers: options?.Modifiers,
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial);

    public Task SetInputFilesAsync(string files, ElementHandleSetInputFilesOptions options = default)
        => SetInputFilesAsync(new[] { files }, options);

    public async Task SetInputFilesAsync(IEnumerable<string> files, ElementHandleSetInputFilesOptions options = default)
    {
        var frame = await OwnerFrameAsync().ConfigureAwait(false);
        if (frame == null)
        {
            throw new PlaywrightException("Cannot set input files to detached element.");
        }
        var converted = await SetInputFilesHelpers.ConvertInputFilesAsync(files, (BrowserContext)frame.Page.Context).ConfigureAwait(false);
        if (converted.Files != null)
        {
            await _channel.SetInputFilesAsync(converted.Files, options?.NoWaitAfter, options?.Timeout).ConfigureAwait(false);
        }
        else
        {
            await _channel.SetInputFilePathsAsync(converted?.LocalPaths, converted?.Streams, options?.NoWaitAfter, options?.Timeout).ConfigureAwait(false);
        }
    }

    public Task SetInputFilesAsync(FilePayload files, ElementHandleSetInputFilesOptions options = default)
        => SetInputFilesAsync(new[] { files }, options);

    public async Task SetInputFilesAsync(IEnumerable<FilePayload> files, ElementHandleSetInputFilesOptions options = default)
    {
        var converted = SetInputFilesHelpers.ConvertInputFiles(files);
        await _channel.SetInputFilesAsync(converted.Files, options?.NoWaitAfter, options?.Timeout).ConfigureAwait(false);
    }

    public async Task<IElementHandle> QuerySelectorAsync(string selector)
        => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

    public async Task<IReadOnlyList<IElementHandle>> QuerySelectorAllAsync(string selector)
        => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(e => ((ElementHandleChannel)e).Object).ToList().AsReadOnly();

    public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            script: expression,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
            selector: selector,
            script: expression,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg = null)
        => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
            selector: selector,
            script: expression,
            arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

    public Task FocusAsync() => _channel.FocusAsync();

    public Task DispatchEventAsync(string type, object eventInit = null)
        => _channel.DispatchEventAsync(
            type,
            eventInit = ScriptsHelper.SerializedArgument(eventInit));

    public Task<string> GetAttributeAsync(string name) => _channel.GetAttributeAsync(name);

    public Task<string> InnerHTMLAsync() => _channel.InnerHTMLAsync();

    public Task<string> InnerTextAsync() => _channel.InnerTextAsync();

    public Task<string> TextContentAsync() => _channel.TextContentAsync();

    public Task SelectTextAsync(ElementHandleSelectTextOptions options = default)
        => _channel.SelectTextAsync(options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(string values, ElementHandleSelectOptionOptions options = default)
        => _channel.SelectOptionAsync(new[] { new SelectOptionValue() { Value = values } }, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, ElementHandleSelectOptionOptions options = default)
        => _channel.SelectOptionAsync(new[] { values }, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, ElementHandleSelectOptionOptions options = default)
        => _channel.SelectOptionAsync(values.Select(x => new SelectOptionValue() { Value = x }), options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, ElementHandleSelectOptionOptions options = default)
        => _channel.SelectOptionAsync(new[] { values }, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, ElementHandleSelectOptionOptions options = default)
        => _channel.SelectOptionAsync(values, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, ElementHandleSelectOptionOptions options = default)
        => _channel.SelectOptionAsync(values, options?.NoWaitAfter, options?.Force, options?.Timeout);

    public Task CheckAsync(ElementHandleCheckOptions options = default)
        => _channel.CheckAsync(
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial);

    public Task UncheckAsync(ElementHandleUncheckOptions options = default)
        => _channel.UncheckAsync(
            position: options?.Position,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial);

    public Task TapAsync(ElementHandleTapOptions options = default)
        => _channel.TapAsync(
            position: options?.Position,
            modifiers: options?.Modifiers,
            timeout: options?.Timeout,
            force: options?.Force,
            noWaitAfter: options?.NoWaitAfter,
            trial: options?.Trial);

    public Task<bool> IsCheckedAsync() => _channel.IsCheckedAsync();

    public Task<bool> IsDisabledAsync() => _channel.IsDisabledAsync();

    public Task<bool> IsEditableAsync() => _channel.IsEditableAsync();

    public Task<bool> IsEnabledAsync() => _channel.IsEnabledAsync();

    public Task<bool> IsHiddenAsync() => _channel.IsHiddenAsync();

    public Task<bool> IsVisibleAsync() => _channel.IsVisibleAsync();

    public Task<string> InputValueAsync(ElementHandleInputValueOptions options = null)
        => _channel.InputValueAsync(options?.Timeout);

    public Task SetCheckedAsync(bool checkedState, ElementHandleSetCheckedOptions options = null)
            => checkedState
            ? _channel.CheckAsync(
                position: options?.Position,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial)
            : _channel.UncheckAsync(
                position: options?.Position,
                timeout: options?.Timeout,
                force: options?.Force,
                noWaitAfter: options?.NoWaitAfter,
                trial: options?.Trial);

    internal static ScreenshotType DetermineScreenshotType(string path)
    {
        string mimeType = path.MimeType();
        return mimeType switch
        {
            "image/png" => ScreenshotType.Png,
            "image/jpeg" => ScreenshotType.Jpeg,
            _ => throw new ArgumentException($"path: unsupported mime type \"{mimeType}\""),
        };
    }
}
