using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class ResponseChannel : Channel<Response>
    {
        public ResponseChannel(string guid, ConnectionScope scope, Response owner) : base(guid, scope, owner)
        {
        }

        internal async Task<string> GetBodyAsync()
            => (await Scope.SendMessageToServer(Guid, "body", null).ConfigureAwait(false))?.GetProperty("binary").ToString();

        internal Task FinishedAsync() => Scope.SendMessageToServer(Guid, "finished", null);
    }
}
