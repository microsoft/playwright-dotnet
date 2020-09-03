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
        /// <param name="wsEndpoint">A browser websocket endpoint to connect to.</param>
        /// <param name="timeout">Maximum time in milliseconds to wait for the connection to be established. Defaults to 30000 (30 seconds). Pass 0 to disable timeout.</param>
        /// <param name="slowMo">Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.</param>
        /// <returns>A <see cref="Task"/> that completes when connection is ready, yielding the browser.</returns>
        Task<IBrowser> ConnectAsync(string wsEndpoint, int? timeout = null, int? slowMo = null);
    }
}
