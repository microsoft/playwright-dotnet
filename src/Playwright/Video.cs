using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal class Video : IVideo
    {
        private readonly Page _page;
        private readonly TaskCompletionSource<string> _pathTask = new TaskCompletionSource<string>();

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
