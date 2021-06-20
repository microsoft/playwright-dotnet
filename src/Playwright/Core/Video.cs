using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal class Video : IVideo
    {
        private readonly TaskCompletionSource<Artifact> _artifactTcs = new();

        public Video(Page page)
        {
            page.Close += (_, _) => _artifactTcs.TrySetCanceled();
            page.Crash += (_, _) => _artifactTcs.TrySetCanceled();
        }

        public async Task DeleteAsync()
        {
            var artifact = await _artifactTcs.Task.ConfigureAwait(false);
            await artifact.DeleteAsync().ConfigureAwait(false);
        }

        public async Task<string> PathAsync()
        {
            var artifact = await _artifactTcs.Task.ConfigureAwait(false);
            return artifact.AbsolutePath;
        }

        public async Task SaveAsAsync(string path)
        {
            var artifact = await _artifactTcs.Task.ConfigureAwait(false);
            await artifact.SaveAsAsync(path).ConfigureAwait(false);
        }

        internal void ArtifactReady(Artifact artifact) => _artifactTcs.TrySetResult(artifact);
    }
}
