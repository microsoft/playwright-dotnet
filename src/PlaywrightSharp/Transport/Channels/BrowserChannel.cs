using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserChannel : Channel<Browser>
    {
        public BrowserChannel(string guid, ConnectionScope scope, Browser owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        internal Task<BrowserContextChannel> NewContextAsync(BrowserContextOptions options)
            => Scope.SendMessageToServer<BrowserContextChannel>(
                Guid,
                "newContext",
                (options ?? new BrowserContextOptions()).ToChannelDictionary());

        internal Task CloseAsync()
            => Scope.SendMessageToServer<BrowserContextChannel>(
                Guid,
                "close",
                null);

        internal Task<CDPSessionChannel> NewBrowserCDPSessionAsync()
            => Scope.SendMessageToServer<CDPSessionChannel>(Guid, "crNewBrowserCDPSession", null);
    }
}
