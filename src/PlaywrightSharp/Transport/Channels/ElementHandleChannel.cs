using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Transport.Channels
{
    internal class ElementHandleChannel : JSHandleChannel, IChannel<ElementHandle>
    {
        public ElementHandleChannel(string guid, ConnectionScope scope, ElementHandle owner) : base(guid, scope, owner)
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

        internal Task<ElementHandleChannel> WaitForSelectorAsync(string selector, WaitForState? state, int? timeout)
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

            return Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "waitForSelector",
                args);
        }

        internal Task<ElementHandleChannel> QuerySelectorAsync(string selector)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "querySelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

        internal Task WaitForElementStateAsync(ElementState state, int? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["state"] = state,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "waitForElementState", args);
        }

        internal Task<ChannelBase[]> QuerySelectorAllAsync(string selector)
            => Scope.SendMessageToServer<ChannelBase[]>(
                Guid,
                "querySelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

        internal Task<JSHandleChannel> EvalOnSelectorAsync(string selector, string script, bool isFunction, object arg)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evalOnSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                });

        internal async Task<string> ScreenshotAsync(string path, bool omitBackground, ScreenshotFormat? type, int? quality, int? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["omitBackground"] = omitBackground,
            };

            if (path != null)
            {
                args["path"] = path;
            }

            if (type != null)
            {
                args["type"] = type;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (quality != null)
            {
                args["quality"] = quality;
            }

            return (await Scope.SendMessageToServer(Guid, "screenshot", args).ConfigureAwait(false))?.GetProperty("binary").ToString();
        }

        internal Task<JsonElement?> EvalOnSelectorAsync(string selector, string script, bool isFunction, EvaluateArgument arg)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evalOnSelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                });

        internal Task<JSHandleChannel> EvalOnSelectorAllAsync(string selector, string script, bool isFunction, object arg)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evalOnSelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                });

        internal Task<JsonElement?> EvalOnSelectorAllAsync(string selector, string script, bool isFunction, EvaluateArgument arg)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "evalOnSelectorAll",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                });

        internal Task<FrameChannel> GetContentFrameAsync() => Scope.SendMessageToServer<FrameChannel>(Guid, "contentFrame", null);

        internal Task<FrameChannel> GetOwnerFrameAsync() => Scope.SendMessageToServer<FrameChannel>(Guid, "ownerFrame", null);

        internal Task HoverAsync(
            Modifier[] modifiers = null,
            Point? position = null,
            int? timeout = null,
            bool force = false)
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

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            return Scope.SendMessageToServer<JsonElement?>(Guid, "hover", args);
        }

        internal Task FocusAsync() => Scope.SendMessageToServer(Guid, "focus", null);

        internal Task ClickAsync(
            int delay,
            MouseButton button,
            int clickCount,
            Modifier[] modifiers,
            Point? position,
            int? timeout,
            bool force,
            bool? noWaitAfter)
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

            if (position != null)
            {
                args["position"] = position;
            }

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            return Scope.SendMessageToServer(Guid, "click", args);
        }

        internal Task DoubleClickAsync(
            int delay,
            MouseButton button,
            Modifier[] modifiers,
            Point? position,
            int? timeout,
            bool force,
            bool? noWaitAfter)
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

            if (position != null)
            {
                args["position"] = position;
            }

            if (modifiers != null)
            {
                args["modifiers"] = modifiers?.Select(m => m.ToValueString());
            }

            return Scope.SendMessageToServer(Guid, "dblclick", args);
        }

        internal async Task<Rect> GetBoundingBoxAsync()
        {
            var result = (await Scope.SendMessageToServer(Guid, "boundingBox", null).ConfigureAwait(false)).Value;

            if (result.TryGetProperty("value", out var value))
            {
                return value.ToObject<Rect>();
            }

            return null;
        }

        internal Task ScrollIntoViewIfNeededAsync(int? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "scrollIntoViewIfNeeded", args);
        }

        internal Task FillAsync(string value, int? timeout, bool? noWaitAfter)
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

            return Scope.SendMessageToServer(Guid, "fill", args);
        }

        internal Task DispatchEventAsync(string type, object eventInit, int? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["type"] = type,
                ["eventInit"] = eventInit,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "dispatchEvent", args);
        }

        internal Task SetInputFilesAsync(FilePayload[] files)
            => Scope.SendMessageToServer<string>(
                Guid,
                "setInputFiles",
                new Dictionary<string, object>
                {
                    ["files"] = files,
                });

        internal async Task<string> GetAttributeAsync(string name, int? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["name"] = name,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return (await Scope.SendMessageToServer(Guid, "getAttribute", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal async Task<string> GetInnerHtmlAsync(int? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return (await Scope.SendMessageToServer(Guid, "innerHTML", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal async Task<string> GetInnerTextAsync(int? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return (await Scope.SendMessageToServer(Guid, "innerText", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal async Task<string> GetTextContentAsync(int? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return (await Scope.SendMessageToServer(Guid, "textContent", args).ConfigureAwait(false))?.GetProperty("value").ToString();
        }

        internal Task SelectTextAsync(int? timeout)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "selectText", args);
        }

        internal async Task<string[]> SelectOptionAsync(object values, bool? noWaitAfter = null, int? timeout = null)
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

            return (await Scope.SendMessageToServer(Guid, "selectOption", args).ConfigureAwait(false))?.GetProperty("values").ToObject<string[]>();
        }

        internal Task CheckAsync(int? timeout, bool force, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>
            {
                ["force"] = force,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "check", args);
        }

        internal Task UncheckAsync(int? timeout, bool force, bool? noWaitAfter)
        {
            var args = new Dictionary<string, object>
            {
                ["force"] = force,
            };

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (noWaitAfter != null)
            {
                args["noWaitAter"] = noWaitAfter;
            }

            return Scope.SendMessageToServer<ElementHandleChannel>(Guid, "uncheck", args);
        }

        internal Task TypeAsync(string text, int delay)
            => Scope.SendMessageToServer(
                Guid,
                "type",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                    ["delay"] = delay,
                });

        internal Task PressAsync(string key, int delay)
            => Scope.SendMessageToServer(
                Guid,
                "press",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["delay"] = delay,
                });
    }
}
