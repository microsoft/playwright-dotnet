using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// A Browser is created when Playwright connects to a browser instance
    /// TODO: Complete when we have more APIs to refer to
    /// </summary>
    public interface IBrowser
    {
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
        /// Creates a new browser context. It won't share cookies/cache with other browser contexts.
        /// </summary>
        /// <param name="options">Context options</param>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// // Create a new incognito browser context.
        /// const context = await browser.NewContextAsync();
        /// // Create a new page in a pristine context.
        /// const page = await context.NewPageAsync("https://example.com");
        /// ]]>
        /// </code>
        /// </example>
        /// <returns>A <see cref="Task{IBrowserContext}"/> that completes when a new <see cref="IBrowserContext"/> is created</returns>
        Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null);
    }
}
