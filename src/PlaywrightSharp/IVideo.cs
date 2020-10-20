using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// When browser context is created with the videosPath option, each page has a video object associated with it.
    /// </summary>
    public interface IVideo
    {
        /// <summary>
        /// Returns the file system path this video will be recorded to.
        /// The video is guaranteed to be written to the filesystem upon closing the browser context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the path has been resolved.</returns>
        Task<string> GetPathAsync();
    }
}
