using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <summary>
    /// Chromium-specific features including background pages, service worker support, etc.
    /// </summary>
    public interface IChromiumBrowserContext : IBrowserContext
    {
        /// <summary>
        /// Raised when new background page is created in the context. Chromium only
        /// </summary>
        event EventHandler<PageEventArgs> BackgroundPage;

        /// <summary>
        /// Raised when new service worker is created in the context. Chromium only
        /// </summary>
        event EventHandler<WorkerEventArgs> ServiceWorker;

        /// <summary>
        /// All existing background pages in the context. Chromium Only.
        /// </summary>
        IPage[] BackgroundPages { get; }

        /// <summary>
        /// All existing service workers in the context. Chromium Only.
        /// </summary>
        IWorker[] ServiceWorkers { get; }

        /// <summary>
        /// Creates a new browser session.
        /// </summary>
        /// <param name="page">Page to create a new session for.</param>
        /// <returns>A <see cref="Task"/> that completes when the browser session was created, yielding the new session.</returns>
        Task<ICDPSession> NewCDPSessionAsync(IPage page);
    }
}
