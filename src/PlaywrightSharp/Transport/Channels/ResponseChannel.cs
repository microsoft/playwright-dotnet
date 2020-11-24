using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class ResponseChannel : Channel<Response>
    {
        public ResponseChannel(string guid, Connection connection, Response owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> GetBodyAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "body", null).ConfigureAwait(false))?.GetProperty("binary").ToString();

        internal Task FinishedAsync() => Connection.SendMessageToServerAsync(Guid, "finished", null);
    }
}
