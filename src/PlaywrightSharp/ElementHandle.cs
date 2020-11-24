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
            _channel.PreviewUpdated += (sender, e) => Preview = e.Preview;
        }

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<ElementHandle> IChannelOwner<ElementHandle>.Channel => _channel;

        internal IChannel<ElementHandle> ElementChannel => _channel;

        /// <inheritdoc />
        public async Task<IElementHandle> WaitForSelectorAsync(string selector, WaitForState? state = null, int? timeout = null)
            => (await _channel.WaitForSelectorAsync(
                selector: selector,
                state: state,
                timeout: timeout).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task WaitForElementStateAsync(ElementState state, int? timeout = null)
            => _channel.WaitForElementStateAsync(state, timeout);

        /// <inheritdoc />
        public Task PressAsync(string key, int delay = 0, int? timeout = null, bool? noWaitAfter = null)
            => _channel.PressAsync(key, delay, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task TypeAsync(string text, int delay = 0, int? timeout = null, bool? noWaitAfter = null)
            => _channel.TypeAsync(text, delay, timeout, noWaitAfter);

        /// <inheritdoc />
        public async Task<byte[]> ScreenshotAsync(
            string path = null,
            bool omitBackground = false,
            ScreenshotFormat? type = null,
            int? quality = null,
            int? timeout = null)
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
        public Task FillAsync(string value, int? timeout = null, bool? noWaitAfter = null)
            => _channel.FillAsync(value, timeout, noWaitAfter);

        /// <inheritdoc />
        public async Task<IFrame> GetContentFrameAsync() => (await _channel.GetContentFrameAsync().ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public Task HoverAsync(
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false)
            => _channel.HoverAsync(modifiers, position, timeout, force);

        /// <inheritdoc />
        public Task ScrollIntoViewIfNeededAsync(int? timeout = null) => _channel.ScrollIntoViewIfNeededAsync(timeout);

        /// <inheritdoc />
        public async Task<IFrame> GetOwnerFrameAsync() => (await _channel.GetOwnerFrameAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc />
        public Task<Rect> GetBoundingBoxAsync() => _channel.GetBoundingBoxAsync();

        /// <inheritdoc />
        public Task ClickAsync(
            int delay = 0,
            MouseButton button = MouseButton.Left,
            int clickCount = 1,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null)
            => _channel.ClickAsync(delay, button, clickCount, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task DblClickAsync(
            int delay = 0,
            MouseButton button = MouseButton.Left,
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false,
            bool? noWaitAfter = null)
            => _channel.DblClickAsync(delay, button, modifiers, position, timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string file, int? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(new[] { file }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(string[] files, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(files.Select(f => f.ToFilePayload()).ToArray(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(FilePayload file, int? timeout = null, bool? noWaitAfter = null)
            => SetInputFilesAsync(new[] { file }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task SetInputFilesAsync(FilePayload[] files, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SetInputFilesAsync(files, timeout, noWaitAfter);

        /// <inheritdoc />
        public async Task<IElementHandle> QuerySelectorAsync(string selector)
            => (await _channel.QuerySelectorAsync(selector).ConfigureAwait(false))?.Object;

        /// <inheritdoc />
        public async Task<IEnumerable<IElementHandle>> QuerySelectorAllAsync(string selector)
            => (await _channel.QuerySelectorAllAsync(selector).ConfigureAwait(false)).Select(e => ((ElementHandleChannel)e).Object);

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAsync<T>(string selector, string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAsync(string selector, string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAsync<T>(string selector, string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string pageFunction)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: EvaluateArgument.Undefined).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<JsonElement?>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public async Task<T> EvalOnSelectorAllAsync<T>(string selector, string pageFunction, object arg)
            => ScriptsHelper.ParseEvaluateResult<T>(await _channel.EvalOnSelectorAllAsync(
                selector: selector,
                script: pageFunction,
                isFunction: pageFunction.IsJavascriptFunction(),
                arg: ScriptsHelper.SerializedArgument(arg)).ConfigureAwait(false));

        /// <inheritdoc />
        public Task FocusAsync() => _channel.FocusAsync();

        /// <inheritdoc />
        public Task DispatchEventAsync(string type, object eventInit = null, int? timeout = null)
            => _channel.DispatchEventAsync(
                type,
                eventInit == null ? EvaluateArgument.Undefined : ScriptsHelper.SerializedArgument(eventInit),
                timeout);

        /// <inheritdoc />
        public Task<string> GetAttributeAsync(string name, int? timeout = null) => _channel.GetAttributeAsync(name, timeout);

        /// <inheritdoc />
        public Task<string> GetInnerHtmlAsync(int? timeout = null) => _channel.GetInnerHtmlAsync(timeout);

        /// <inheritdoc />
        public Task<string> GetInnerTextAsync(int? timeout = null) => _channel.GetInnerTextAsync(timeout);

        /// <inheritdoc />
        public Task<string> GetTextContentAsync(int? timeout = null) => _channel.GetTextContentAsync(timeout);

        /// <inheritdoc />
        public Task SelectTextAsync(int? timeout = null) => _channel.SelectTextAsync(timeout);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string value, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(string[] values, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SelectOptionAsync(values.Cast<object>().Select(v => v == null ? v : new { value = v }).ToArray(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(IElementHandle value, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(IElementHandle[] value, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SelectOptionAsync(value.Cast<ElementHandle>(), timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(SelectOption value, int? timeout = null, bool? noWaitAfter = null)
            => SelectOptionAsync(new[] { value }, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(SelectOption[] value, int? timeout = null, bool? noWaitAfter = null)
            => _channel.SelectOptionAsync(value, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(int? timeout = null, bool? noWaitAfter = null)
        => _channel.SelectOptionAsync(null, timeout, noWaitAfter);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params string[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params SelectOption[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task<string[]> SelectOptionAsync(params IElementHandle[] values) => SelectOptionAsync(values);

        /// <inheritdoc />
        public Task CheckAsync(int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.CheckAsync(timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task UncheckAsync(int? timeout = null, bool force = false, bool? noWaitAfter = null)
            => _channel.UncheckAsync(timeout, force, noWaitAfter);

        /// <inheritdoc />
        public Task TapAsync(Point? position = null, Modifier[] modifiers = null, bool force = false, bool noWaitAfter = false, int timeout = 0)
            => _channel.TapAsync(position, modifiers, force, noWaitAfter, timeout);

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

        internal Task<string> CreateSelectorForTestAsync(string name) => _channel.CreateSelectorForTestAsync(name);
    }
}
