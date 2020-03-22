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
        BrowserContext BrowserContext { get; set; }

        /// <summary>
        /// Creates a new page in the context.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the new page is created, yielding the <see cref="global::PlaywrightSharp.IPage"/>.</returns>
        Task<IPage> NewPage();

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
        /// Sets the page's geolocation.
        /// </summary>
        /// <param name="geolocation">Geolocation.</param>
        /// <returns>A <see cref="Task"/> that completes when the message was confirmed by the browser.</returns>
        Task SetGeolocationAsync(GeolocationOption geolocation);
    }
}
