using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class StreamChannel : Channel<PlaywrightStream>
    {
        public StreamChannel(string guid, Connection connection, PlaywrightStream owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> ReadAsync()
            => (await Connection.SendMessageToServerAsync(
                Guid,
                "read",
                new Dictionary<string, object>
                {
                    ["size"] = 0,
                }).ConfigureAwait(false))?.GetProperty("binary").ToString();

        internal Task CloseAsync() => Connection.SendMessageToServerAsync(Guid, "close", null);
    }
}
