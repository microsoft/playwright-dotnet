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
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Locator : ILocator
{
    internal readonly Frame _frame;
    internal readonly string _selector;
    private static string _testIdAttributeName = "data-testid";

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
            var textSelector = "text=" + EscapeForTextSelector(options.HasTextRegex, false);
            _selector += $" >> internal:has={JsonSerializer.Serialize(textSelector, serializerOptions)}";
        }
        else if (options?.HasTextString != null)
        {
            var textSelector = "text=" + EscapeForTextSelector(options.HasTextString, false);
            _selector += $" >> internal:has={JsonSerializer.Serialize(textSelector, serializerOptions)}";
        }

        if (options?.Has != null)
        {
            var locator = (Locator)options.Has;
            if (locator._frame != _frame)
            {
                throw new ArgumentException("Inner \"Has\" locator must belong to the same frame.");
            }
            _selector += " >> internal:has=" + JsonSerializer.Serialize(locator._selector, serializerOptions);
        }
    }

    public ILocator First => new Locator(_frame, $"{_selector} >> nth=0");

    public ILocator Last => new Locator(_frame, $"{_selector} >> nth=-1");

    IPage ILocator.Page => _frame.Page;

    public Task<LocatorBoundingBoxResult> BoundingBoxAsync(LocatorBoundingBoxOptions options = null)
        => WithElementAsync(
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

    public ILocator GetByAltText(string text, LocatorGetByAltTextOptions options = null)
        => ((ILocator)this).Locator(GetByAltTextSelector(text, options?.Exact));

    public ILocator GetByAltText(Regex text, LocatorGetByAltTextOptions options = null)
        => ((ILocator)this).Locator(GetByAltTextSelector(text, options?.Exact));

    public ILocator GetByLabel(string text, LocatorGetByLabelOptions options = null)
        => ((ILocator)this).Locator(GetByLabelSelector(text, options?.Exact));

    public ILocator GetByLabel(Regex text, LocatorGetByLabelOptions options = null)
        => ((ILocator)this).Locator(GetByLabelSelector(text, options?.Exact));

    public ILocator GetByPlaceholder(string text, LocatorGetByPlaceholderOptions options = null)
        => ((ILocator)this).Locator(GetByPlaceholderSelector(text, options?.Exact));

    public ILocator GetByPlaceholder(Regex text, LocatorGetByPlaceholderOptions options = null)
        => ((ILocator)this).Locator(GetByPlaceholderSelector(text, options?.Exact));

    public ILocator GetByRole(AriaRole role, LocatorGetByRoleOptions options = null)
        => ((ILocator)this).Locator(GetByRoleSelector(role, new(options)));

    public ILocator GetByTestId(string testId)
        => ((ILocator)this).Locator(GetByTestIdSelector(testId));

    public ILocator GetByText(string text, LocatorGetByTextOptions options = null)
        => ((ILocator)this).Locator(GetByTextSelector(text, options?.Exact));

    public ILocator GetByText(Regex text, LocatorGetByTextOptions options = null)
        => ((ILocator)this).Locator(GetByTextSelector(text, options?.Exact));

    public ILocator GetByTitle(string text, LocatorGetByTitleOptions options = null)
        => ((ILocator)this).Locator(GetByTitleSelector(text, options?.Exact));

    public ILocator GetByTitle(Regex text, LocatorGetByTitleOptions options = null)
        => ((ILocator)this).Locator(GetByTitleSelector(text, options?.Exact));

    internal static void SetTestIdAttribute(string attributeName)
        => _testIdAttributeName = attributeName;

    internal static string GetByTestIdSelector(string testId)
        => GetByAttributeTextSelector(_testIdAttributeName, testId, exact: true);

    internal static string GetByAttributeTextSelector(string attrName, string text, bool? exact)
        => $"internal:attr=[{attrName}={EscapeForAttributeSelector(text, exact ?? false)}]";

    internal static string GetByAttributeTextSelector(string attrName, Regex text, bool? exact)
        => $"internal:attr=[{attrName}={EscapeForTextSelector(text, exact)}]";

    internal static string GetByLabelSelector(string text, bool? exact)
        => "internal:label=" + EscapeForTextSelector(text, exact);

    internal static string GetByLabelSelector(Regex text, bool? exact)
        => "internal:label=" + EscapeForTextSelector(text, exact);

    internal static string GetByAltTextSelector(string text, bool? exact)
        => GetByAttributeTextSelector("alt", text, exact);

    internal static string GetByAltTextSelector(Regex text, bool? exact)
        => GetByAttributeTextSelector("alt", text, exact);

    internal static string GetByTitleSelector(string text, bool? exact)
        => GetByAttributeTextSelector("title", text, exact);

    internal static string GetByTitleSelector(Regex text, bool? exact)
        => GetByAttributeTextSelector("title", text, exact);

    internal static string GetByPlaceholderSelector(string text, bool? exact)
        => GetByAttributeTextSelector("placeholder", text, exact);

    internal static string GetByPlaceholderSelector(Regex text, bool? exact)
        => GetByAttributeTextSelector("placeholder", text, exact);

    internal static string GetByTextSelector(string text, bool? exact)
        => $"text={EscapeForTextSelector(text, exact)}";

    internal static string GetByTextSelector(Regex text, bool? exact)
        => $"text={EscapeForTextSelector(text, exact)}";

    internal static string GetByRoleSelector(AriaRole role, ByRoleOptions options)
    {
        List<List<string>> props = new();
        if (options.Checked != null)
        {
            props.Add(new List<string> { "checked", options.Checked.ToJson() });
        }
        if (options.Disabled != null)
        {
            props.Add(new List<string> { "disabled", options.Disabled.ToJson() });
        }
        if (options.Selected != null)
        {
            props.Add(new List<string> { "selected", options.Selected.ToJson() });
        }
        if (options.Expanded != null)
        {
            props.Add(new List<string> { "expanded", options.Expanded.ToJson() });
        }
        if (options.IncludeHidden != null)
        {
            props.Add(new List<string> { "include-hidden", options.IncludeHidden.ToJson() });
        }
        if (options.Level != null)
        {
            props.Add(new List<string> { "level", options.Level.ToString() });
        }
        if (options.NameString != null)
        {
            props.Add(new List<string> { "name", EscapeForAttributeSelector(options.NameString, false) });
        }
        else if (options.NameRegex != null)
        {
            props.Add(new List<string> { "name", $"/{options.NameRegex}/{options.NameRegex.Options.GetInlineFlags()}" });
        }
        if (options.Pressed != null)
        {
            props.Add(new List<string> { "pressed", options.Pressed.ToJson() });
        }
        return $"role={role.ToValueString()}{string.Concat(props.Select(p => $"[{p[0]}={p[1]}]"))}";
    }

    private static string EscapeForAttributeSelector(string value, bool exact)
    {
        // TODO: this should actually be
        //   cssEscape(value).replace(/\\ /g, ' ')
        // However, our attribute selectors do not conform to CSS parsing spec,
        // so we escape them differently.
        var exactFlag = (exact == true) ? string.Empty : "i";
        return $"\"{value.Replace("\"", "\\\"")}\"{exactFlag}";
    }

    private static string EscapeForTextSelector(Regex text, bool? exact)
    {
        return $"/{text}/{text.Options.GetInlineFlags()}";
    }

    private static string EscapeForTextSelector(string text, bool? exact)
    {
        if (exact == true)
        {
            return $"\"{text.Replace("\"", "\\\"")}\"";
        }
        if (text.Contains("\"") || text.Contains(">>") || text[0] == '/')
        {
            return $"/{new Regex(@"\s+").Replace(EscapeForRegex(text), "\\s+")}/i";
        }
        return text;
    }

    private static string EscapeForRegex(string text)
    {
        var patern = new Regex(@"[.*+?^>${}()|[\]\\]");
        return patern.Replace(text, "\\$&");
    }
}

internal class ByRoleOptions
{
    public ByRoleOptions(FrameGetByRoleOptions clone)
    {
        if (clone == null)
        {
            return;
        }
        Checked = clone.Checked;
        Disabled = clone.Disabled;
        Expanded = clone.Expanded;
        IncludeHidden = clone.IncludeHidden;
        Level = clone.Level;
        NameString = clone.NameString;
        NameRegex = clone.NameRegex;
        Pressed = clone.Pressed;
        Selected = clone.Selected;
    }

    public ByRoleOptions(FrameLocatorGetByRoleOptions clone)
    {
        if (clone == null)
        {
            return;
        }
        Checked = clone.Checked;
        Disabled = clone.Disabled;
        Expanded = clone.Expanded;
        IncludeHidden = clone.IncludeHidden;
        Level = clone.Level;
        NameString = clone.NameString;
        NameRegex = clone.NameRegex;
        Pressed = clone.Pressed;
        Selected = clone.Selected;
    }

    public ByRoleOptions(PageGetByRoleOptions clone)
    {
        if (clone == null)
        {
            return;
        }
        Checked = clone.Checked;
        Disabled = clone.Disabled;
        Expanded = clone.Expanded;
        IncludeHidden = clone.IncludeHidden;
        Level = clone.Level;
        NameString = clone.NameString;
        NameRegex = clone.NameRegex;
        Pressed = clone.Pressed;
        Selected = clone.Selected;
    }

    public ByRoleOptions(LocatorGetByRoleOptions clone)
    {
        if (clone == null)
        {
            return;
        }
        Checked = clone.Checked;
        Disabled = clone.Disabled;
        Expanded = clone.Expanded;
        IncludeHidden = clone.IncludeHidden;
        Level = clone.Level;
        NameString = clone.NameString;
        NameRegex = clone.NameRegex;
        Pressed = clone.Pressed;
        Selected = clone.Selected;
    }

    public bool? Checked { get; set; }

    public bool? Disabled { get; set; }

    public bool? Expanded { get; set; }

    public bool? IncludeHidden { get; set; }

    public int? Level { get; set; }

    public string NameString { get; set; }

    public Regex NameRegex { get; set; }

    public bool? Pressed { get; set; }

    public bool? Selected { get; set; }
}
