using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Converters;

namespace PlaywrightSharp.Transport.Channels
{
    internal class CDPSessionChannel : Channel<CDPSession>
    {
        public CDPSessionChannel(string guid, ConnectionScope scope, CDPSession owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler<CDPEventArgs> CDPEvent;

        internal event EventHandler Disconnected;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "disconnected":
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    break;
                case "event":
                    CDPEvent?.Invoke(this, serverParams?.ToObject<CDPEventArgs>(Scope.Connection.GetDefaultJsonSerializerOptions()));
                    break;
            }
        }

        internal Task<JsonElement?> SendAsync(string method, object args)
            => Scope.SendMessageToServer<JsonElement?>(
                Guid,
                "send",
                new Dictionary<string, object>
                {
                    ["method"] = method,
                    ["params"] = args,
                });

        internal Task DetachAsync() => Scope.SendMessageToServer(Guid, "detach", null);
    }
}
