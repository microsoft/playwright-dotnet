using System;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Messaging.Target;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IBrowserContext"/>
    public class ChromiumBrowserContext : IBrowserContext
    {
        private readonly ChromiumSession _client;
        private readonly string _contextId;

        internal ChromiumBrowserContext(ChromiumSession client, ChromiumBrowser chromiumBrowser, string contextId)
        {
            _client = client;
            Browser = chromiumBrowser;
            _contextId = contextId;
        }

        internal ChromiumBrowser Browser { get; }

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
            var page = await NewPage();

            if (!string.IsNullOrEmpty(url))
            {
                await page.GoToAsync(url);
            }

            return page;
        }

        private async Task<ChromiumPage> NewPage()
        {
            var createTargetRequest = new TargetCreateTargetRequest
            {
                Url = "about:blank"
            };

            if (_contextId != null)
            {
                createTargetRequest.BrowserContextId = _contextId;
            }

            string targetId = (await _client.SendAsync<TargetCreateTargetResponse>("Target.createTarget", createTargetRequest)
                .ConfigureAwait(false)).TargetId;
            var target = Browser.TargetsMap[targetId];
            await target.InitializedTask.ConfigureAwait(false);
            return await target.PageAsync().ConfigureAwait(false);
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