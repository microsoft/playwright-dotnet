using System;
using System.Collections.Generic;
using System.Linq;
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

        public new ElementHandle Object { get; set; }

        internal Task<FrameChannel> GetContentFrameAsync() => Scope.SendMessageToServer<FrameChannel>(Guid, "contentFrame", null);

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

        internal Task SelectTextAsync(int? timeout)
            => Scope.SendMessageToServer<ElementHandleChannel>(
                Guid,
                "selectText",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });
    }
}
