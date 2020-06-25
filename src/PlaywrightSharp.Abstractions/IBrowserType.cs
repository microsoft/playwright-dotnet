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
        /// Executable path based on <see cref="IBrowserFetcher"/>.
        /// </summary>
        string ExecutablePath { get; }

        /// <summary>
        /// Returns browser name.
        /// </summary>
        Browser Name { get; }

        /// <summary>
        /// Creates the browser fetcher.
        /// </summary>
        /// <param name="options">Options.</param>
        /// <returns>The browser fetcher.</returns>
        IBrowserFetcher CreateBrowserFetcher(BrowserFetcherOptions options = null);

        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<IBrowser> LaunchAsync(LaunchOptions options = null);

        /// <summary>
        /// Launches browser that uses persistent storage located at <paramref name="userDataDir"/> and returns the only context.
        /// Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">
        /// Path to a User Data Directory, which stores browser session data like cookies and local storage.
        /// More details for <see href="https://chromium.googlesource.com/chromium/src/+/master/docs/user_data_dir.md">Chromium</see> and <see href="https://developer.mozilla.org/en-US/docs/Mozilla/Command_Line_Options#User_Profile">Firefox</see>.
        /// </param>
        /// <param name="options">Options</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the context.</returns>
        public Task<IBrowser> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options = null);

        /// <summary>
        /// Launches browser server that client can connect to.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the context.</returns>
        public Task<IBrowserServer> LaunchServerAsync(LaunchServerOptions options = null);

        /// <summary>
        /// This methods attaches PlaywrightSharp to an existing browser instance.
        /// </summary>
        /// <param name="options">Connect options.</param>
        /// <returns>A <see cref="Task"/> that completes when connection is ready, yielding the browser.</returns>
        Task<IBrowser> ConnectAsync(ConnectOptions options = null);
    }
}
