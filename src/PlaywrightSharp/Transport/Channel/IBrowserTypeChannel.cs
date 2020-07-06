using System.Threading.Tasks;

namespace PlaywrightSharp.Transport.Channel
{
    /// <summary>
    /// <see cref="IBrowserType"/> channel.
    /// </summary>
    internal interface IBrowserTypeChannel
    {
        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<BrowserChannel> LaunchAsync(LaunchOptions options);
    }
}
