using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RequestChannel : Channel<Request>
    {
        public RequestChannel(string guid, Connection connection, Request owner) : base(guid, connection, owner)
        {
        }

        internal Task<ResponseChannel> GetResponseAsync() => Connection.SendMessageToServerAsync<ResponseChannel>(Guid, "response", null);
    }
}
