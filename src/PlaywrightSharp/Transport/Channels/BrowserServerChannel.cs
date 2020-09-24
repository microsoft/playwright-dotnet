using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BrowserServerChannel : Channel<BrowserServer>
    {
        public BrowserServerChannel(string guid, Connection connection, BrowserServer owner) : base(guid, connection, owner)
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

        internal Task CloseAsync()
            => Connection.SendMessageToServer(
                Guid,
                "close",
                null);

        internal Task KillAsync()
            => Connection.SendMessageToServer(
                Guid,
                "kill",
                null);
    }
}
