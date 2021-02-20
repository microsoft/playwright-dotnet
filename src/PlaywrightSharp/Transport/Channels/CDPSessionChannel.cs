using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channels
{
    internal class CDPSessionChannel : Channel<CDPSession>
    {
        public CDPSessionChannel(string guid, Connection connection, CDPSession owner) : base(guid, connection, owner)
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
                    CDPEvent?.Invoke(this, serverParams?.ToObject<CDPEventArgs>(Connection.DefaultJsonSerializerOptions));
                    break;
            }
        }

        internal Task<JsonElement?> SendAsync(string method, object args)
            => Connection.SendMessageToServerAsync<JsonElement?>(
                Guid,
                "send",
                new Dictionary<string, object>
                {
                    ["method"] = method,
                    ["params"] = args,
                });

        internal Task DetachAsync() => Connection.SendMessageToServerAsync(Guid, "detach", null);
    }
}
