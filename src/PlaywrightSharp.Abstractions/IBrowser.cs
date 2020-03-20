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
    public interface IBrowser : IDisposable
#if NETSTANDARD2_1
#pragma warning disable SA1001 // Space after comma
, IAsyncDisposable
#pragma warning restore SA1001 // Space after comma
#endif
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
        /// - <see cref="DisconnectAsync"/> method was called
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Returns an array of all open browser contexts.
        /// In a newly created browser, this will return a single instance of <seealso cref="IBrowserContext"/>.
        /// </summary>
        IBrowserContext[] BrowserContexts { get; }

        /// <summary>
        /// Returns the default browser context.
        /// </summary>
        /// <remarks>
        /// The default browser context can not be closed.
        /// </remarks>
        IBrowserContext DefaultContext { get; }

        /// <summary>
        /// Indicates that the browser is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Starts tracing.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        /// <param name="options">Tracing options.</param>
        Task StartTracingAsync(TracingOptions options = null);

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
        /// Disconnects Browser from the browser application, but leaves the application process running.
        /// After calling disconnect, the Browser object is considered disposed and cannot be used anymore.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the browser was disconnected.</returns>
        Task DisconnectAsync();

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
        /// This searches for a target in this specific browser context.
        /// <example>
        /// <code>
        /// <![CDATA[
        /// await page.EvaluateAsync("() => window.open('https://www.example.com/')");
        /// var newWindowTarget = await browserContext.WaitForTargetAsync((target) => target.Url == "https://www.example.com/");
        /// ]]>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="predicate">A function to be run for every target.</param>
        /// <param name="options">options.</param>
        /// <returns>Resolves to the first target found that matches the predicate function.</returns>
        Task<ITarget> WaitForTargetAsync(Func<ITarget, bool> predicate, WaitForOptions options = null);
    }
}
