using System;
using System.Collections.Generic;
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

        internal Task<BrowserContextChannel> NewContextAsync(BrowserContextOptions options)
            => Scope.SendMessageToServer<BrowserContextChannel>(
                Guid,
                "newContext",
                new Dictionary<string, object>
                {
                    ["options"] = options ?? new BrowserContextOptions(),
                });
    }
}
