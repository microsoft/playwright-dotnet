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
    }
}
