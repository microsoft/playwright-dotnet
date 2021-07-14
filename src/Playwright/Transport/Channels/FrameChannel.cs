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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class FrameChannel : Channel<Frame>
    {
        public FrameChannel(string guid, Connection connection, Frame owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler<FrameNavigatedEventArgs> Navigated;

        internal event EventHandler<FrameChannelLoadStateEventArgs> LoadState;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "navigated":
                    var e = serverParams?.ToObject<FrameNavigatedEventArgs>(Connection.GetDefaultJsonSerializerOptions());

                    if (serverParams.Value.TryGetProperty("newDocument", out var documentElement))
                    {
                        e.NewDocument = documentElement.ToObject<NavigateDocument>(Connection.GetDefaultJsonSerializerOptions());
                    }

                    Navigated?.Invoke(this, e);
                    break;
                case "loadstate":
                    LoadState?.Invoke(
                        this,
                        serverParams?.ToObject<FrameChannelLoadStateEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
            }
        }

        internal Task<ResponseChannel> GotoAsync(string url, float? timeout, WaitUntilState? waitUntil, string referer)
        {
            var args = new Dictionary<string, object>();
            args["url"] = url;
            args["timeout"] = timeout;
            args["waitUntil"] = waitUntil;
            args["referer"] = referer;
            return Connection.SendMessageToServerAsync<ResponseChannel>(Guid, "goto", args);
        }

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(
            string script,
            object arg)
        {
            return Connection.SendMessageToServerAsync<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["arg"] = arg,
                });
        }

        internal Task<JSHandleChannel> WaitForFunctionAsync(
            string expression,
            object arg,
            float? timeout,
            float? polling)
        {
            var args = new Dictionary<string, object>();
            args["expression"] = expression;
            args["arg"] = arg;
            args["timeout"] = timeout;
            args["pollingInterval"] = polling;
            return Connection.SendMessageToServerAsync<JSHandleChannel>(
                Guid,
                "waitForFunction",
                args);
        }

        internal Task<JsonElement?> EvaluateExpressionAsync(
            string script,
            object arg)
        {
            return Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evaluateExpression",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["arg"] = arg,
                });
        }

        internal Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, object arg)
            => Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evalOnSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["arg"] = arg,
                });

        internal Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, object arg)
            => Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evalOnSelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["arg"] = arg,
                });

        internal Task<ElementHandleChannel> FrameElementAsync() => Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "frameElement", null);

        internal async Task<string> TitleAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "title", null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal Task<ElementHandleChannel> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;
            args["state"] = state;
            return Connection.SendMessageToServerAsync<ElementHandleChannel>(
                Guid,
                "waitForSelector",
                args);
        }

        internal Task<ElementHandleChannel> AddScriptTagAsync(string url, string path, string content, string type)
        {
            var args = new Dictionary<string, object>();
            args["url"] = url;
            args["path"] = path;
            args["content"] = content;
            args["type"] = type;
            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "addScriptTag", args);
        }

        internal Task<ElementHandleChannel> AddStyleTagAsync(string url, string path, string content)
        {
            var args = new Dictionary<string, object>();
            args["url"] = url;
            args["path"] = path;
            args["content"] = content;
            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "addStyleTag", args);
        }

        internal Task<ResponseChannel> WaitForNavigationAsync(LoadState? waitUntil, string url, float? timeout)
        {
            var param = new Dictionary<string, object>();
            param["timeout"] = timeout;
            param["url"] = url;
            param["waitUntil"] = waitUntil;

            return Connection.SendMessageToServerAsync<ResponseChannel>(Guid, "waitForNavigation", param);
        }

        internal Task WaitForLoadStateAsync(LoadState? state, float? timeout)
        {
            var param = new Dictionary<string, object>();
            param["timeout"] = timeout;
            param["state"] = state;

            return Connection.SendMessageToServerAsync(
                Guid,
                "waitForLoadState",
                param);
        }

        internal Task SetContentAsync(string html, float? timeout, WaitUntilState? waitUntil)
        {
            var args = new Dictionary<string, object>();
            args["html"] = html;
            args["waitUntil"] = waitUntil;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync(Guid, "setContent", args);
        }

        internal Task ClickAsync(
            string selector,
            float? delay,
            MouseButton? button,
            int? clickCount,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool? force,
            bool? noWaitAfter,
            bool? trial)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["button"] = button;
            args["force"] = force;
            args["delay"] = delay;
            args["clickCount"] = clickCount;
            args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            args["position"] = position;
            args["noWaitAfter"] = noWaitAfter;
            args["trial"] = trial;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "click", args);
        }

        internal Task DblClickAsync(
            string selector,
            float? delay,
            MouseButton? button,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            float? timeout,
            bool? force,
            bool? noWaitAfter,
            bool? trial)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["button"] = button;
            args["delay"] = delay;
            args["force"] = force;
            args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            args["position"] = position;
            args["trial"] = trial;
            args["timeout"] = timeout;
            args["noWaitAfter"] = noWaitAfter;

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "dblclick", args);
        }

        internal Task<ElementHandleChannel> QuerySelectorAsync(string selector)
            => Connection.SendMessageToServerAsync<ElementHandleChannel>(
                Guid,
                "querySelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

        internal Task<ChannelBase[]> QuerySelectorAllAsync(string selector)
            => Connection.SendMessageToServerAsync<ChannelBase[]>(
                Guid,
                "querySelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

        internal Task FillAsync(string selector, string value, float? timeout, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["value"] = value;
            args["timeout"] = timeout;
            args["noWaitAfter"] = noWaitAfter;

            return Connection.SendMessageToServerAsync(Guid, "fill", args);
        }

        internal Task CheckAsync(string selector, Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["force"] = force;
            args["position"] = position;
            args["noWaitAfter"] = noWaitAfter;
            args["trial"] = trial;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "check", args);
        }

        internal Task UncheckAsync(string selector, Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["force"] = force;
            args["position"] = position;
            args["noWaitAfter"] = noWaitAfter;
            args["trial"] = trial;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "uncheck", args);
        }

        internal Task DispatchEventAsync(string selector, string type, object eventInit, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["type"] = type;
            args["eventInit"] = eventInit;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync(Guid, "dispatchEvent", args);
        }

        internal Task HoverAsync(
            string selector,
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            bool? force,
            float? timeout,
            bool? trial)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["force"] = force;
            args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            args["position"] = position;
            args["trial"] = trial;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync(Guid, "hover", args);
        }

        internal Task<string[]> PressAsync(string selector, string text, float? delay, float? timeout, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["key"] = text;
            args["delay"] = delay;
            args["timeout"] = timeout;
            args["noWaitAfter"] = noWaitAfter;

            return Connection.SendMessageToServerAsync<string[]>(Guid, "press", args);
        }

        internal async Task<string[]> SelectOptionAsync(string selector, IEnumerable<SelectOptionValue> values, bool? noWaitAfter, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["options"] = values;
            args["noWaitAfter"] = noWaitAfter;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
        }

        internal async Task<string[]> SelectOptionAsync(string selector, IEnumerable<ElementHandle> values, bool? noWaitAfter, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["elements"] = values;
            args["noWaitAfter"] = noWaitAfter;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
        }

        internal async Task<string> GetAttributeAsync(string selector, string name, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["name"] = name;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "getAttribute", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal async Task<string> InnerHTMLAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "innerHTML", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal Task TypeAsync(string selector, string text, float? delay, float? timeout, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["text"] = text;
            args["delay"] = delay;
            args["noWaitAfter"] = noWaitAfter;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync(Guid, "type", args);
        }

        internal async Task<string> ContentAsync()
            => (await Connection.SendMessageToServerAsync(
                Guid,
                "content",
                null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal Task FocusAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync(Guid, "focus", args);
        }

        internal async Task<string> InnerTextAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "innerText", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal Task SetInputFilesAsync(string selector, IEnumerable<FilePayload> files, bool? noWaitAfter, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["files"] = files.Select(f => new
            {
                f.Name,
                Buffer = Convert.ToBase64String(f.Buffer),
                f.MimeType,
            });
            args["noWaitAfter"] = noWaitAfter;
            args["timeout"] = timeout;

            return Connection.SendMessageToServerAsync<string>(Guid, "setInputFiles", args);
        }

        internal async Task<string> TextContentAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "textContent", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal Task TapAsync(
            string selector,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool? force,
            bool? noWaitAfter,
            bool? trial)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["force"] = force;
            args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            args["noWaitAfter"] = noWaitAfter;
            args["trial"] = trial;
            args["timeout"] = timeout;
            args["position"] = position;

            return Connection.SendMessageToServerAsync(Guid, "tap", args);
        }

        internal async Task<bool> IsCheckedAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "isChecked", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
        }

        internal async Task<bool> IsDisabledAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "isDisabled", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
        }

        internal async Task<bool> IsEditableAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "isEditable", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
        }

        internal async Task<bool> IsEnabledAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "isEnabled", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
        }

        internal async Task<bool> IsHiddenAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "isHidden", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
        }

        internal async Task<bool> IsVisibleAsync(string selector, float? timeout)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;
            args["timeout"] = timeout;

            return (await Connection.SendMessageToServerAsync(Guid, "isVisible", args).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;
        }

        internal async Task<string> InputValueAsync(string selector)
        {
            var args = new Dictionary<string, object>();
            args["selector"] = selector;

            return (await Connection.SendMessageToServerAsync(Guid, "inputValue", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }
    }
}
