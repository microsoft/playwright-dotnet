using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserChannel : Channel<Browser>
    {
        public BrowserChannel(string guid, Connection connection, Browser owner) : base(guid, connection, owner)
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
            => Connection.SendMessageToServer<BrowserContextChannel>(
                Guid,
                "newContext",
                options.ToChannelDictionary(),
                true);

        internal Task CloseAsync() => Connection.SendMessageToServer<BrowserContextChannel>(Guid, "close", null);

        internal Task<CDPSessionChannel> NewBrowserCDPSessionAsync()
            => Connection.SendMessageToServer<CDPSessionChannel>(Guid, "crNewBrowserCDPSession", null);

        internal Task StartTracingAsync(IPage page, bool screenshots, string path, IEnumerable<string> categories)
        {
            var args = new Dictionary<string, object>
            {
                ["screenshots"] = screenshots,
            };

            if (path != null)
            {
                args["path"] = path;
            }

            if (page != null)
            {
                args["page"] = page;
            }

            if (categories != null)
            {
                args["categories"] = categories;
            }

            return Connection.SendMessageToServer(Guid, "crStartTracing", args);
        }

        internal async Task<string> StopTracingAsync()
            => (await Connection.SendMessageToServer(Guid, "crStopTracing", null).ConfigureAwait(false))?.GetProperty("binary").ToString();
    }
}
