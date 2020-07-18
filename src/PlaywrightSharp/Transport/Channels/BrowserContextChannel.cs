using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextChannel : Channel<BrowserContext>
    {
        public BrowserContextChannel(string guid, ConnectionScope scope, BrowserContext owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal event EventHandler<BrowserContextOnPageEventArgs> OnPage;

        internal Task<PageChannel> NewPageAsync(string url)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "newPage",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                });

        internal Task CloseAsync() => Scope.SendMessageToServer(Guid, "close");

        internal Task SetDefaultTimeoutNoReplyAsync(int timeout)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
                case "page":
                    OnPage?.Invoke(
                        this,
                        new BrowserContextOnPageEventArgs { PageChannel = serverParams?.ToObject<PageChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()) });
                    break;
            }
        }
    }
}
