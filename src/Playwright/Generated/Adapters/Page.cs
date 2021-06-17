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
    internal partial class Page
    {
        public Task<IElementHandle> AddScriptTagAsync(PageAddScriptTagOptions? options = default)
        {
            options ??= new();
            return AddScriptTagAsync(url: options.Url, path: options.Path, content: options.Content, type: options.Type);
        }

        public Task<IElementHandle> AddStyleTagAsync(PageAddStyleTagOptions? options = default)
        {
            options ??= new();
            return AddStyleTagAsync(url: options.Url, path: options.Path, content: options.Content);
        }

        public Task CheckAsync(string selector, PageCheckOptions? options = default)
        {
            options ??= new();
            return CheckAsync(selector, position: options.Position, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task ClickAsync(string selector, PageClickOptions? options = default)
        {
            options ??= new();
            return ClickAsync(selector, button: options.Button, clickCount: options.ClickCount, delay: options.Delay, position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task CloseAsync(PageCloseOptions? options = default)
        {
            options ??= new();
            return CloseAsync(runBeforeUnload: options.RunBeforeUnload);
        }

        public Task DblClickAsync(string selector, PageDblClickOptions? options = default)
        {
            options ??= new();
            return DblClickAsync(selector, button: options.Button, delay: options.Delay, position: options.Position, modifiers: options.Modifiers, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task DispatchEventAsync(string selector, string type, object? eventInit = default, PageDispatchEventOptions? options = default)
        {
            options ??= new();
            return DispatchEventAsync(selector, type, eventInit, timeout: options.Timeout);
        }

        public Task EmulateMediaAsync(PageEmulateMediaOptions? options = default)
        {
            options ??= new();
            return EmulateMediaAsync(media: options.Media, colorScheme: options.ColorScheme, reducedMotion: options.ReducedMotion);
        }

        public Task ExposeBindingAsync(string name, Action callback, PageExposeBindingOptions? options = default)
        {
            options ??= new();
            return ExposeBindingAsync(name, callback, handle: options.Handle);
        }

        public Task FillAsync(string selector, string value, PageFillOptions? options = default)
        {
            options ??= new();
            return FillAsync(selector, value, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task FocusAsync(string selector, PageFocusOptions? options = default)
        {
            options ??= new();
            return FocusAsync(selector, timeout: options.Timeout);
        }

        public Task<string?> GetAttributeAsync(string selector, string name, PageGetAttributeOptions? options = default)
        {
            options ??= new();
            return GetAttributeAsync(selector, name, timeout: options.Timeout);
        }

        public Task<IResponse?> GoBackAsync(PageGoBackOptions? options = default)
        {
            options ??= new();
            return GoBackAsync(waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<IResponse?> GoForwardAsync(PageGoForwardOptions? options = default)
        {
            options ??= new();
            return GoForwardAsync(waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<IResponse?> GotoAsync(string url, PageGotoOptions? options = default)
        {
            options ??= new();
            return GotoAsync(url, waitUntil: options.WaitUntil, timeout: options.Timeout, referer: options.Referer);
        }

        public Task HoverAsync(string selector, PageHoverOptions? options = default)
        {
            options ??= new();
            return HoverAsync(selector, position: options.Position, modifiers: options.Modifiers, force: options.Force, timeout: options.Timeout, trial: options.Trial);
        }

        public Task<string> InnerHTMLAsync(string selector, PageInnerHTMLOptions? options = default)
        {
            options ??= new();
            return InnerHTMLAsync(selector, timeout: options.Timeout);
        }

        public Task<string> InnerTextAsync(string selector, PageInnerTextOptions? options = default)
        {
            options ??= new();
            return InnerTextAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsCheckedAsync(string selector, PageIsCheckedOptions? options = default)
        {
            options ??= new();
            return IsCheckedAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsDisabledAsync(string selector, PageIsDisabledOptions? options = default)
        {
            options ??= new();
            return IsDisabledAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsEditableAsync(string selector, PageIsEditableOptions? options = default)
        {
            options ??= new();
            return IsEditableAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsEnabledAsync(string selector, PageIsEnabledOptions? options = default)
        {
            options ??= new();
            return IsEnabledAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsHiddenAsync(string selector, PageIsHiddenOptions? options = default)
        {
            options ??= new();
            return IsHiddenAsync(selector, timeout: options.Timeout);
        }

        public Task<bool> IsVisibleAsync(string selector, PageIsVisibleOptions? options = default)
        {
            options ??= new();
            return IsVisibleAsync(selector, timeout: options.Timeout);
        }

        public Task<byte[]> PdfAsync(PagePdfOptions? options = default)
        {
            options ??= new();
            return PdfAsync(path: options.Path, scale: options.Scale, displayHeaderFooter: options.DisplayHeaderFooter, headerTemplate: options.HeaderTemplate, footerTemplate: options.FooterTemplate, printBackground: options.PrintBackground, landscape: options.Landscape, pageRanges: options.PageRanges, format: options.Format, width: options.Width, height: options.Height, margin: options.Margin, preferCSSPageSize: options.PreferCSSPageSize);
        }

        public Task PressAsync(string selector, string key, PagePressOptions? options = default)
        {
            options ??= new();
            return PressAsync(selector, key, delay: options.Delay, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IResponse?> ReloadAsync(PageReloadOptions? options = default)
        {
            options ??= new();
            return ReloadAsync(waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<byte[]> ScreenshotAsync(PageScreenshotOptions? options = default)
        {
            options ??= new();
            return ScreenshotAsync(path: options.Path, type: options.Type, quality: options.Quality, fullPage: options.FullPage, clip: options.Clip, omitBackground: options.OmitBackground, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, string values, PageSelectOptionOptions? options = default)
        {
            options ??= new();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IElementHandle values, PageSelectOptionOptions? options = default)
        {
            options ??= new();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<string> values, PageSelectOptionOptions? options = default)
        {
            options ??= new();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, SelectOptionValue values, PageSelectOptionOptions? options = default)
        {
            options ??= new();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<IElementHandle> values, PageSelectOptionOptions? options = default)
        {
            options ??= new();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task<IReadOnlyList<string>> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, PageSelectOptionOptions? options = default)
        {
            options ??= new();
            return SelectOptionAsync(selector, values, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetContentAsync(string html, PageSetContentOptions? options = default)
        {
            options ??= new();
            return SetContentAsync(html, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task SetInputFilesAsync(string selector, string files, PageSetInputFilesOptions? options = default)
        {
            options ??= new();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string selector, IEnumerable<string> files, PageSetInputFilesOptions? options = default)
        {
            options ??= new();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string selector, FilePayload files, PageSetInputFilesOptions? options = default)
        {
            options ??= new();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, PageSetInputFilesOptions? options = default)
        {
            options ??= new();
            return SetInputFilesAsync(selector, files, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task TapAsync(string selector, PageTapOptions? options = default)
        {
            options ??= new();
            return TapAsync(selector, position: options.Position, modifiers: options.Modifiers, noWaitAfter: options.NoWaitAfter, force: options.Force, timeout: options.Timeout, trial: options.Trial);
        }

        public Task<string?> TextContentAsync(string selector, PageTextContentOptions? options = default)
        {
            options ??= new();
            return TextContentAsync(selector, timeout: options.Timeout);
        }

        public Task TypeAsync(string selector, string text, PageTypeOptions? options = default)
        {
            options ??= new();
            return TypeAsync(selector, text, delay: options.Delay, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout);
        }

        public Task UncheckAsync(string selector, PageUncheckOptions? options = default)
        {
            options ??= new();
            return UncheckAsync(selector, position: options.Position, force: options.Force, noWaitAfter: options.NoWaitAfter, timeout: options.Timeout, trial: options.Trial);
        }

        public Task<IConsoleMessage> WaitForConsoleMessageAsync(PageWaitForConsoleMessageOptions? options = default)
        {
            options ??= new();
            return WaitForConsoleMessageAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IConsoleMessage> RunAndWaitForConsoleMessageAsync(Func<Task> action, PageRunAndWaitForConsoleMessageOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForConsoleMessageAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IDownload> WaitForDownloadAsync(PageWaitForDownloadOptions? options = default)
        {
            options ??= new();
            return WaitForDownloadAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IDownload> RunAndWaitForDownloadAsync(Func<Task> action, PageRunAndWaitForDownloadOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForDownloadAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IFileChooser> WaitForFileChooserAsync(PageWaitForFileChooserOptions? options = default)
        {
            options ??= new();
            return WaitForFileChooserAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IFileChooser> RunAndWaitForFileChooserAsync(Func<Task> action, PageRunAndWaitForFileChooserOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForFileChooserAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IJSHandle> WaitForFunctionAsync(string expression, object? arg = default, PageWaitForFunctionOptions? options = default)
        {
            options ??= new();
            return WaitForFunctionAsync(expression, arg, pollingInterval: options.PollingInterval, timeout: options.Timeout);
        }

        public Task WaitForLoadStateAsync(LoadState? state = default, PageWaitForLoadStateOptions? options = default)
        {
            options ??= new();
            return WaitForLoadStateAsync(state, timeout: options.Timeout);
        }

        public Task<IResponse?> WaitForNavigationAsync(PageWaitForNavigationOptions? options = default)
        {
            options ??= new();
            return WaitForNavigationAsync(urlString: options.UrlString, urlRegex: options.UrlRegex, urlFunc: options.UrlFunc, waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<IResponse?> RunAndWaitForNavigationAsync(Func<Task> action, PageRunAndWaitForNavigationOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForNavigationAsync(action, urlString: options.UrlString, urlRegex: options.UrlRegex, urlFunc: options.UrlFunc, waitUntil: options.WaitUntil, timeout: options.Timeout);
        }

        public Task<IPage> WaitForPopupAsync(PageWaitForPopupOptions? options = default)
        {
            options ??= new();
            return WaitForPopupAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IPage> RunAndWaitForPopupAsync(Func<Task> action, PageRunAndWaitForPopupOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForPopupAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IRequest> WaitForRequestAsync(string urlOrPredicate, PageWaitForRequestOptions? options = default)
        {
            options ??= new();
            return WaitForRequestAsync(urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IRequest> WaitForRequestAsync(Regex urlOrPredicate, PageWaitForRequestOptions? options = default)
        {
            options ??= new();
            return WaitForRequestAsync(urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IRequest> WaitForRequestAsync(Func<IRequest, bool> urlOrPredicate, PageWaitForRequestOptions? options = default)
        {
            options ??= new();
            return WaitForRequestAsync(urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, string urlOrPredicate, PageRunAndWaitForRequestOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForRequestAsync(action, urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Regex urlOrPredicate, PageRunAndWaitForRequestOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForRequestAsync(action, urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IRequest> RunAndWaitForRequestAsync(Func<Task> action, Func<IRequest, bool> urlOrPredicate, PageRunAndWaitForRequestOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForRequestAsync(action, urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IRequest> WaitForRequestFinishedAsync(PageWaitForRequestFinishedOptions? options = default)
        {
            options ??= new();
            return WaitForRequestFinishedAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IRequest> RunAndWaitForRequestFinishedAsync(Func<Task> action, PageRunAndWaitForRequestFinishedOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForRequestFinishedAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IResponse> WaitForResponseAsync(string urlOrPredicate, PageWaitForResponseOptions? options = default)
        {
            options ??= new();
            return WaitForResponseAsync(urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IResponse> WaitForResponseAsync(Regex urlOrPredicate, PageWaitForResponseOptions? options = default)
        {
            options ??= new();
            return WaitForResponseAsync(urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IResponse> WaitForResponseAsync(Func<IResponse, bool> urlOrPredicate, PageWaitForResponseOptions? options = default)
        {
            options ??= new();
            return WaitForResponseAsync(urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, string urlOrPredicate, PageRunAndWaitForResponseOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForResponseAsync(action, urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Regex urlOrPredicate, PageRunAndWaitForResponseOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForResponseAsync(action, urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IResponse> RunAndWaitForResponseAsync(Func<Task> action, Func<IResponse, bool> urlOrPredicate, PageRunAndWaitForResponseOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForResponseAsync(action, urlOrPredicate, timeout: options.Timeout);
        }

        public Task<IElementHandle?> WaitForSelectorAsync(string selector, PageWaitForSelectorOptions? options = default)
        {
            options ??= new();
            return WaitForSelectorAsync(selector, state: options.State, timeout: options.Timeout);
        }

        public Task WaitForURLAsync(string url, PageWaitForURLOptions? options = default)
        {
            options ??= new();
            return WaitForURLAsync(url, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task WaitForURLAsync(Regex url, PageWaitForURLOptions? options = default)
        {
            options ??= new();
            return WaitForURLAsync(url, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task WaitForURLAsync(Func<string, bool> url, PageWaitForURLOptions? options = default)
        {
            options ??= new();
            return WaitForURLAsync(url, timeout: options.Timeout, waitUntil: options.WaitUntil);
        }

        public Task<IWebSocket> WaitForWebSocketAsync(PageWaitForWebSocketOptions? options = default)
        {
            options ??= new();
            return WaitForWebSocketAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IWebSocket> RunAndWaitForWebSocketAsync(Func<Task> action, PageRunAndWaitForWebSocketOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForWebSocketAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IWorker> WaitForWorkerAsync(PageWaitForWorkerOptions? options = default)
        {
            options ??= new();
            return WaitForWorkerAsync(predicate: options.Predicate, timeout: options.Timeout);
        }

        public Task<IWorker> RunAndWaitForWorkerAsync(Func<Task> action, PageRunAndWaitForWorkerOptions? options = default)
        {
            options ??= new();
            return RunAndWaitForWorkerAsync(action, predicate: options.Predicate, timeout: options.Timeout);
        }
    }
}

#nullable disable
