using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class DownloadChannel : Channel<Download>
    {
        public DownloadChannel(string guid, Connection connection, Download owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> GetPathAsync()
            => (await Connection.SendMessageToServer(Guid, "path", null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> GetFailureAsync()
            => (await Connection.SendMessageToServer(Guid, "failure", treatErrorPropertyAsError: false).ConfigureAwait(false))?.GetProperty("error").ToString();

        internal Task DeleteAsync() => Connection.SendMessageToServer(Guid, "delete", null);

        internal Task SaveAsAsync(string path) => Connection.SendMessageToServer(Guid, "saveAs", new { path });
    }
}
