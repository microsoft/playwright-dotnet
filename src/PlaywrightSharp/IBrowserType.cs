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
        /// Executable path.
        /// </summary>
        string ExecutablePath { get; }

        /// <summary>
        /// Returns browser name. For example: 'chromium', 'webkit' or 'firefox'.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<IBrowser> LaunchAsync(LaunchOptions options = null);

        /// <summary>
        /// Launches browser server that client can connect to.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserServer> LaunchServerAsync(LaunchOptions options);

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistenContextAsync(string userDataDir, LaunchPersistentOptions options);

        /// <summary>
        /// This methods attaches PlaywrightSharp to an existing browser instance.
        /// </summary>
        /// <param name="options">Connect options.</param>
        /// <returns>A <see cref="Task"/> that completes when connection is ready, yielding the browser.</returns>
        Task<IBrowser> ConnectAsync(ConnectOptions options = null);
    }
}
