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
        private readonly TaskCompletionSource<string> _pathTask = new();

        public Video(Page page) => _page = page;

        /// <inheritdoc/>
        public Task DeleteAsync() => throw new NotImplementedException();

        /// <inheritdoc/>
        public Task<string> PathAsync() => _pathTask.Task;

        /// <inheritdoc/>
        public Task SaveAsAsync(string path) => throw new NotImplementedException();

        internal void SetRelativePath(string relativePath)
            => _pathTask.TrySetResult(Path.Combine(_page.Context.VideoPath, relativePath));
    }
}
