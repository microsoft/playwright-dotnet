using System.Linq;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContext"/>
    public class BrowserContext : IBrowserContext
    {
        private readonly IBrowserContextDelegate _delegate;
        private bool _closed;

        internal BrowserContext(IBrowserContextDelegate browserContextDelegate, BrowserContextOptions options = null)
        {
            _delegate = browserContextDelegate;
            _delegate.BrowserContext = this;
            Options = options?.Clone() ?? new BrowserContextOptions();
        }

        /// <inheritdoc cref="IBrowserContext.Options"/>
        public BrowserContextOptions Options { get; }

        /// <inheritdoc cref="IBrowserContext.ClearCookiesAsync"/>
        public Task ClearCookiesAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext.CloseAsync"/>
        public async Task CloseAsync()
        {
            if (_closed)
            {
                return;
            }

            await _delegate.CloseAsync().ConfigureAwait(false);
            _closed = true;
        }

        /// <inheritdoc cref="IBrowserContext.GetCookiesAsync(string[])"/>
        public Task<NetworkCookie[]> GetCookiesAsync(params string[] urls)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext.GetPagesAsync"/>
        public Task<IPage[]> GetPagesAsync() => _delegate.GetPagesAsync();

        /// <inheritdoc cref="IBrowserContext.NewPageAsync(string)"/>
        public async Task<IPage> NewPageAsync(string url = null)
        {
            var page = await _delegate.NewPage().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(url))
            {
                await page.GoToAsync(url).ConfigureAwait(false);
            }

            return page;
        }

        /// <inheritdoc cref="IBrowserContext.SetCookiesAsync(SetNetworkCookieParam[])"/>
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContext.SetGeolocationAsync(GeolocationOption)"/>
        public Task SetGeolocationAsync(GeolocationOption geolocation)
            => _delegate.SetGeolocationAsync(geolocation);

        /// <inheritdoc cref="IBrowserContext.ClearPermissionsAsync"/>
        public Task ClearPermissionsAsync() => throw new System.NotImplementedException();

        /// <inheritdoc cref="IBrowserContext.SetPermissionsAsync(string, ContextPermission[])"/>
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions)
            => _delegate.SetPermissionsAsync(origin, permissions);

        internal async Task InitializeAsync()
        {
            if (Options?.Permissions?.Count > 0)
            {
                await Task.WhenAll(Options.Permissions.Select(permission => SetPermissionsAsync(permission.Key, permission.Value)).ToArray()).ConfigureAwait(false);
            }

            if (Options?.Geolocation != null)
            {
                await SetGeolocationAsync(Options.Geolocation).ConfigureAwait(false);
            }
        }
    }
}
