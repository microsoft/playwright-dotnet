using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Coverage gathers information about parts of JavaScript and CSS that were used by the page.
    /// </summary>
    /// <seealso cref="IPage.Coverage"/>
    public interface ICoverage
    {
        /// <summary>
        /// Starts the JS coverage.
        /// </summary>
        /// <param name="options">Coverage options.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task StartJSCoverageAsync(CoverageStartOptions options = null);

        /// <summary>
        /// Stops the JS coverage.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser, yielding the coverage result.</returns>
        Task<CoverageEntry[]> StopJSCoverageAsync();

        /// <summary>
        /// Starts CSS coverage.
        /// </summary>
        /// <param name="options">Set of configurable options for coverage.</param>
        /// <returns>A task that resolves when coverage is started.</returns>
        Task StartCSSCoverageAsync(CoverageStartOptions options = null);

        /// <summary>
        /// Stops JS coverage and returns coverage reports for all non-anonymous scripts.
        /// </summary>
        /// <returns>Task that resolves to the array of coverage reports for all stylesheets.</returns>
        /// <remarks>
        /// JavaScript Coverage doesn't include anonymous scripts; however, scripts with sourceURLs are reported.
        /// </remarks>
        public Task<CoverageEntry[]> StopCSSCoverageAsync();
    }
}
