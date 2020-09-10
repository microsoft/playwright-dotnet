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
                    PreviewUpdated?.Invoke(this, new PreviewUpdatedEventArgs { Preview = serverParams.Value.ToString() });
                    break;
            }
        }

        internal Task<ElementHandleChannel> QuerySelectorAsync(string selector)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "querySelector",
                new Dictionary<string, object>
                {
                    ["selector"] = selector,
                });

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

        internal Task<string> ScreenshotAsync(string path, bool omitBackground, ScreenshotFormat? type, int? quality, int? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["path"] = path,
                ["omitBackground"] = omitBackground,
                ["type"] = type,
                ["timeout"] = timeout,
            };

            if (quality != null)
            {
                args["quality"] = quality;
            }

            return Scope.SendMessageToServer<string>(Guid, "screenshot", args);
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
                ["timeout"] = timeout,
                ["position"] = position,
            };

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
            bool noWaitAfter)
        {
            var args = new Dictionary<string, object>
            {
                ["delay"] = delay,
                ["button"] = button,
                ["clickCount"] = clickCount,
                ["force"] = force,
                ["noWaitAfter"] = noWaitAfter,
            };

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
            bool noWaitAfter)
        {
            var args = new Dictionary<string, object>
            {
                ["delay"] = delay,
                ["button"] = button,
                ["force"] = force,
                ["noWaitAfter"] = noWaitAfter,
            };

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

        internal Task<Rect> GetBoundingBoxAsync() => Scope.SendMessageToServer<Rect>(Guid, "boundingBox", null);

        internal Task ScrollIntoViewIfNeededAsync(int? timeout)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "scrollIntoViewIfNeeded",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task FillAsync(string value, int? timeout, bool noWaitAfter)
            => Scope.SendMessageToServer(
                Guid,
                "fill",
                new Dictionary<string, object>
                {
                    ["value"] = value,
                    ["noWaitAfter"] = noWaitAfter,
                    ["timeout"] = timeout,
                });

        internal Task DispatchEventAsync(string type, object eventInit, int? timeout)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "dispatchEvent",
                new Dictionary<string, object>
                {
                    ["type"] = type,
                    ["eventInit"] = eventInit,
                    ["timeout"] = timeout,
                });

        internal Task SetInputFilesAsync(FilePayload[] files)
            => Scope.SendMessageToServer<string>(
                Guid,
                "setInputFiles",
                new Dictionary<string, object>
                {
                    ["files"] = files,
                });

        internal Task<string> GetAttributeAsync(string name, int? timeout)
            => Scope.SendMessageToServer<string>(
                Guid,
                "getAttribute",
                new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["timeout"] = timeout,
                });

        internal Task<JSHandleChannel> EvaluateExpressionHandleAsync(string script, bool isFunction, object arg)
            => Scope.SendMessageToServer<JSHandleChannel>(
                Guid,
                "evaluateExpressionHandle",
                new Dictionary<string, object>
                {
                    ["expression"] = script,
                    ["isFunction"] = isFunction,
                    ["arg"] = arg,
                });

        internal async Task<string> GetInnerHtmlAsync(int? timeout)
            => (await Scope.SendMessageToServer(
                Guid,
                "innterHTML",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                }).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> GetInnerTextAsync(int? timeout)
            => (await Scope.SendMessageToServer(
                Guid,
                "innerText",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                }).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> GetTextContentAsync(int? timeout)
            => (await Scope.SendMessageToServer(
                Guid,
                "textContent",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                }).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal Task SelectTextAsync(int? timeout)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "selectText",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task<string[]> SelectOptionAsync(object values, bool? noWaitAfter = null, int? timeout = null)
            => Scope.SendMessageToServer<string[]>(
                Guid,
                "selectOption",
                new Dictionary<string, object>
                {
                    ["values"] = values,
                    ["noWaitAfter"] = noWaitAfter,
                    ["timeout"] = timeout,
                });

        internal Task CheckAsync(int? timeout, bool force, bool noWaitAfter)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "check",
                new Dictionary<string, object>
                {
                    ["force"] = force,
                    ["timeout"] = timeout,
                    ["noWaitAfter"] = noWaitAfter,
                });

        internal Task UncheckAsync(int? timeout, bool force, bool noWaitAfter)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "uncheck",
                new Dictionary<string, object>
                {
                    ["force"] = force,
                    ["timeout"] = timeout,
                    ["noWaitAfter"] = noWaitAfter,
                });

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
