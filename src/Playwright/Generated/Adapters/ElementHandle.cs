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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    internal partial class ElementHandle
    {
        public Task CheckAsync(ElementHandleCheckOptions options = default)
        {
            options ??= new ElementHandleCheckOptions();
            return CheckAsync(position: options.Position, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task ClickAsync(ElementHandleClickOptions options = default)
        {
            options ??= new ElementHandleClickOptions();
            return ClickAsync(button: options.Button, clickCount: options.ClickCount, delay: options.Delay, position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task DblClickAsync(ElementHandleDblClickOptions options = default)
        {
            options ??= new ElementHandleDblClickOptions();
            return DblClickAsync(button: options.Button, delay: options.Delay, position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task FillAsync(string value, ElementHandleFillOptions options = default)
        {
            options ??= new ElementHandleFillOptions();
            return FillAsync(value, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task HoverAsync(ElementHandleHoverOptions options = default)
        {
            options ??= new ElementHandleHoverOptions();
            return HoverAsync(position: options.Position, modifiers: options.Modifiers, force: options.Force, timeout: options.Timeout, trial: options.Trial);
        }

        public Task PressAsync(string key, ElementHandlePressOptions options = default)
        {
            options ??= new ElementHandlePressOptions();
            return PressAsync(key, delay: options.Delay, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<byte[]> ScreenshotAsync(ElementHandleScreenshotOptions options = default)
        {
            options ??= new ElementHandleScreenshotOptions();
            return ScreenshotAsync(path: options.Path, type: options.Type, quality: options.Quality, omitBackground: options.OmitBackground, timeout: options.Timeout);
        }

        public Task ScrollIntoViewIfNeededAsync(ElementHandleScrollIntoViewIfNeededOptions options = default)
        {
            options ??= new ElementHandleScrollIntoViewIfNeededOptions();
            return ScrollIntoViewIfNeededAsync(timeout: options.Timeout);
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, ElementHandleSelectOptionOptions options = default)
        {
            options ??= new ElementHandleSelectOptionOptions();
            return SelectOptionAsync(values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, ElementHandleSelectOptionOptions options = default)
        {
            options ??= new ElementHandleSelectOptionOptions();
            return SelectOptionAsync(values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, ElementHandleSelectOptionOptions options = default)
        {
            options ??= new ElementHandleSelectOptionOptions();
            return SelectOptionAsync(values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(SelectOptionValue values, ElementHandleSelectOptionOptions options = default)
        {
            options ??= new ElementHandleSelectOptionOptions();
            return SelectOptionAsync(values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, ElementHandleSelectOptionOptions options = default)
        {
            options ??= new ElementHandleSelectOptionOptions();
            return SelectOptionAsync(values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, ElementHandleSelectOptionOptions options = default)
        {
            options ??= new ElementHandleSelectOptionOptions();
            return SelectOptionAsync(values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SelectTextAsync(ElementHandleSelectTextOptions options = default)
        {
            options ??= new ElementHandleSelectTextOptions();
            return SelectTextAsync(timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string files, ElementHandleSetInputFilesOptions options = default)
        {
            options ??= new ElementHandleSetInputFilesOptions();
            return SetInputFilesAsync(files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(IEnumerable<string> files, ElementHandleSetInputFilesOptions options = default)
        {
            options ??= new ElementHandleSetInputFilesOptions();
            return SetInputFilesAsync(files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(FilePayload files, ElementHandleSetInputFilesOptions options = default)
        {
            options ??= new ElementHandleSetInputFilesOptions();
            return SetInputFilesAsync(files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(IEnumerable<FilePayload> files, ElementHandleSetInputFilesOptions options = default)
        {
            options ??= new ElementHandleSetInputFilesOptions();
            return SetInputFilesAsync(files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task TapAsync(ElementHandleTapOptions options = default)
        {
            options ??= new ElementHandleTapOptions();
            return TapAsync(position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task TypeAsync(string text, ElementHandleTypeOptions options = default)
        {
            options ??= new ElementHandleTypeOptions();
            return TypeAsync(text, delay: options.Delay, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task UncheckAsync(ElementHandleUncheckOptions options = default)
        {
            options ??= new ElementHandleUncheckOptions();
            return UncheckAsync(position: options.Position, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task WaitForElementStateAsync(ElementState state, ElementHandleWaitForElementStateOptions options = default)
        {
            options ??= new ElementHandleWaitForElementStateOptions();
            return WaitForElementStateAsync(state, timeout: options.Timeout);
        }

        public Task<IElementHandle> WaitForSelectorAsync(string selector, ElementHandleWaitForSelectorOptions options = default)
        {
            options ??= new ElementHandleWaitForSelectorOptions();
            return WaitForSelectorAsync(selector, state: options.State, timeout: options.Timeout);
        }
    }
}
