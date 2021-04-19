using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <summary>
    /// Chromium-specific features including Tracing, service worker support, etc.
    /// You can use chromiumBrowser.StartTracingAsync and chromiumBrowser.StopTracingAsync to create a trace file which can be opened in Chrome DevTools or timeline viewer.
    /// </summary>
    public interface IChromiumBrowser : IBrowser
    {
        /// <inheritdoc cref="IBrowser.Contexts"/>
        new IChromiumBrowserContext[] Contexts { get; }

        /// <summary>
        /// Starts tracing.
        /// </summary>
        /// <param name="page">Optional, if specified, tracing includes screenshots of the given page.</param>
        /// <param name="screenshots">Gets or sets a value indicating whether Tracing should captures screenshots in the trace.</param>
        /// <param name="path">A path to write the trace file to.</param>
        /// <param name="categories">Specify custom categories to use instead of default.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task StartTracingAsync(IPage page = null, bool screenshots = false, string path = null, IEnumerable<string> categories = null);

        /// <summary>
        /// Stops tracing.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser, yielding the tracing result.</returns>
        Task<string> StopTracingAsync();

        /// <summary>
        /// Creates a new browser session.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser session was created, yielding the new session.</returns>
        Task<ICDPSession> NewBrowserCDPSessionAsync();
    }
}
