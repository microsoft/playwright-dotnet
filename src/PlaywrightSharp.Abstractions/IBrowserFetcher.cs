using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserFetcher can download and manage different versions of a browser.
    /// </summary>
    public interface IBrowserFetcher
    {
        /// <summary>
        /// Gets the revision info.
        /// </summary>
        /// <returns>Revision info.</returns>
        /// <param name="revision">A revision to get info for.</param>
        RevisionInfo GetRevisionInfo(int revision);

        /// <summary>
        /// The method initiates a HEAD request to check if the revision is available.
        /// </summary>
        /// <param name="revision">A revision to check availability.</param>
        /// <returns>A <see cref="Task"/> that completes when the evaluation finished, yielding whether the version is available or not.</returns>
        Task<bool> CanDownloadAsync(int revision);

        /// <summary>
        /// Downloads the revision.
        /// </summary>
        /// <param name="revision">Revision.</param>
        /// <returns>A <see cref="Task"/> that completes when the download completes, yielding the information of the revision downloaded.</returns>
        Task<RevisionInfo> DownloadAsync(int revision);

        /// <summary>
        /// A list of all revisions available locally on disk.
        /// </summary>
        /// <returns>The available revisions.</returns>
        IEnumerable<int> GetLocalRevisions();

        /// <summary>
        /// Removes a downloaded revision.
        /// </summary>
        /// <param name="revision">Revision to remove.</param>
        void Remove(int revision);
    }
}