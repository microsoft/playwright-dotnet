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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IElementHandle" />
    public class ElementHandle : JSHandle, IElementHandle, IChannelOwner<ElementHandle>
    {
        private readonly ElementHandleChannel _channel;

        internal ElementHandle(IChannelOwner parent, string guid, ElementHandleInitializer initializer) : base(parent, guid, initializer)
        {
            _channel = new ElementHandleChannel(guid, parent.Connection, this);
            _channel.PreviewUpdated += (_, e) => Preview = e.Preview;
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<ElementHandle> IChannelOwner<ElementHandle>.Channel => _channel;

        internal IChannel<ElementHandle> ElementChannel => _channel;

        /// <inheritdoc />
        public async Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForSelectorState state, float? timeout)
            => (await _channel.WaitForSelectorAsync(
                selector: selector,
                state: state.EnsureDefaultValue(WaitForSelectorState.Visible),
                timeout: timeout).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task WaitForElementStateAsync(ElementState state, float? timeout)
            => _channel.WaitForElementStateAsync(state, timeout);

        /// <inheritdoc />
        public Task PressAsync(string key, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.PressAsync(key, delay ?? 0, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task TypeAsync(string text, float? delay, bool? noWaitAfter, float? timeout)
            => _channel.TypeAsync(text, delay ?? 0, timeout, noWaitAfter);

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Task FillAsync(string value, bool? noWaitAfter, float? timeout) => _channel.FillAsync(value, noWaitAfter, timeout);

        /// <inheritdoc />
        public async Task<IFrame> GetContentFrameAsync() => (await _channel.GetContentFrameAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task HoverAsync(
            ElementHandlePosition position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            float? timeout)
            => _channel.HoverAsync(modifiers, position, timeout, force ?? false);

        /// <inheritdoc />
        public Task ScrollIntoViewIfNeededAsync(float? timeout) => _channel.ScrollIntoViewIfNeededAsync(timeout);

        /// <inheritdoc />
        public async Task<IFrame> GetOwnerFrameAsync() => (await _channel.GetOwnerFrameAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task<ElementHandleBoundingBoxResult> GetBoundingBoxAsync() => _channel.GetBoundingBoxAsync();

        /// <inheritdoc />
        public Task ClickAsync(
            MouseButton button,
            int? clickCount,
            float? delay,
            ElementHandlePosition position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout)
            => _channel.ClickAsync(delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), clickCount ?? 1, modifiers, position, timeout, force ?? false, noWaitAfter);

        /// <inheritdoc />
        public Task DblclickAsync(
            MouseButton button,
            float? delay,
            ElementHandlePosition position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            bool? noWaitAfter,
            float? timeout)
            => _channel.DblclickAsync(delay ?? 0, button.EnsureDefaultValue(MouseButton.Left), modifiers, position, timeout, force ?? false, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(IEnumerable<string> files, bool? noWaitAfter, float? timeout)
            => _channel.SetInputFilesAsync(files.Select(f => f.ToElementHandleFile()).ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(ElementHandleFiles files, bool? noWaitAfter, float? timeout)
            => SetInputFilesAsync(new[] { files }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task SetInputFilesAsync(IEnumerable<ElementHandleFiles> files, bool? noWaitAfter, float? timeout)
            => _channel.SetInputFilesAsync(files, noWaitAfter, timeout);

        /// <inheritdoc />
        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(e => ((ElementHandleChannel)e).Object).ToList().AsReadOnly();

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: arg.ToEvaluateArgument()).ConfigureAwait(false));

        /// <inheritdoc />
        public Task FocusAsync() => _channel.FocusAsync();

        /// <inheritdoc />
        public Task DispatchEventAsync(string type, object eventInit)
            => _channel.DispatchEventAsync(
                type,
                eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit));

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string name) => _channel.GetAttributeAsync(name);

        /// <inheritdoc />
        public Task<string> GetInnerHTMLAsync() => _channel.GetInnerHTMLAsync();

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync() => _channel.GetInnerTextAsync();

        /// <inheritdoc />
        public Task<string> GetTextContentAsync() => _channel.GetTextContentAsync();

        /// <inheritdoc />
        public Task SelectTextAsync(float? timeout) => _channel.SelectTextAsync(timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(string values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<string> values, bool? noWaitAfter, float? timeout)
            => _channel.SelectOptionAsync(values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IElementHandle values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<IElementHandle> values, bool? noWaitAfter, float? timeout)
            => _channel.SelectOptionAsync(values.Cast<ElementHandle>(), noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(ElementHandleValues values, bool? noWaitAfter, float? timeout)
            => SelectOptionAsync(new[] { values }, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(IEnumerable<ElementHandleValues> values, bool? noWaitAfter, float? timeout)
            => _channel.SelectOptionAsync(values, noWaitAfter, timeout);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(params string[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(params ElementHandleValues[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> SelectOptionAsync(params IElementHandle[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task CheckAsync(bool? force, bool? noWaitAfter, float? timeout)
            => _channel.CheckAsync(timeout, force ?? false, noWaitAfter);

        /// <inheritdoc />
        public Task UncheckAsync(bool? force, bool? noWaitAfter, float? timeout)
            => _channel.UncheckAsync(timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task TapAsync(ElementHandlePosition position, IEnumerable<KeyboardModifier> modifiers, bool? force, bool? noWaitAfter, float? timeout)
            => _channel.TapAsync(position, modifiers, timeout, force ?? false, noWaitAfter);

        /// <inheritdoc />
        public Task<bool> GetIsCheckedAsync() => _channel.GetIsCheckedAsync();

        /// <inheritdoc />
        public Task<bool> GetIsDisabledAsync() => _channel.GetIsDisabledAsync();

        /// <inheritdoc />
        public Task<bool> GetIsEditableAsync() => _channel.GetIsEditableAsync();

        /// <inheritdoc />
        public Task<bool> GetIsEnabledAsync() => _channel.GetIsEnabledAsync();

        /// <inheritdoc />
        public Task<bool> GetIsHiddenAsync() => _channel.GetIsHiddenAsync();

        /// <inheritdoc />
        public Task<bool> GetIsVisibleAsync() => _channel.GetIsVisibleAsync();

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
