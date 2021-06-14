/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright.Core
{
    internal partial class Frame
    {
        public Task<IElementHandle> AddScriptTagAsync(FrameAddScriptTagOptions? options = default)
        {
            options ??= new FrameAddScriptTagOptions();
            return AddScriptTagAsync(url: options.Url, path: options.Path, content: options.Content, type: options.Type);
        }

        public Task<IElementHandle> AddStyleTagAsync(FrameAddStyleTagOptions? options = default)
        {
            options ??= new FrameAddStyleTagOptions();
            return AddStyleTagAsync(url: options.Url, path: options.Path, content: options.Content);
        }

        public Task CheckAsync(string selector, FrameCheckOptions? options = default)
        {
            options ??= new FrameCheckOptions();
            return CheckAsync(selector, position: options.Position, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task ClickAsync(string selector, FrameClickOptions? options = default)
        {
            options ??= new FrameClickOptions();
            return ClickAsync(selector, button: options.Button, clickCount: options.ClickCount, delay: options.Delay, position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task DblClickAsync(string selector, FrameDblClickOptions? options = default)
        {
            options ??= new FrameDblClickOptions();
            return DblClickAsync(selector, button: options.Button, delay: options.Delay, position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task DispatchEventAsync(string selector, string type, object? eventInit = default, FrameDispatchEventOptions? options = default)
        {
            options ??= new FrameDispatchEventOptions();
            return DispatchEventAsync(selector, type, eventInit, timeout: options.Timeout);
        }

        public Task FillAsync(string selector, string value, FrameFillOptions? options = default)
        {
            options ??= new FrameFillOptions();
            return FillAsync(selector, value, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task FocusAsync(string selector, FrameFocusOptions? options = default)
        {
            options ??= new FrameFocusOptions();
            return FocusAsync(selector, timeout: options.Timeout);
        }

        public Task<string?> GetAttributeAsync(string selector, string name, FrameGetAttributeOptions? options = default)
        {
            options ??= new FrameGetAttributeOptions();
            return GetAttributeAsync(selector, name, timeout: options.Timeout);
        }

        public Task<IResponse?> GotoAsync(string url, FrameGotoOptions? options = default)
        {
            options ??= new FrameGotoOptions();
            return GotoAsync(url, waitUntil: options.WaitUntil, timeout: options.Timeout, referer: options.Referer);
        }

        public Task HoverAsync(string selector, FrameHoverOptions? options = default)
        {
            options ??= new FrameHoverOptions();
            return HoverAsync(selector, position: options.Position, modifiers: options.Modifiers, force: options.Force, timeout: options.Timeout, trial: options.Trial);
        }

        public Task<string> InnerHTMLAsync(string selector, FrameInnerHTMLOptions? options = default)
        {
            options ??= new FrameInnerHTMLOptions();
            return InnerHTMLAsync(selector, timeout: options.Timeout);
        }

        public Task<string> InnerTextAsync(string selector, FrameInnerTextOptions? options = default)
        {
            options ??= new FrameInnerTextOptions();
            return InnerTextAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsCheckedAsync(string selector, FrameIsCheckedOptions? options = default)
        {
            options ??= new FrameIsCheckedOptions();
            return IsCheckedAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsDisabledAsync(string selector, FrameIsDisabledOptions? options = default)
        {
            options ??= new FrameIsDisabledOptions();
            return IsDisabledAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsEditableAsync(string selector, FrameIsEditableOptions? options = default)
        {
            options ??= new FrameIsEditableOptions();
            return IsEditableAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsEnabledAsync(string selector, FrameIsEnabledOptions? options = default)
        {
            options ??= new FrameIsEnabledOptions();
            return IsEnabledAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsHiddenAsync(string selector, FrameIsHiddenOptions? options = default)
        {
            options ??= new FrameIsHiddenOptions();
            return IsHiddenAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsVisibleAsync(string selector, FrameIsVisibleOptions? options = default)
        {
            options ??= new FrameIsVisibleOptions();
            return IsVisibleAsync(selector, timeout: options.Timeout);
        }

        public Task PressAsync(string selector, string key, FramePressOptions? options = default)
        {
            options ??= new FramePressOptions();
            return PressAsync(selector, key, delay: options.Delay, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string values, FrameSelectOptionOptions? options = default)
        {
            options ??= new FrameSelectOptionOptions();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle values, FrameSelectOptionOptions? options = default)
        {
            options ??= new FrameSelectOptionOptions();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, FrameSelectOptionOptions? options = default)
        {
            options ??= new FrameSelectOptionOptions();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, FrameSelectOptionOptions? options = default)
        {
            options ??= new FrameSelectOptionOptions();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, FrameSelectOptionOptions? options = default)
        {
            options ??= new FrameSelectOptionOptions();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, FrameSelectOptionOptions? options = default)
        {
            options ??= new FrameSelectOptionOptions();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetContentAsync(string html, FrameSetContentOptions? options = default)
        {
            options ??= new FrameSetContentOptions();
            return SetContentAsync(html, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task SetInputFilesAsync(string selector, string files, FrameSetInputFilesOptions? options = default)
        {
            options ??= new FrameSetInputFilesOptions();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, FrameSetInputFilesOptions? options = default)
        {
            options ??= new FrameSetInputFilesOptions();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string selector, FilePayload files, FrameSetInputFilesOptions? options = default)
        {
            options ??= new FrameSetInputFilesOptions();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, FrameSetInputFilesOptions? options = default)
        {
            options ??= new FrameSetInputFilesOptions();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task TapAsync(string selector, FrameTapOptions? options = default)
        {
            options ??= new FrameTapOptions();
            return TapAsync(selector, position: options.Position, modifiers: options.Modifiers, noWaitAfter: options.NoWaitAfter, force: options.Force, timeout: options.Timeout, trial: options.Trial);
        }

        public Task<string?> TextContentAsync(string selector, FrameTextContentOptions? options = default)
        {
            options ??= new FrameTextContentOptions();
            return TextContentAsync(selector, timeout: options.Timeout);
        }

        public Task TypeAsync(string selector, string text, FrameTypeOptions? options = default)
        {
            options ??= new FrameTypeOptions();
            return TypeAsync(selector, text, delay: options.Delay, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task UncheckAsync(string selector, FrameUncheckOptions? options = default)
        {
            options ??= new FrameUncheckOptions();
            return UncheckAsync(selector, position: options.Position, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task<IJSHandle> WaitForFunctionAsync(string expression, object? arg = default, FrameWaitForFunctionOptions? options = default)
        {
            options ??= new FrameWaitForFunctionOptions();
            return WaitForFunctionAsync(expression, arg, pollingInterval: options.PollingInterval, timeout: options.Timeout);
        }

        public Task WaitForLoadStateAsync(LoadState? state = default, FrameWaitForLoadStateOptions? options = default)
        {
            options ??= new FrameWaitForLoadStateOptions();
            return WaitForLoadStateAsync(state, timeout: options.Timeout);
        }

        public Task<IResponse?> WaitForNavigationAsync(FrameWaitForNavigationOptions? options = default)
        {
            options ??= new FrameWaitForNavigationOptions();
            return WaitForNavigationAsync(urlString: options.UrlString, urlRegex: options.UrlRegex, urlFunc: options.UrlFunc, waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<IResponse?> RunAndWaitForNavigationAsync(Func<Task> action, FrameRunAndWaitForNavigationOptions? options = default)
        {
            options ??= new FrameRunAndWaitForNavigationOptions();
            return RunAndWaitForNavigationAsync(action, urlString: options.UrlString, urlRegex: options.UrlRegex, urlFunc: options.UrlFunc, waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<IElementHandle?> WaitForSelectorAsync(string selector, FrameWaitForSelectorOptions? options = default)
        {
            options ??= new FrameWaitForSelectorOptions();
            return WaitForSelectorAsync(selector, state: options.State, timeout: options.Timeout);
        }

        public Task WaitForURLAsync(string url, FrameWaitForURLOptions? options = default)
        {
            options ??= new FrameWaitForURLOptions();
            return WaitForURLAsync(url, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task WaitForURLAsync(Regex url, FrameWaitForURLOptions? options = default)
        {
            options ??= new FrameWaitForURLOptions();
            return WaitForURLAsync(url, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task WaitForURLAsync(Func<string, bool> url, FrameWaitForURLOptions? options = default)
        {
            options ??= new FrameWaitForURLOptions();
            return WaitForURLAsync(url, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }
    }
}

#nullable disable
