using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class DownloadChannel : Channel<Download>
    {
        public DownloadChannel(string guid, ConnectionScope scope, Download owner) : base(guid, scope, owner)
        {
        }

        internal async Task<string> GetPathAsync()
            => (await Scope.SendMessageToServer(Guid, "path", null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> GetFailureAsync()
            => (await Scope.SendMessageToServer(Guid, "failure", treatErrorPropertyAsError: false).ConfigureAwait(false))?.GetProperty("error").ToString();

        internal Task DeleteAsync() => Scope.SendMessageToServer(Guid, "delete", null);
    }
}
