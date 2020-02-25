using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserContext"/>
    public class BrowserContext : IBrowserContext
    {
        private readonly IBrowserContextDelegate _browserContextDelegate;

        internal BrowserContext(IBrowserContextDelegate browserContextDelegate)
        {
            _browserContextDelegate = browserContextDelegate;
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public BrowserContextOptions Options { get; }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task ClearCookiesAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task CloseAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task<NetworkCookie[]> GetCookiesAsync(params string[] urls)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task<IPage[]> GetPagesAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public async Task<IPage> NewPageAsync(string url = null)
        {
            var page = await _browserContextDelegate.NewPage().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(url))
            {
                await page.GoToAsync(url).ConfigureAwait(false);
            }

            return page;
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task SetGeolocationAsync(GeolocationOption geolocation)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public Task SetPermissionsAsync(string url, params ContextPermission[] permissions)
        {
            throw new System.NotImplementedException();
        }
    }
}