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
        public async Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForState? state = null, float? timeout = null)
            => (await _channel.WaitForSelectorAsync(
                selector: selector,
                state: state,
                timeout: timeout).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task WaitForElementStateAsync(ElementState state, float? timeout = null)
            => _channel.WaitForElementStateAsync(state, timeout);

        /// <inheritdoc />
        public Task PressAsync(string key, int delay = 0, float? timeout = null, bool? noWaitAfter = null)
            => _channel.PressAsync(key, delay, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task TypeAsync(string text, int delay = 0, float? timeout = null, bool? noWaitAfter = null)
            => _channel.TypeAsync(text, delay, timeout, noWaitAfter);

        /// <inheritdoc />
        public async Task<byte[]> ScreenshotAsync(
            string path = null,
            bool omitBackground = false,
            ScreenshotFormat? type = null,
            int? quality = null,
            float? timeout = null)
        {
            type = !string.IsNullOrEmpty(path) ? DetermineScreenshotType(path) : type;
            byte[] result = Convert.FromBase64String(await _channel.ScreenshotAsync(path, omitBackground, type, quality, timeout).ConfigureAwait(false));

            if (!string.IsNullOrEmpty(path))
            {
                Directory.CreateDirectory(new FileInfo(path).Directory.FullName);
                File.WriteAllBytes(path, result);
            }

            return result;
        }

        /// <inheritdoc />
        public Task FillAsync(string value, float? timeout = null, bool? noWaitAfter = null)
            => _channel.FillAsync(value, timeout, noWaitAfter);

        /// <inheritdoc />
        public async Task<IFrame> GetContentFrameAsync() => (await _channel.GetContentFrameAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task HoverAsync(
            Modifier[] modifiers = null,
            Point? position = null,
            float? timeout = null,
            bool force = false)
            => _channel.HoverAsync(modifiers, position, timeout, force);

        /// <inheritdoc />
        public Task ScrollIntoViewIfNeededAsync(float? timeout = null) => _channel.ScrollIntoViewIfNeededAsync(timeout);

        /// <inheritdoc />
        public async Task<IFrame> GetOwnerFrameAsync() => (await _channel.GetOwnerFrameAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task<ElementHandleBoundingBoxResult> GetBoundingBoxAsync() => _channel.GetBoundingBoxAsync();

        /// <inheritdoc />
        public Task ClickAsync(
            int delay = 0,
            MouseButton button = MouseButton.Left,
            int clickCount = 1,
            Modifier[] modifiers = null,
            Point? position = null,
            float? timeout = null,
            bool force = false,
            bool? noWaitAfter = null)
            => _channel.ClickAsync(delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task DblClickAsync(
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            float? timeout = null,
            bool force = false,
            bool? noWaitAfter = null)
            => _channel.DblClickAsync(delay, button, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string file, float? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(new[] { file }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string[] files, float? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(files.Select(f => f.ToFilePayload()).ToArray(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(FilePayload file, float? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(new[] { file }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(FilePayload[] files, float? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(files, timeout, noWaitAfter);

        /// <inheritdoc />
        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(e => ((ElementHandleChannel)e).Object);

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAsync<T>(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string expression)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string expression, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: expression,
                isFunction: expression.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public Task FocusAsync() => _channel.FocusAsync();

        /// <inheritdoc />
        public Task DispatchEventAsync(string type, object eventInit = null, float? timeout = null)
            => _channel.DispatchEventAsync(
                type,
                eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit),
                timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string name, float? timeout = null) => _channel.GetAttributeAsync(name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHtmlAsync(float? timeout = null) => _channel.GetInnerHtmlAsync(timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(float? timeout = null) => _channel.GetInnerTextAsync(timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(float? timeout = null) => _channel.GetTextContentAsync(timeout);

        /// <inheritdoc />
        public Task SelectTextAsync(float? timeout = null) => _channel.SelectTextAsync(timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string value, float? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string[] values, float? timeout = null, bool? noWaitAfter = null)
            => _channel.SelectOptionAsync(values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(IElementHandle value, float? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(IElementHandle[] value, float? timeout = null, bool? noWaitAfter = null)
            => _channel.SelectOptionAsync(value.Cast<ElementHandle>(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(SelectOption value, float? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(SelectOption[] value, float? timeout = null, bool? noWaitAfter = null)
            => _channel.SelectOptionAsync(value, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(float? timeout = null, bool? noWaitAfter = null)
        => _channel.SelectOptionAsync(null, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params string[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params SelectOption[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params IElementHandle[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task CheckAsync(float? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.CheckAsync(timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task UncheckAsync(float? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.UncheckAsync(timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task TapAsync(Point? position = null, Modifier[] modifiers = null, float? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.TapAsync(position, modifiers, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task<bool> IsCheckedAsync() => _channel.IsCheckedAsync();

        /// <inheritdoc />
        public Task<bool> IsDisabledAsync() => _channel.IsDisabledAsync();

        /// <inheritdoc />
        public Task<bool> IsEditableAsync() => _channel.IsEditableAsync();

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync() => _channel.IsEnabledAsync();

        /// <inheritdoc />
        public Task<bool> IsHiddenAsync() => _channel.IsHiddenAsync();

        /// <inheritdoc />
        public Task<bool> IsVisibleAsync() => _channel.IsVisibleAsync();

        internal static ScreenshotFormat? DetermineScreenshotType(string path)
        {
            string mimeType = path.MimeType();
            return mimeType switch
            {
                "image/png" => ScreenshotFormat.Png,
                "image/jpeg" => ScreenshotFormat.Jpeg,
                _ => throw new ArgumentException($"path: unsupported mime type \"{mimeType}\""),
            };
        }
    }
}
