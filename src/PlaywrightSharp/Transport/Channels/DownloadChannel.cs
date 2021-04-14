using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channels
{
    internal class DownloadChannel : Channel<Download>
    {
        public DownloadChannel(string guid, Connection connection, Download owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> PathAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "path", null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal async Task<string> GetFailureAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "failure", treatErrorPropertyAsError: false).ConfigureAwait(false))?.GetProperty("error").ToString();

        internal Task DeleteAsync() => Connection.SendMessageToServerAsync(Guid, "delete", null);

        internal Task SaveAsAsync(string path) => Connection.SendMessageToServerAsync(Guid, "saveAs", new { path });
    }
}
