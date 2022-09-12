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
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Locator : ILocator
{
    internal readonly Frame _frame;
    internal readonly string _selector;

    public Locator(Frame parent, string selector, LocatorLocatorOptions options = null)
    {
        _frame = parent;
        _selector = selector;

        var serializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        if (options?.HasTextRegex != null)
        {
            var jsRegex = $"/{options.HasTextRegex.ToString()}/{options.HasTextRegex.Options.GetInlineFlags()}";
            _selector += $" >> has={JsonSerializer.Serialize("text=" + jsRegex, serializerOptions)}";
        }
        else if (options?.HasTextString != null)
        {
            _selector += $" >> :scope:has-text({options.HasTextString.EscapeWithQuotes("\"")})";
        }

        if (options?.Has != null)
        {
            var has = (Locator)options.Has;
            if (has._frame != _frame)
            {
                throw new ArgumentException("Inner \"Has\" locator must belong to the same frame.");
            }
            _selector += " >> has=" + JsonSerializer.Serialize(has._selector, serializerOptions);
        }
    }

    public ILocator First => new Locator(_frame, $"{_selector} >> nth=0");

    public ILocator Last => new Locator(_frame, $"{_selector} >> nth=-1");

    IPage ILocator.Page => _frame.Page;

    public Task<LocatorBoundingBoxResult> BoundingBoxAsync(LocatorBoundingBoxOptions options = null)
        => WithElementAsync(
            async (h, timeout) =>
            {
                var bb = await h.BoundingBoxAsync().ConfigureAwait(false);
                if (bb == null)
                {
                    return null;
                }

                return new LocatorBoundingBoxResult()
                {
                    Height = bb.Height,
                    Width = bb.Width,
                    X = bb.X,
                    Y = bb.Y,
                };
            },
            options?.Timeout);

    public Task CheckAsync(LocatorCheckOptions options = null)
        => _frame.CheckAsync(
            _selector,
            ConvertOptions<FrameCheckOptions>(options));

    public Task ClickAsync(LocatorClickOptions options = null)
        => _frame.ClickAsync(
            _selector,
            ConvertOptions<FrameClickOptions>(options));

    public Task DblClickAsync(LocatorDblClickOptions options = null)
        => _frame.DblClickAsync(_selector, ConvertOptions<FrameDblClickOptions>(options));

    public Task DispatchEventAsync(string type, object eventInit = null, LocatorDispatchEventOptions options = null)
        => _frame.DispatchEventAsync(_selector, type, eventInit, ConvertOptions<FrameDispatchEventOptions>(options));

    public Task DragToAsync(ILocator target, LocatorDragToOptions options = null)
        => _frame.DragAndDropAsync(_selector, ((Locator)target)._selector, ConvertOptions<FrameDragAndDropOptions>(options));

    public Task<JsonElement?> EvaluateAsync(string expression, object arg = null, LocatorEvaluateOptions options = null)
        => EvaluateAsync<JsonElement?>(expression, arg, options);

    public Task<T> EvaluateAsync<T>(string expression, object arg = null, LocatorEvaluateOptions options = null)
        => WithElementAsync(
            (h, _) => h.EvaluateAsync<T>(expression, arg),
            options?.Timeout);

    public Task<T> EvaluateAllAsync<T>(string expression, object arg = null)
        => _frame.EvalOnSelectorAllAsync<T>(_selector, expression, arg);

    public Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null, LocatorEvaluateHandleOptions options = null)
        => WithElementAsync((e, _) => e.EvaluateHandleAsync(expression, arg), options?.Timeout);

    public Task FillAsync(string value, LocatorFillOptions options = null)
        => _frame.FillAsync(_selector, value, ConvertOptions<FrameFillOptions>(options));

    public Task HighlightAsync() => _frame.HighlightAsync(_selector);

    ILocator ILocator.Locator(string selector, LocatorLocatorOptions options)
        => new Locator(_frame, $"{_selector} >> {selector}", options);

    IFrameLocator ILocator.FrameLocator(string selector) =>
        new FrameLocator(_frame, $"{_selector} >> {selector}");

    public ILocator Filter(LocatorFilterOptions options = null) =>
        new Locator(_frame, _selector, new()
        {
            Has = options?.Has,
            HasTextString = options?.HasTextString,
            HasTextRegex = options?.HasTextRegex,
        });

    public Task<IElementHandle> ElementHandleAsync(LocatorElementHandleOptions options = null)
        => _frame.WaitForSelectorAsync(
            _selector,
            ConvertOptions<FrameWaitForSelectorOptions>(options, new() { Strict = true, State = WaitForSelectorState.Attached }));

    public Task<IReadOnlyList<IElementHandle>> ElementHandlesAsync()
        => _frame.QuerySelectorAllAsync(_selector);

    public ILocator Nth(int index) => new Locator(_frame, $"{_selector} >> nth={index}");

    public Task FocusAsync(LocatorFocusOptions options = null)
        => _frame.FocusAsync(_selector, ConvertOptions<FrameFocusOptions>(options));

    public Task<int> CountAsync()
        => _frame.QueryCountAsync(_selector);

    public Task<string> GetAttributeAsync(string name, LocatorGetAttributeOptions options = null)
        => _frame.GetAttributeAsync(_selector, name, ConvertOptions<FrameGetAttributeOptions>(options));

    public Task HoverAsync(LocatorHoverOptions options = null)
        => _frame.HoverAsync(_selector, ConvertOptions<FrameHoverOptions>(options));

    public Task<string> InnerHTMLAsync(LocatorInnerHTMLOptions options = null)
        => _frame.InnerHTMLAsync(_selector, ConvertOptions<FrameInnerHTMLOptions>(options));

    public Task<string> InnerTextAsync(LocatorInnerTextOptions options = null)
        => _frame.InnerTextAsync(_selector, ConvertOptions<FrameInnerTextOptions>(options));

    public Task<string> InputValueAsync(LocatorInputValueOptions options = null)
        => _frame.InputValueAsync(_selector, ConvertOptions<FrameInputValueOptions>(options));

    public Task<bool> IsCheckedAsync(LocatorIsCheckedOptions options = null)
        => _frame.IsCheckedAsync(_selector, ConvertOptions<FrameIsCheckedOptions>(options));

    public Task<bool> IsDisabledAsync(LocatorIsDisabledOptions options = null)
        => _frame.IsDisabledAsync(_selector, ConvertOptions<FrameIsDisabledOptions>(options));

    public Task<bool> IsEditableAsync(LocatorIsEditableOptions options = null)
        => _frame.IsEditableAsync(_selector, ConvertOptions<FrameIsEditableOptions>(options));

    public Task<bool> IsEnabledAsync(LocatorIsEnabledOptions options = null)
        => _frame.IsEnabledAsync(_selector, ConvertOptions<FrameIsEnabledOptions>(options));

    public Task<bool> IsHiddenAsync(LocatorIsHiddenOptions options = null)
        => _frame.IsHiddenAsync(_selector, ConvertOptions<FrameIsHiddenOptions>(options));

    public Task<bool> IsVisibleAsync(LocatorIsVisibleOptions options = null)
        => _frame.IsVisibleAsync(_selector, ConvertOptions<FrameIsVisibleOptions>(options));

    public Task PressAsync(string key, LocatorPressOptions options = null)
        => _frame.PressAsync(_selector, key, ConvertOptions<FramePressOptions>(options));

    public Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions options = null)
        => WithElementAsync((h, timeout) => h.ScreenshotAsync(ConvertOptions<ElementHandleScreenshotOptions>(options, new() { Timeout = timeout })), options?.Timeout);

    public Task ScrollIntoViewIfNeededAsync(LocatorScrollIntoViewIfNeededOptions options = null)
        => WithElementAsync((h, timeout) => h.ScrollIntoViewIfNeededAsync(ConvertOptions<ElementHandleScrollIntoViewIfNeededOptions>(options, new() { Timeout = timeout })), options?.Timeout);

    public Task<IReadOnlyList<string>> SelectOptionAsync(string values, LocatorSelectOptionOptions options = null)
        => _frame.SelectOptionAsync(_selector, values, ConvertOptions<FrameSelectOptionOptions>(options));

    public Task<IReadOnlyList<string>> SelectOptionAsync(IElementHandle values, LocatorSelectOptionOptions options = null)
        => _frame.SelectOptionAsync(_selector, values, ConvertOptions<FrameSelectOptionOptions>(options));

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<string> values, LocatorSelectOptionOptions options = null)
        => _frame.SelectOptionAsync(_selector, values, ConvertOptions<FrameSelectOptionOptions>(options));

    public Task<IReadOnlyList<string>> SelectOptionAsync(SelectOptionValue values, LocatorSelectOptionOptions options = null)
        => _frame.SelectOptionAsync(_selector, values, ConvertOptions<FrameSelectOptionOptions>(options));

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, LocatorSelectOptionOptions options = null)
        => _frame.SelectOptionAsync(_selector, values, ConvertOptions<FrameSelectOptionOptions>(options));

    public Task<IReadOnlyList<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, LocatorSelectOptionOptions options = null)
        => _frame.SelectOptionAsync(_selector, values, ConvertOptions<FrameSelectOptionOptions>(options));

    public Task SelectTextAsync(LocatorSelectTextOptions options = null)
        => WithElementAsync((h, timeout) => h.SelectTextAsync(ConvertOptions<ElementHandleSelectTextOptions>(options, new() { Timeout = timeout })), options?.Timeout);

    public Task SetCheckedAsync(bool @checked, LocatorSetCheckedOptions options = null)
        => @checked ?
            CheckAsync(ConvertOptions<LocatorCheckOptions>(options))
            : UncheckAsync(ConvertOptions<LocatorUncheckOptions>(options));

    public Task SetInputFilesAsync(string files, LocatorSetInputFilesOptions options = null)
        => _frame.SetInputFilesAsync(_selector, files, ConvertOptions<FrameSetInputFilesOptions>(options));

    public Task SetInputFilesAsync(IEnumerable<string> files, LocatorSetInputFilesOptions options = null)
        => _frame.SetInputFilesAsync(_selector, files, ConvertOptions<FrameSetInputFilesOptions>(options));

    public Task SetInputFilesAsync(FilePayload files, LocatorSetInputFilesOptions options = null)
        => _frame.SetInputFilesAsync(_selector, files, ConvertOptions<FrameSetInputFilesOptions>(options));

    public Task SetInputFilesAsync(IEnumerable<FilePayload> files, LocatorSetInputFilesOptions options = null)
        => _frame.SetInputFilesAsync(_selector, files, ConvertOptions<FrameSetInputFilesOptions>(options));

    public Task TapAsync(LocatorTapOptions options = null)
        => _frame.TapAsync(_selector, ConvertOptions<FrameTapOptions>(options));

    public Task<string> TextContentAsync(LocatorTextContentOptions options = null)
        => _frame.TextContentAsync(_selector, ConvertOptions<FrameTextContentOptions>(options));

    public Task TypeAsync(string text, LocatorTypeOptions options = null)
        => _frame.TypeAsync(_selector, text, ConvertOptions<FrameTypeOptions>(options));

    public Task UncheckAsync(LocatorUncheckOptions options = null)
        => _frame.UncheckAsync(_selector, ConvertOptions<FrameUncheckOptions>(options));

    public async Task<IReadOnlyList<string>> AllInnerTextsAsync()
        => await _frame.EvalOnSelectorAllAsync<string[]>(_selector, "ee => ee.map(e => e.innerText)").ConfigureAwait(false);

    public async Task<IReadOnlyList<string>> AllTextContentsAsync()
        => await _frame.EvalOnSelectorAllAsync<string[]>(_selector, "ee => ee.map(e => e.textContent || '')").ConfigureAwait(false);

    public Task WaitForAsync(LocatorWaitForOptions options = null)
        => _frame._channel.WaitForSelectorAsync(
            selector: _selector,
            strict: true,
            omitReturnValue: true,
            state: options?.State,
            timeout: options?.Timeout);

    internal Task<FrameExpectResult> ExpectAsync(string expression, FrameExpectOptions options = null)
        => _frame.ExpectAsync(
            _selector,
            expression,
            options);

    public override string ToString() => "Locator@" + _selector;

    private T ConvertOptions<T>(object source, T inheritFrom = default)
        where T : class, new()
    {
        T target = inheritFrom ?? new();
        var targetType = target.GetType();
        if (source != null)
        {
            var sourceType = source.GetType();
            foreach (var sourceProperty in sourceType.GetProperties())
            {
                var targetProperty = targetType.GetProperty(sourceProperty.Name);
                if (targetProperty != null)
                {
                    targetProperty.SetValue(target, sourceProperty.GetValue(source));
                }
            }
        }
        var strictProperty = targetType.GetProperty("Strict");
        if (strictProperty != null && strictProperty.GetValue(target) == null)
        {
            strictProperty.SetValue(target, true);
        }
        return target;
    }

    private Task<TResult> WithElementAsync<TResult>(Func<IElementHandle, float?, Task<TResult>> task, float? timeout)
    {
        timeout = ((Page)this._frame.Page)._timeoutSettings.Timeout(timeout);
        long? deadline = timeout.HasValue ? Stopwatch.GetTimestamp() + (long)(timeout.Value * 1000 * Stopwatch.Frequency / 1000) : null;

        return this._frame.WrapApiCallAsync(async () =>
        {
            var result = await this._frame._channel.WaitForSelectorAsync(this._selector, state: WaitForSelectorState.Attached, strict: true, timeout: timeout).ConfigureAwait(false);
            var handle = result?.Object;
            if (handle == null)
            {
                throw new PlaywrightException($"Could not resolve {this._selector} to DOM Element");
            }
            float? taskTimeout = deadline.HasValue ? (deadline.Value - Stopwatch.GetTimestamp()) / (float)Stopwatch.Frequency : null;
            try
            {
                return await task(handle, taskTimeout).ConfigureAwait(false);
            }
            finally
            {
                await handle.DisposeAsync().ConfigureAwait(false);
            }
        });
    }

    private async Task WithElementAsync(Func<IElementHandle, float?, Task> callback, float? timeout)
    {
        await WithElementAsync<bool>(
            async (h, t) =>
            {
                await callback(h, t).ConfigureAwait(false);
                return true;
            },
            timeout).ConfigureAwait(false);
    }
}
