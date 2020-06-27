using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Browser context delegate.
    /// </summary>
    internal interface IBrowserContextDelegate
    {
        /// <summary>
        /// <see cref="IBrowserContext"/> using the delegate.
        /// </summary>
        IBrowserContext BrowserContext { get; set; }

        /// <summary>
        /// Creates a new page in the context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the new page is created, yielding the <see cref="IPage"/>.</returns>
        Task<IPage> NewPageAsync();

        /// <summary>
        /// An array of all pages inside the browser context.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that completes when get browser context got all the pages, yielding the pages inside that browser context.
        /// </returns>
        Task<IPage[]> GetPagesAsync();

        /// <summary>
        /// Grants permissions to an URL.
        /// </summary>
        /// <param name="origin">The origin to grant permissions to, e.g. "https://example.com".</param>
        /// <param name="permissions">An array of permissions to grant.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetPermissionsAsync(string origin, params ContextPermission[] permissions);

        /// <summary>
        /// Clears all permission overrides for the browser context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task ClearPermissionsAsync();

        /// <summary>
        /// Sets the page's geolocation.
        /// </summary>
        /// <param name="geolocation">Geolocation.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetGeolocationAsync(GeolocationOption geolocation);

        /// <summary>
        /// Closes the current context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task CloseAsync();

        /// <summary>
        /// Gets all the existing pages in the context.
        /// </summary>
        /// <returns>A list of pages.</returns>
        IEnumerable<IPage> GetExistingPages();

        /// <summary>
        /// Returns the context's cookies.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the cookies are sent by the browser, yielding a <see cref="t:NetworkCookie[]"/>.</returns>
        Task<IEnumerable<NetworkCookie>> GetCookiesAsync();

        /// <summary>
        /// Clears all of the current cookies and then sets the cookies for the context.
        /// </summary>
        /// <param name="cookies">Cookies to set.</param>
        /// <returns>A <see cref="Task"/> that completes when the cookies are set.</returns>
        Task SetCookiesAsync(params SetNetworkCookieParam[] cookies);

        /// <summary>
        /// Clears the context's cookies.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the cookies are cleared.</returns>
        Task ClearCookiesAsync();
    }
}
