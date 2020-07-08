using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class FrameChannel : Channel<Frame>
    {
        public FrameChannel(string guid, ConnectionScope scope, Frame owner) : base(guid, scope, owner)
        {
        }

        internal Task<ResponseChannel> GoToAsync(string url, GoToOptions options)
            => Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "goTo",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["options"] = options,
                });
    }
}
