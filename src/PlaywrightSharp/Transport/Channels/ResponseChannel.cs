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
            => (await Connection.SendMessageToServer(Guid, "body", null).ConfigureAwait(false))?.GetProperty("binary").ToString();

        internal Task FinishedAsync() => Connection.SendMessageToServer(Guid, "finished", null);
    }
}
