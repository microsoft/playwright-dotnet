using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserFetcher can download and manage different versions of a browser.
    /// </summary>
    public interface IBrowserFetcher
    {
        /// <summary>
        /// Occurs when download progress in <see cref="DownloadAsync(string)"/> changes.
        /// </summary>
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        /// <summary>
        /// Gets the revision info.
        /// </summary>
        /// <returns>Revision info.</returns>
        /// <param name="revision">A revision to get info for.</param>
        RevisionInfo GetRevisionInfo(string revision);

        /// <summary>
        /// The method initiates a HEAD request to check if the revision is available.
        /// </summary>
        /// <param name="revision">A revision to check availability.</param>
        /// <returns>A <see cref="Task"/> that completes when the evaluation finished, yielding whether the version is available or not.</returns>
        Task<bool> CanDownloadAsync(string revision);

        /// <summary>
        /// Downloads the revision.
        /// </summary>
        /// <param name="revision">Revision.</param>
        /// <returns>A <see cref="Task"/> that completes when the download completes, yielding the information of the revision downloaded.</returns>
        Task<RevisionInfo> DownloadAsync(string revision);

        /// <summary>
        /// A list of all revisions available locally on disk.
        /// </summary>
        /// <returns>The available revisions.</returns>
        IEnumerable<string> GetLocalRevisions();

        /// <summary>
        /// Removes a downloaded revision.
        /// </summary>
        /// <param name="revision">Revision to remove.</param>
        void Remove(string revision);
    }
}
