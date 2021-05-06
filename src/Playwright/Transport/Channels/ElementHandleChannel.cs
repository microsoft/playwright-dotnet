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
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class ElementHandleChannel : JSHandleChannel, IChannel<ElementHandle>
    {
        public ElementHandleChannel(string guid, Connection connection, ElementHandle owner) : base(guid, connection, owner)
        {
            Object = owner;
        }

        internal event EventHandler<PreviewUpdatedEventArgs> PreviewUpdated;

        public new ElementHandle Object { get; set; }

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "previewUpdated":
                    PreviewUpdated?.Invoke(this, new PreviewUpdatedEventArgs { Preview = serverParams.Value.GetProperty("preview").ToString() });
                    break;
            }
        }

        internal Task<ElementHandleChannel> WaitForSelectorAsync(string selector, WaitForSelectorState? state, float? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["selector"] = selector,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (state != null)
            {
                args["state"] = state;
            }

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(
                Guid,
                "waitForSelector",
                args);
        }

        internal Task<ElementHandleChannel> QuerySelectorAsync(string selector)
            => Connection.SendMessageToServerAsync<ElementHandleChannel>(
                Guid,
                "querySelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

        internal Task WaitForElementStateAsync(ElementState state, float? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["state"] = state,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "waitForElementState", args);
        }

        internal Task<ChannelBase[]> QuerySelectorAllAsync(string selector)
            => Connection.SendMessageToServerAsync<ChannelBase[]>(
                Guid,
                "querySelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

        internal async Task<string> ScreenshotAsync(string path, bool omitBackground, ScreenshotType type, int? quality, float? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["omitBackground"] = omitBackground,
                ["type"] = type,
            };

            if (path != null)
            {
                args["path"] = path;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (quality != null)
            {
                args["quality"] = quality;
            }

            return (await Connection.SendMessageToServerAsync(Guid, "screenshot", args).ConfigureAwait(false))?.GetProperty("binary").ToString();
        }

        internal Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, bool isFunction, object arg)
            => Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evalOnSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = ScriptsHelper.SerializedArgument(arg),
                });

        internal Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, bool isFunction, object arg)
            => Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "evalOnSelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = ScriptsHelper.SerializedArgument(arg),
                });

        internal Task<FrameChannel> ContentFrameAsync() => Connection.SendMessageToServerAsync<FrameChannel>(Guid, "contentFrame", null);

        internal Task<FrameChannel> OwnerFrameAsync() => Connection.SendMessageToServerAsync<FrameChannel>(Guid, "ownerFrame", null);

        internal Task HoverAsync(
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool force,
            bool? trial)
        {
            var args = new Dictionary<string, object>
            {
                ["force"] = force,
            };

            if (position != null)
            {
                args["position"] = position;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (trial != null)
            {
                args["trial"] = trial;
            }

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            return Connection.SendMessageToServerAsync<JsonElement?>(Guid, "hover", args);
        }

        internal Task FocusAsync() => Connection.SendMessageToServerAsync(Guid, "focus", null);

        internal Task ClickAsync(
            float delay,
            MouseButton button,
            int clickCount,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool force,
            bool? noWaitAfter,
            bool? trial)
        {
            var args = new Dictionary<string, object>
            {
                ["delay"] = delay,
                ["button"] = button,
                ["clickCount"] = clickCount,
                ["force"] = force,
            };

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (trial != null)
            {
                args["trial"] = trial;
            }

            if (position != null)
            {
                args["position"] = position;
            }

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            return Connection.SendMessageToServerAsync(Guid, "click", args);
        }

        internal Task DblClickAsync(
            float delay,
            MouseButton button,
            IEnumerable<KeyboardModifier> modifiers,
            Position position,
            float? timeout,
            bool force,
            bool? noWaitAfter,
            bool? trial)
        {
            var args = new Dictionary<string, object>
            {
                ["delay"] = delay,
                ["button"] = button,
                ["force"] = force,
            };

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (trial != null)
            {
                args["trial"] = trial;
            }

            if (position != null)
            {
                args["position"] = position;
            }

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            return Connection.SendMessageToServerAsync(Guid, "dblclick", args);
        }

        internal async Task<ElementHandleBoundingBoxResult> BoundingBoxAsync()
        {
            var result = (await Connection.SendMessageToServerAsync(Guid, "boundingBox", null).ConfigureAwait(false)).Value;

            if (result.TryGetProperty("value", out var value))
            {
                return value.ToObject<ElementHandleBoundingBoxResult>();
            }

            return null;
        }

        internal Task ScrollIntoViewIfNeededAsync(float? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "scrollIntoViewIfNeeded", args);
        }

        internal Task FillAsync(string value, bool? noWaitAfter, float? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["value"] = value,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Connection.SendMessageToServerAsync(Guid, "fill", args);
        }

        internal Task DispatchEventAsync(string type, object eventInit)
        {
            var args = new Dictionary<string, object>
            {
                ["type"] = type,
                ["eventInit"] = eventInit,
            };

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "dispatchEvent", args);
        }

        internal Task SetInputFilesAsync(IEnumerable<FilePayload> files, bool? noWaitAfter, float? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["files"] = files.Select(f => new
                {
                    f.Name,
                    Buffer = Convert.ToBase64String(f.Buffer),
                    f.MimeType,
                }),
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Connection.SendMessageToServerAsync<string>(Guid, "setInputFiles", args);
        }

        internal async Task<string> GetAttributeAsync(string name)
        {
            var args = new Dictionary<string, object>
            {
                ["name"] = name,
            };

            return (await Connection.SendMessageToServerAsync(Guid, "getAttribute", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal async Task<string> InnerHTMLAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "innerHTML").ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> InnerTextAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "innerText").ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> TextContentAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "textContent").ConfigureAwait(false))?.GetProperty("value").ToString();

        internal Task SelectTextAsync(float? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "selectText", args);
        }

        internal async Task<IReadOnlyCollection<string>> SelectOptionAsync(object values, bool? noWaitAfter = null, float? timeout = null)
        {
            var args = new Dictionary<string, object>();

            if (values != null)
            {
                if (values is IElementHandle[])
                {
                    args["elements"] = values;
                }
                else
                {
                    args["options"] = values;
                }
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return (await Connection.SendMessageToServerAsync(Guid, "selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<List<string>>().AsReadOnly();
        }

        internal async Task<bool> IsVisibleAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "isVisible", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

        internal async Task<bool> IsHiddenAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "isHidden", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

        internal async Task<bool> IsEnabledAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "isEnabled", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

        internal async Task<bool> IsEditableAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "isEditable", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

        internal async Task<bool> IsDisabledAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "isDisabled", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

        internal async Task<bool> IsCheckedAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "isChecked", null).ConfigureAwait(false))?.GetProperty("value").GetBoolean() ?? default;

        internal Task CheckAsync(Position position, float? timeout, bool force, bool? noWaitAfter, bool? trial)
        {
            var args = new Dictionary<string, object>
            {
                ["force"] = force,
            };

            if (position != null)
            {
                args["position"] = position;
            }

            if (trial != null)
            {
                args["trial"] = trial;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "check", args);
        }

        internal Task UncheckAsync(Position position, float? timeout, bool? force, bool? noWaitAfter, bool? trial)
        {
            var args = new Dictionary<string, object>();

            if (force != null)
            {
                args["force"] = force;
            }

            if (position != null)
            {
                args["position"] = position;
            }

            if (trial != null)
            {
                args["trial"] = trial;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Connection.SendMessageToServerAsync<ElementHandleChannel>(Guid, "uncheck", args);
        }

        internal Task TypeAsync(string text, float delay, float? timeout, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>
            {
                ["text"] = text,
                ["delay"] = delay,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Connection.SendMessageToServerAsync(Guid, "type", args);
        }

        internal Task PressAsync(string key, float delay, float? timeout, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>
            {
                ["key"] = key,
                ["delay"] = delay,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Connection.SendMessageToServerAsync(Guid, "press", args);
        }

        internal Task TapAsync(
            Position position,
            IEnumerable<KeyboardModifier> modifiers,
            float? timeout,
            bool force,
            bool? noWaitAfter,
            bool? trial)
        {
            var args = new Dictionary<string, object>
            {
                ["force"] = force,
            };

            if (noWaitAfter != null)
            {
                args["noWaitAfter"] = noWaitAfter;
            }

            if (position != null)
            {
                args["position"] = new Dictionary<string, object>
                {
                    ["x"] = position.X,
                    ["y"] = position.Y,
                };
            }

            if (modifiers != null)
            {
                args["modifiers"] = modifiers.Select(m => m.ToValueString());
            }

            if (trial != null)
            {
                args["trial"] = trial;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Connection.SendMessageToServerAsync(Guid, "tap", args);
        }
    }
}
