using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

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

        internal Task<FrameChannel> GetContentFrameAsync() => Scope.SendMessageToServer<FrameChannel>(Guid, "contentFrame", null);

        internal Task<FrameChannel> GetOwnerFrameAsync() => Scope.SendMessageToServer<FrameChannel>(Guid, "ownerFrame", null);

        internal Task HoverAsync(PointerActionOptions options) => Scope.SendMessageToServer(Guid, "hover", options);

        internal Task FocusAsync() => Scope.SendMessageToServer(Guid, "focus", null);

        internal Task ClickAsync(ClickOptions options)
            => Scope.SendMessageToServer(
                Guid,
                "click",
                new Dictionary<string, object>
                {
                    ["delay"] = options?.Delay,
                    ["button"] = options?.Button ?? Input.MouseButton.Left,
                    ["clickCount"] = options?.ClickCount ?? 1,
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["position"] = options?.Position,
                    ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
                });

        internal Task DoubleClickAsync(ClickOptions options)
            => Scope.SendMessageToServer(
                Guid,
                "dblclick",
                new Dictionary<string, object>
                {
                    ["delay"] = options?.Delay,
                    ["button"] = options?.Button ?? Input.MouseButton.Left,
                    ["clickCount"] = options?.ClickCount ?? 1,
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["position"] = options?.Position,
                    ["modifiers"] = options?.Modifiers?.Select(m => m.ToValueString()),
                });

        internal Task<Rect> GetBoundingBoxAsync() => Scope.SendMessageToServer<Rect>(Guid, "boundingBox", null);

        internal Task ScrollIntoViewIfNeededAsync(int? timeout)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "scrollIntoViewIfNeeded",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task FillAsync(string value, NavigatingActionWaitOptions options)
            => Scope.SendMessageToServer(
                Guid,
                "fill",
                new Dictionary<string, object>
                {
                    ["value"] = value,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                    ["timeout"] = options?.Timeout,
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

        internal Task<string> GetInnerHtmlAsync(int? timeout)
            => Scope.SendMessageToServer<string>(
                Guid,
                "innerHTML",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task<string> GetInnerTextAsync(int? timeout)
            => Scope.SendMessageToServer<string>(
                Guid,
                "innerText",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task<string> GetTextContentAsync(int? timeout)
            => Scope.SendMessageToServer<string>(
                Guid,
                "textContent",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SelectTextAsync(int? timeout)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "selectText",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SelectOptionAsync(object values, NavigatingActionWaitOptions options)
            => Scope.SendMessageToServer(
                Guid,
                "selectOption",
                new Dictionary<string, object>
                {
                    ["values"] = values,
                    ["noWaitAfter"] = options.NoWaitAfter,
                    ["timeout"] = options.Timeout,
                });

        internal Task CheckAsync(CheckOptions options)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "check",
                new Dictionary<string, object>
                {
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
                });

        internal Task UncheckAsync(CheckOptions options)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "uncheck",
                new Dictionary<string, object>
                {
                    ["force"] = options?.Force,
                    ["timeout"] = options?.Timeout,
                    ["noWaitAfter"] = options?.NoWaitAfter,
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
