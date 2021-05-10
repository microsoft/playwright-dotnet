using System;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class ArtifactChannel : Channel<Artifact>
    {
        public ArtifactChannel(string guid, Connection connection, Artifact owner) : base(guid, connection, owner)
        {
        }

        internal async Task<string> PathAfterFinishedAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "pathAfterFinished", null).ConfigureAwait(false))?.GetProperty("value").ToString();

        internal Task SaveAsAsync(string path) => Connection.SendMessageToServerAsync(Guid, "saveAs", new { path });

        internal async Task<string> GetFailureAsync()
            => (await Connection.SendMessageToServerAsync(Guid, "failure", treatErrorPropertyAsError: false).ConfigureAwait(false))?.GetProperty("error").ToString();

        internal Task DeleteAsync() => Connection.SendMessageToServerAsync(Guid, "delete", null);

        internal Task<ArtifactStreamResult> GetStreamAsync() => Connection.SendMessageToServerAsync<ArtifactStreamResult>(Guid, "stream", null);
    }
}
