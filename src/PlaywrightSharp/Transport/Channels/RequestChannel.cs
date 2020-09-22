using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RequestChannel : Channel<Request>
    {
        public RequestChannel(string guid, Connection connection, Request owner) : base(guid, connection, owner)
        {
        }

        internal Task<ResponseChannel> GetResponseAsync() => Connection.SendMessageToServer<ResponseChannel>(Guid, "response", null);
    }
}
