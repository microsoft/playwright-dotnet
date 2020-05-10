using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol.Browser;
using PlaywrightSharp.Firefox.Protocol.Target;

namespace PlaywrightSharp.Firefox
{
    /// <inheritdoc cref="IBrowserContextDelegate"/>
    public class FirefoxBrowserContext : IBrowserContextDelegate
    {
        private readonly string _browserContextId;
        private readonly FirefoxConnection _connection;
        private readonly BrowserContextOptions _browserContextOptions;
        private readonly FirefoxBrowser _browser;

        internal FirefoxBrowserContext(string browserContextId, FirefoxConnection connection, BrowserContextOptions browserContextOptions, FirefoxBrowser browser)
        {
            _browserContextId = browserContextId;
            _connection = connection;
            _browserContextOptions = browserContextOptions;
            _browser = browser;
        }

        /// <inheritdoc cref="IBrowserContextDelegate.BrowserContext"/>
        public BrowserContext BrowserContext { get; set; }

        /// <inheritdoc cref="IBrowserContextDelegate.GetPagesAsync"/>
        public async Task<IPage[]> GetPagesAsync()
        {
            var targets = _browser.GetAllTargets()
                .Where(target => target.BrowserContext == BrowserContext && target.Type == TargetType.Page);
            var pages = await Task.WhenAll(targets.Select(target => target.GetPageAsync())).ConfigureAwait(false);
            return pages.Where(page => page != null).ToArray();
        }

        /// <inheritdoc cref="IBrowserContextDelegate.NewPageAsync"/>
        public async Task<IPage> NewPageAsync()
        {
            var response = await _connection.SendAsync(new TargetNewPageRequest { BrowserContextId = _browserContextId }).ConfigureAwait(false);
            var target = _browser.TargetsMap[response.TargetId];
            return await target.GetPageAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IBrowserContextDelegate.SetGeolocationAsync(GeolocationOption)"/>
        public Task SetGeolocationAsync(GeolocationOption geolocation)
            => throw new NotSupportedException("Geolocation emulation is not supported in Firefox");

        /// <inheritdoc cref="IBrowserContextDelegate.CloseAsync"/>
        public async Task CloseAsync()
        {
            if (_browserContextId == null)
            {
                throw new PlaywrightSharpException("Non-incognito profiles cannot be closed");
            }

            await _connection.SendAsync(new TargetRemoveBrowserContextRequest
            {
                BrowserContextId = _browserContextId,
            }).ConfigureAwait(false);
            _browser.Contexts.Remove(_browserContextId);
        }

        /// <inheritdoc cref="IBrowserContextDelegate.GetExistingPages"/>
        public IEnumerable<IPage> GetExistingPages() => _browser.GetAllTargets()
                .Where(target => target.BrowserContext == BrowserContext && target.Page != null)
                .Select(target => target.Page);

        /// <inheritdoc cref="IBrowserContextDelegate.SetPermissionsAsync(string, ContextPermission[])"/>
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc cref="IBrowserContextDelegate.SetCookiesAsync(SetNetworkCookieParam[])"/>
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies)
            => _connection.SendAsync(new BrowserSetCookiesRequest
            {
                BrowserContextId = _browserContextId,
                Cookies = Array.ConvertAll(cookies, c => (CookieOptions)c),
            });

        /// <inheritdoc cref="IBrowserContextDelegate.GetCookiesAsync"/>
        public async Task<IEnumerable<NetworkCookie>> GetCookiesAsync()
        {
            var result = await _connection.SendAsync(new BrowserGetCookiesRequest { BrowserContextId = _browserContextId }).ConfigureAwait(false);
            return result.Cookies.Select(c => (NetworkCookie)c);
        }

        /// <inheritdoc cref="IBrowserContextDelegate.ClearCookiesAsync"/>
        public Task ClearCookiesAsync() => _connection.SendAsync(new BrowserClearCookiesRequest { BrowserContextId = _browserContextId });
    }
}
