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

namespace Microsoft.Playwright
{
    public class ElementHandle : JSHandle, IElementHandle, IChannelOwner<ElementHandle>
    {
        private readonly ElementHandleChannel _channel;

        internal ElementHandle(IChannelOwner parent, string guid, ElementHandleInitializer initializer) : base(parent, guid, initializer)
        {
            _channel = new ElementHandleChannel(guid, parent.Connection, this);
            _channel.PreviewUpdated += (_, e) => Preview = e.Preview;
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<ElementHandle> IChannelOwner<ElementHandle>.Channel => _channel;

        internal IChannel<ElementHandle> ElementChannel => _channel;

        public async Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState state, float? timeout)
            => (await _channel.WaitForSelectorAsync(
                selector: selector,
                state: state.EnsureDefaultValue(WaitForSelectorState.Visible),
                timeout: timeout).ConfigureAwait(false))?.Object;

        public Task WaitForElementStateAsync(ElementState state, float? timeout)
            => _channel.WaitForElementStateAsync(state, timeout);

        public Task PressAsync(string key, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.PressAsync(key, delay ?? 0, timeout, noWaitAfter);

        public Task TypeAsync(string text, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.TypeAsync(text, delay ?? 0, timeout, noWaitAfter);

        public async Task<byte[]> ScreenshotAsync(
            string path,
            ScreenshotType type,
            int? quality,
            bool? omitBackground,
            float? timeout)
        {
            type = !string.IsNullOrEmpty(path) ? DetermineScreenshotType(path) : type.EnsureDefaultValue(ScreenshotType.Png);
            byte[] result = Convert.FromBase64String(await _channel.ScreenshotAsync(path, omitBackground ?? false, type, quality, timeout).ConfigureAwait(false));

            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
                File.WriteAllBytes(path, result);
            }

            return result;
        }

        public Task FillAsync(string value, bool? noWaitAfter, float? timeout) => _channel.FillAsync(value, noWaitAfter, timeout);

        public async Task<IFrame> ContentFrameAsync() => (await _channel.ContentFrameAsync().ConfigureAwait(false))?.Object;

        public Task HoverAsync(
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            float? timeout,
            bool? trial)
            => _channel.HoverAsync(modifiers, position, timeout, force ?? false, trial);

        public Task ScrollIntoViewIfNeededAsync(float? timeout) => _channel.ScrollIntoViewIfNeededAsync(timeout);

        public async Task<IFrame> OwnerFrameAsync() => (await _channel.OwnerFrameAsync().ConfigureAwait(false)).Object;

        public Task<ElementHandleBoundingBoxResult> BoundingBoxAsync() => _channel.BoundingBoxAsync();

        public Task ClickAsync(
            MouseButton button,
            int? clickCount,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => _channel.ClickAsync(delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1, modifiers, position, timeout, force ?? false, noWaitAfter, trial);

        public Task DblClickAsync(
            MouseButton button,
            float? delay,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout,
            bool? trial)
            => _channel.DblClickAsync(delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), modifiers, position, timeout, force ?? false, noWaitAfter, trial);

        public Task SetInputFilesAsync(string files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(new[] { files }, noWaitAfter, timeout);

        public Task SetInputFilesAsync(IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => _channel.SetInputFilesAsync(files.Select(f => f.ToFilePayload()).ToArray(), noWaitAfter, timeout);

        public Task SetInputFilesAsync(FilePayload files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(new[] { files }, noWaitAfter, timeout);

        public Task SetInputFilesAsync(IEnumerable<FilePayload> files, bool? noWaitAfter, float? timeout)
            => _channel.SetInputFilesAsync(files, noWaitAfter, timeout);

        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        public async Task<IReadOnlyCollection<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(e => ((ElementHandleChannel)e).Object).ToList().AsReadOnly();

        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        public Task FocusAsync() => _channel.FocusAsync();

        public Task DispatchEventAsync(string type, object eventInit)
            => _channel.DispatchEventAsync(
                type,
                eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit));

        public Task<string> GetAttributeAsync(string name) => _channel.GetAttributeAsync(name);

        public Task<string> InnerHTMLAsync() => _channel.InnerHTMLAsync();

        public Task<string> InnerTextAsync() => _channel.InnerTextAsync();

        public Task<string> TextContentAsync() => _channel.TextContentAsync();

        public Task SelectTextAsync(float? timeout) => _channel.SelectTextAsync(timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(new[] { values }, noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, bool? noWaitAfter, float? timeout)
            => _channel.SelectOptionAsync(values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(new[] { values }, noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout)
            => _channel.SelectOptionAsync(values.Cast<ElementHandle>(), noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(SelectOptionValue values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(new[] { values }, noWaitAfter, timeout);

        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<SelectOptionValue> values, bool? noWaitAfter, float? timeout)
            => _channel.SelectOptionAsync(values, noWaitAfter, timeout);

        public Task CheckAsync(Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.CheckAsync(position, timeout, force ?? false, noWaitAfter, trial);

        public Task UncheckAsync(Position position, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.UncheckAsync(position, timeout, force, noWaitAfter, trial);

        public Task TapAsync(Position position, IEnumerable<KeyboardModifier> modifiers, bool? force, bool? noWaitAfter, float? timeout, bool? trial)
            => _channel.TapAsync(position, modifiers, timeout, force ?? false, noWaitAfter, trial);

        public Task<bool> IsCheckedAsync() => _channel.IsCheckedAsync();

        public Task<bool> IsDisabledAsync() => _channel.IsDisabledAsync();

        public Task<bool> IsEditableAsync() => _channel.IsEditableAsync();

        public Task<bool> IsEnabledAsync() => _channel.IsEnabledAsync();

        public Task<bool> IsHiddenAsync() => _channel.IsHiddenAsync();

        public Task<bool> IsVisibleAsync() => _channel.IsVisibleAsync();

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
}
