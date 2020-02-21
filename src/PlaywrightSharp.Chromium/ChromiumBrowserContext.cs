using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserContext"
    public class ChromiumBrowserContext : IBrowserContext
    {
        private readonly ChromiumConnection _connection;
        private readonly ChromiumBrowser _chromiumBrowser;
        private readonly string _contextId;

        internal ChromiumBrowserContext(ChromiumConnection connection, ChromiumBrowser chromiumBrowser, string contextId)
        {
            _connection = connection;
            _chromiumBrowser = chromiumBrowser;
            _contextId = contextId;
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task ClearCookiesAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task CloseAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task<NetworkCookie[]> GetCookiesAsync(params string[] urls)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task<IPage[]> GetPagesAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task<IPage> NewPageAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task SetGeolocationAsync(GeolocationOption geolocation)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext"
        public Task SetPermissionsAsync(string url, params ContextPermission[] permissions)
        {
            throw new System.NotImplementedException();
        }
    }
}