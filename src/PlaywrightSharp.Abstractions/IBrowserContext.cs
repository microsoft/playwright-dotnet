using System;
using System.Collections;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// BrowserContexts provide a way to operate multiple independent browser sessions.
    /// If a <see cref="IPage"/> opens another page, e.g.with a window.open call, the popup will belong to the parent page's browser context.
    /// PlaywrightSharp allows creation of "incognito" browser contexts with <seealso cref="IBrowser.NewContextAsync"/> method. "Incognito" browser contexts don't write any browsing data to disk.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// // Create a new incognito browser context
    /// const context = await browser.NewContextAsync();
    /// // Create a new page inside context.
    /// const page = await context.NewPageAsync("https://example.com");
    /// // Dispose context once it's no longer needed.
    /// await context.CloseAsync();
    /// ]]>
    /// </code>
    /// </example>
    public interface IBrowserContext
    {
        /// <summary>
        /// Creates a new page in the browser context and optionally navigates it to the specified URL.
        /// </summary>
        /// <returns>A <see cref="Task{IPage}"/> that completes when a new <see cref="IPage"/> is created</returns>, yielding the new <see cref="IPage"/>.
        Task<IPage> NewPageAsync();
        /// <summary>
        /// Closes the browser context. All the targets that belong to the browser context will be closed.
        /// </summary>
        /// <remarks>NOTE only incognito browser contexts can be closed.</remarks>
        /// <returns>A <see cref="Task"/> that completes when the browser context is closed</returns>
        Task CloseAsync();

        /// <summary>
        /// An array of all pages inside the browser context.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that completes when get browser context got all the pages, yielding the pages inside that browser context.
        /// </returns>
        Task<IPage[]> GetPagesAsync();

        /// <summary>
        /// Returns the context's cookies
        /// </summary>
        /// <param name="urls">Url's to return cookies for</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are sent by the browser, yielding a <see cref="t:NetworkCookie[]"/></returns>
        /// <remarks>
        /// If no URLs are specified, this method returns cookies for the current page URL.
        /// If URLs are specified, only cookies for those URLs are returned.
        /// </remarks>
        Task<SetNetworkCookieParam[]> GetCookiesAsync(params string[] urls);

        /// <summary>
        /// Clears all of the current cookies and then sets the cookies for the context
        /// </summary>
        /// <param name="cookies">Cookies to set</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are set</returns>
        Task SetCookiesAsync(params SetNetworkCookieParam[] cookies);

        /// <summary>
        /// Deletes cookies from the context
        /// </summary>
        /// <param name="cookies">Cookies to delete</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are deleted.</returns>
        Task DeleteCookiesAsync(params SetNetworkCookieParam[] cookies);
    }
}
