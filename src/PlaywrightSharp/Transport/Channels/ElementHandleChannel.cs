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

        internal Task ClickAsync(ClickOptions options)
            => Scope.SendMessageToServer<ElementHandleChannel>(
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
            => Scope.SendMessageToServer<ElementHandleChannel>(
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
