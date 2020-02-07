using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserType provides methods to launch a specific browser instance or connect to an existing one.
    /// </summary>
    public interface IBrowserType
    {
        /// <summary>
        /// Returns a list of devices to be used with <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/>.
        /// </summary>
        IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices { get; }

        /// <summary>
        /// A path where PlaywrightSharp expects to find a bundled browser.
        /// </summary>
        string ExecutablePath { get; set; }

        /// <summary>
        /// Creates the browser fetcher.
        /// </summary>
        /// <param name="options">Options.</param>
        /// <returns>The browser fetcher.</returns>
        IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options);

        /// <summary>
        /// Launches a new browser app.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser app.</returns>
        Task<IBrowserApp> LaunchBrowserAppAsync(LaunchOptions options = null);

        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<IBrowser> LaunchAsync(LaunchOptions options = null);

        /// <summary>
        /// The default flags that browser will be launched with.
        /// </summary>
        /// <param name="options">Set of configurable options to set on the browser.</param>
        /// <returns>Command arguments to be sent to the browser.</returns>
        string[] GetDefaultArgs(BrowserArgOptions options = null);

        /// <summary>
        /// This methods attaches PlaywrightSharp to an existing browser instance.
        /// </summary>
        /// <param name="options">Connect options.</param>
        /// <returns>A <see cref="Task"/> that completes when connection is ready, yielding the browser.</returns>
        Task<IBrowser> ConnectAsync(ConnectOptions options = null);
    }
}
