using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// A Browser is created when Playwright connects to a browser instance.
    /// </summary>
    public interface IBrowser : IAsyncDisposable
    {
        /// <summary>
        /// Raised when the url of a target changes
        /// </summary>
        public event EventHandler<TargetChangedArgs> TargetChanged;

        /// <summary>
        /// Raised when a target is created, for example when a new page is opened by <c>window.open</c> <see href="https://developer.mozilla.org/en-US/docs/Web/API/Window/open"/> or <see cref="IBrowserContext.NewPageAsync"/>.
        /// </summary>
        public event EventHandler<TargetChangedArgs> TargetCreated;

        /// <summary>
        /// Raised when a target is destroyed, for example when a page is closed
        /// </summary>
        public event EventHandler<TargetChangedArgs> TargetDestroyed;

        /// <summary>
        /// Raised when the <see cref="IBrowser"/> gets disconnected from the browser instance.
        /// This might happen because one of the following:
        /// - Browser is closed or crashed
        /// - <see cref="CloseAsync"/> method was called
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Returns an array of all open browser contexts.
        /// In a newly created browser, this will return a single instance of <seealso cref="IBrowserContext"/>.
        /// </summary>
        IEnumerable<IBrowserContext> BrowserContexts { get; }

        /// <summary>
        /// Indicates that the browser is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns an array of all open browser contexts. In a newly created browser, this will return zero browser contexts.
        /// </summary>
        IBrowserContext[] Contexts { get; }

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
        /// Closes browser and all of its pages (if any were opened).
        /// The Browser object itself is considered to be disposed and cannot be used anymore.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser is closed.</returns>
        Task CloseAsync();

        /// <summary>
        /// Gets the <see cref="IPage"/> <see cref="ITarget"/>.
        /// </summary>
        /// <param name="page">Page to evaluate.</param>
        /// <returns><see cref="IPage"/> main target.</returns>
        ITarget GetPageTarget(IPage page);

        /// <summary>
        /// Creates a new browser context. It won't share cookies/cache with other browser contexts.
        /// </summary>
        /// <param name="options">Context options.</param>
        /// <example>.
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IBrowserContext}"/> that completes when a new <see cref="IBrowserContext"/> is created.</returns>
        Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null);

        /// <summary>
        /// Creates a new page in a new browser context. Closing this page will close the context as well.
        /// </summary>
        /// <param name="options">Context options.</param>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created.</returns>
        Task<IPage> NewPageAsync(BrowserContextOptions options = null);

        /// <summary>
        /// Creates a new browser session.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser session was created, yielding the new session.</returns>
        Task<ICDPSession> NewBrowserCDPSessionAsync();
    }
}
