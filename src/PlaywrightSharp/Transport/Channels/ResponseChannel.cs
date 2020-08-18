using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class ResponseChannel : Channel<Response>
    {
        public ResponseChannel(string guid, ConnectionScope scope, Response owner) : base(guid, scope, owner)
        {
        }

        internal Task<string> GetBodyAsync() => Scope.SendMessageToServer<string>(Guid, "body", null);

        internal Task FinishedAsync() => Scope.SendMessageToServer(Guid, "finished", null);
    }
}
