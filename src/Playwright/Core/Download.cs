using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core
{
    /// <summary>
    /// Download objects are dispatched by page via the <see cref="IPage.Download"/> event.
    /// All the downloaded files belonging to the browser context are deleted when the browser context is closed.All downloaded files are deleted when the browser closes.
    /// Download event is emitted once the download starts.
    /// </summary>
    internal class Download : IDownload
    {
        private readonly Artifact _artifact;

        internal Download(IPage page, string url, string suggestedFilename, Artifact artifact)
        {
            Page = page;
            Url = url;
            SuggestedFilename = suggestedFilename;
            _artifact = artifact;
        }

        public IPage Page { get; }

        public string Url { get; }

        public string SuggestedFilename { get; }

        public Task<string> PathAsync() => _artifact.PathAfterFinishedAsync();

        public Task<string> FailureAsync() => _artifact.FailureAsync();

        public Task DeleteAsync() => _artifact.DeleteAsync();

        public Task SaveAsAsync(string path) => _artifact.SaveAsAsync(path);

        public Task<Stream> CreateReadStreamAsync() => _artifact.CreateReadStreamAsync();
    }
}
