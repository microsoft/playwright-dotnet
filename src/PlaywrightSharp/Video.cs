using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal class Video : IVideo
    {
        private readonly Page _page;
        private readonly TaskCompletionSource<string> _pathTask = new TaskCompletionSource<string>();

        public Video(Page page) => _page = page;

        /// <inheritdoc/>
        public Task<string> GetPathAsync() => _pathTask.Task;

        internal void SetRelativePath(string relativePath)
            => _pathTask.TrySetResult(Path.Combine(_page.BrowserContext.Options.RecordVideo.Dir, relativePath));
    }
}
