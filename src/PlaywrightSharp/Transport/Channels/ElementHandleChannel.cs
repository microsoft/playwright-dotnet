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

        internal Task<Rect> GetBoundingBoxAsync() => Scope.SendMessageToServer<Rect>(Guid, "boundingBox", null);
    }
}
