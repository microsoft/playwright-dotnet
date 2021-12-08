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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core
{
    internal class Locator : ILocator
    {
        private readonly Frame _frame;
        private readonly string _selector;

        public Locator(Frame parent, string selector)
        {
            _frame = parent;
            _selector = selector;
        }

        public ILocator First => new Locator(_frame, $"{_selector} >> nth=0");

        public ILocator Last => new Locator(_frame, $"{_selector} >> nth=-1");

        public async Task<IReadOnlyList<string>> AllInnerTextsAsync()
        {
            var handles = await _frame.QuerySelectorAllAsync(_selector).ConfigureAwait(false);
            return (await handles
                    .SelectAsync(async x => await x.InnerTextAsync().ConfigureAwait(false))
                    .ConfigureAwait(false))
                .ToArray();
        }

        public async Task<IReadOnlyList<string>> AllTextContentsAsync()
        {
            var handles = await _frame.QuerySelectorAllAsync(_selector).ConfigureAwait(false);
            return (await handles
                    .SelectAsync(async x => await x.TextContentAsync().ConfigureAwait(false))
                    .ConfigureAwait(false))
                .Select(x => x ?? string.Empty) // we don't filter nulls, as per https://github.com/microsoft/playwright/blob/master/src/client/locator.ts#L205
                .ToArray();
        }

        public async Task<LocatorBoundingBoxResult> BoundingBoxAsync(LocatorBoundingBoxOptions options = null)
            => await WithElementAsync(
                async (h, _) =>
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
                options).ConfigureAwait(false);

        public Task CheckAsync(LocatorCheckOptions options = null)
            => _frame.CheckAsync(
                _selector,
                ConvertOptions<FrameCheckOptions>(options));

        public Task ClickAsync(LocatorClickOptions options = null)
            => _frame.ClickAsync(
                _selector,
                ConvertOptions<FrameClickOptions>(options));

        public Task SetCheckedAsync(bool checkedState, LocatorSetCheckedOptions options = null)
            => checkedState ?
                CheckAsync(ConvertOptions<LocatorCheckOptions>(options))
                : UncheckAsync(ConvertOptions<LocatorUncheckOptions>(options));

        public Task<int> CountAsync()
            => EvaluateAllAsync<int>("ee => ee.length");

        public Task DblClickAsync(LocatorDblClickOptions options = null)
            => _frame.DblClickAsync(_selector, ConvertOptions<FrameDblClickOptions>(options));

        public Task DispatchEventAsync(string type, object eventInit = null, LocatorDispatchEventOptions options = null)
            => _frame.DispatchEventAsync(_selector, type, eventInit, ConvertOptions<FrameDispatchEventOptions>(options));

        public Task DragToAsync(ILocator target, LocatorDragToOptions options = null)
            => _frame.DragAndDropAsync(_selector, ((Locator)target)._selector, ConvertOptions<FrameDragAndDropOptions>(options));

        public async Task<IElementHandle> ElementHandleAsync(LocatorElementHandleOptions options = null)
            => await _frame.WaitForSelectorAsync(
                _selector,
                ConvertOptions<FrameWaitForSelectorOptions>(options, (o) => o.State = WaitForSelectorState.Attached)).ConfigureAwait(false);

        public Task<IReadOnlyList<IElementHandle>> ElementHandlesAsync()
            => _frame.QuerySelectorAllAsync(_selector);

        public Task<T> EvaluateAllAsync<T>(string expression, object arg = null)
            => _frame.EvalOnSelectorAllAsync<T>(_selector, expression, arg);

        public Task<JsonElement?> EvaluateAsync(string expression, object arg = null, LocatorEvaluateOptions options = null)
            => EvaluateAsync<JsonElement?>(expression, arg, options);

        public Task<T> EvaluateAsync<T>(string expression, object arg = null, LocatorEvaluateOptions options = null)
            => _frame.EvalOnSelectorAsync<T>(_selector, expression, arg, ConvertOptions<FrameEvalOnSelectorOptions>(options));

        public async Task<IJSHandle> EvaluateHandleAsync(string expression, object arg = null, LocatorEvaluateHandleOptions options = null)
            => await WithElementAsync(async (e, _) => await e.EvaluateHandleAsync(expression, arg).ConfigureAwait(false), options).ConfigureAwait(false);

        public async Task FillAsync(string value, LocatorFillOptions options = null)
            => await _frame.FillAsync(_selector, value, ConvertOptions<FrameFillOptions>(options)).ConfigureAwait(false);

        public Task FocusAsync(LocatorFocusOptions options = null)
            => _frame.FocusAsync(_selector, ConvertOptions<FrameFocusOptions>(options));

        IFrameLocator ILocator.FrameLocator(string selector) =>
            new FrameLocator(_frame, $"{_selector} >> {selector}");

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

        public ILocator Nth(int index)
            => new Locator(_frame, $"{_selector} >> nth={index}");

        public ILocator WithText(string text)
            => new Locator(_frame, $"{_selector} >> :scope:has-text({text.EscapeWithQuotes("\"")})");

        public ILocator WithText(Regex regex)
            => new Locator(_frame, $"{_selector} >> :scope:text-matches({regex.ToString().EscapeWithQuotes("\"")})");

        public Task PressAsync(string key, LocatorPressOptions options = null)
            => _frame.PressAsync(_selector, key, ConvertOptions<FramePressOptions>(options));

        public Task<byte[]> ScreenshotAsync(LocatorScreenshotOptions options = null)
            => WithElementAsync(async (h, o) => await h.ScreenshotAsync(ConvertOptions<ElementHandleScreenshotOptions>(o)).ConfigureAwait(false), options);

        public Task ScrollIntoViewIfNeededAsync(LocatorScrollIntoViewIfNeededOptions options = null)
            => WithElementAsync(async (h, o) => await h.ScrollIntoViewIfNeededAsync(ConvertOptions<ElementHandleScrollIntoViewIfNeededOptions>(o)).ConfigureAwait(false), options);

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
            => WithElementAsync((h, o) => h.SelectTextAsync(ConvertOptions<ElementHandleSelectTextOptions>(o)), options);

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

        ILocator ILocator.Locator(string selector)
            => new Locator(_frame, $"{_selector} >> {selector}");

        public Task WaitForAsync(LocatorWaitForOptions options = null)
            => _frame.LocatorWaitForAsync(_selector, ConvertOptions<LocatorWaitForOptions>(options));

        public override string ToString() => "Locator@" + _selector;

        private T ConvertOptions<T>(object incoming, Action<T> configure = null)
            where T : class, new()
        {
            var jsonValue = JsonSerializer.Serialize(incoming);
            T obj = JsonSerializer.Deserialize<T>(jsonValue) ?? new();
            typeof(T).GetProperty("Strict")?.SetValue(obj, true);
            configure?.Invoke(obj);
            return obj;
        }

        private async Task<TResult> WithElementAsync<TOptions, TResult>(Func<IElementHandle, TOptions, Task<TResult>> callback, TOptions options)
            where TOptions : class
            where TResult : class
        {
            IElementHandle handle = await ElementHandleAsync(ConvertOptions<LocatorElementHandleOptions>(options)).ConfigureAwait(false);
            try
            {
                return await callback(handle, options).ConfigureAwait(false);
            }
            finally
            {
                await (handle?.DisposeAsync()).ConfigureAwait(false);
            }
        }

        private async Task WithElementAsync<TOptions>(Func<IElementHandle, TOptions, Task> callback, TOptions options)
            where TOptions : class
        {
            IElementHandle handle = await ElementHandleAsync(ConvertOptions<LocatorElementHandleOptions>(options)).ConfigureAwait(false);
            try
            {
                await callback(handle, options).ConfigureAwait(false);
            }
            finally
            {
                await (handle?.DisposeAsync()).ConfigureAwait(false);
            }
        }
    }
}
