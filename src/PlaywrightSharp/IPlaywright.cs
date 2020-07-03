using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// IPlaywright provides methods to interact with the playwright server.
    /// </summary>
    public interface IPlaywright
    {
        /// <summary>
        /// Gets the Chromium browser type from the playwright server.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright server confirmed that the chromium <see cref="IBrowserType"/> is available.</returns>
        Task<IBrowserType> GetChromiumBrowserAsync();

        /// <summary>
        /// Gets the Firefox browser type from the playwright server.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright server confirmed that the firefox <see cref="IBrowserType"/> is available.</returns>
        Task<IBrowserType> GetFirefoxBrowserAsync();

        /// <summary>
        /// Gets the Webkit browser type from the playwright server.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the playwright server confirmed that the Webkit <see cref="IBrowserType"/> is available.</returns>
        Task<IBrowserType> GetWebkitTypeAsync();
    }
}
