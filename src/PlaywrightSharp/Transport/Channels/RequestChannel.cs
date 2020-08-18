using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RequestChannel : Channel<Request>
    {
        public RequestChannel(string guid, ConnectionScope scope, Request owner) : base(guid, scope, owner)
        {
        }

        internal Task<ResponseChannel> GetResponseAsync() => Scope.SendMessageToServer<ResponseChannel>(Guid, "response", null);
    }
}
