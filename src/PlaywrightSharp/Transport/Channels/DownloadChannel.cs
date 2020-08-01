using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class DownloadChannel : Channel<Download>
    {
        public DownloadChannel(string guid, ConnectionScope scope, Download owner) : base(guid, scope, owner)
        {
        }

        internal Task<string> GetPathAsync() => Scope.SendMessageToServer<string>(Guid, "path", null);

        internal Task<string> GetFailureAsync() => Scope.SendMessageToServer<string>(Guid, "failure", null);

        internal Task DeleteAsync() => Scope.SendMessageToServer(Guid, "delete", null);
    }
}
