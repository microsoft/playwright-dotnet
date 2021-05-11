using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public partial interface IBrowserType
    {
        /// <summary>
        /// Launches a new browser.
        /// </summary>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser.</returns>
        Task<IBrowser> LaunchAsync(LaunchOptions options);

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchOptions options);

        /// <summary>
        /// Launches browser that uses persistent storage located at userDataDir and returns the only context. Closing this context will automatically close the browser.
        /// </summary>
        /// <param name="userDataDir">Path to a User Data Directory, which stores browser session data like cookies and local storage.</param>
        /// <param name="options">Launch options.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser is launched, yielding the browser server.</returns>
        Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options);
    }
}
