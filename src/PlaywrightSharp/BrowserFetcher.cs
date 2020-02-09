using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserFetcher"/>
    public class BrowserFetcher : IBrowserFetcher
    {
        private string _downloadsFolder;
        private Platform _platform;
        private string _preferredRevision;
        private Func<Platform, string, BrowserFetcherConfig> _params;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserFetcher"/> class.
        /// </summary>
        /// <param name="downloadsFolder">Folder to use.</param>
        /// <param name="platform">Current platform (some browsers are more or less specific).</param>
        /// <param name="preferredRevision">Revision to download.</param>
        /// <param name="paramsGetter">Function use to return argumens based on a platform and a revision.</param>
        public BrowserFetcher(string downloadsFolder, Platform platform, string preferredRevision, Func<Platform, string, BrowserFetcherConfig> paramsGetter)
        {
            _downloadsFolder = downloadsFolder;
            _platform = platform;
            _preferredRevision = preferredRevision;
            _params = paramsGetter;
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public Task<bool> CanDownloadAsync(int revision)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public Task<RevisionInfo> DownloadAsync(int revision)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public IEnumerable<int> GetLocalRevisions()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public RevisionInfo GetRevisionInfo(int revision)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserFetcher"/>
        public void Remove(int revision)
        {
            throw new NotImplementedException();
        }
    }
}
