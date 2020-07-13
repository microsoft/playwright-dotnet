using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserServerChannel : Channel<BrowserServer>
    {
        public BrowserServerChannel(string guid, ConnectionScope scope, BrowserServer owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal override void OnMessage(string method, PlaywrightSharpServerParams serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        internal Task CloseAsync()
            => Scope.SendMessageToServer(
                Guid,
                "close",
                null);
    }
}
