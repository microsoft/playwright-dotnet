using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserContextChannel : Channel<BrowserContext>
    {
        public BrowserContextChannel(string guid, ConnectionScope scope, BrowserContext owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal Task<PageChannel> NewPageAsync(string url)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "newPage",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                });

        internal override void OnMessage(string method, PlaywrightSharpServerParams serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
