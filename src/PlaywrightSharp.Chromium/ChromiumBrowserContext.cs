using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Chromium.Protocol.Browser;
using PlaywrightSharp.Chromium.Protocol.Emulation;
using PlaywrightSharp.Chromium.Protocol.Target;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContextDelegate"/>
    public class ChromiumBrowserContext : IBrowserContextDelegate
    {
        private static readonly IReadOnlyDictionary<ContextPermission, PermissionType> WebPermissionToProtocol = new Dictionary<ContextPermission, PermissionType>
        {
            // TODO
        };

        private readonly ChromiumSession _client;
        private readonly string _contextId;

        internal ChromiumBrowserContext(ChromiumSession client, ChromiumBrowser chromiumBrowser) : this(client, chromiumBrowser, null, null)
        {
        }

        internal ChromiumBrowserContext(
            ChromiumSession client,
            ChromiumBrowser chromiumBrowser,
            string contextId,
            BrowserContextOptions options)
        {
            _client = client;
            Browser = chromiumBrowser;
            _contextId = contextId;
            Options = options;
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public BrowserContextOptions Options { get; }

        /// <inheritdoc cref="IBrowserContextDelegate.BrowserContext"/>
        public BrowserContext BrowserContext { get; set; }

        internal ChromiumBrowser Browser { get; }

        /// <inheritdoc cref="IBrowserContextDelegate.GetPagesAsync"/>
        public async Task<IPage[]> GetPagesAsync()
        {
            var pageTasks =
                Browser.GetAllTargets()
                .Where(target => target.BrowserContext == BrowserContext && target.Type == TargetType.Page)
                .Select(t => t.PageAsync());

            var pages = await Task.WhenAll(pageTasks).ConfigureAwait(false);

            return pages.Where(p => p != null).ToArray();
        }

        /// <inheritdoc cref="IBrowserContextDelegate.NewPage"/>
        public async Task<IPage> NewPage()
        {
            var createTargetRequest = new TargetCreateTargetRequest
            {
                Url = "about:blank",
            };

            if (_contextId != null)
            {
                createTargetRequest.BrowserContextId = _contextId;
            }

            string targetId = (await _client.SendAsync(createTargetRequest)
                .ConfigureAwait(false)).TargetId;
            var target = Browser.TargetsMap[targetId];
            await target.InitializedTask.ConfigureAwait(false);
            return await target.PageAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IBrowserContextDelegate.SetGeolocationAsync(GeolocationOption)"/>
        public async Task SetGeolocationAsync(GeolocationOption geolocation)
        {
            var request = new EmulationSetGeolocationOverrideRequest();
            if (geolocation != null)
            {
                request.Accuracy = geolocation.Accuracy;
                request.Latitude = geolocation.Latitude;
                request.Longitude = geolocation.Longitude;
            }

            foreach (Page page in await BrowserContext.GetPagesAsync().ConfigureAwait(false))
            {
                await ((ChromiumPage)page.Delegate).Client.SendAsync(request).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IBrowserContextDelegate.SetPermissionsAsync(string, ContextPermission[])"/>
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions)
        {
            return _client.SendAsync(new BrowserGrantPermissionsRequest
            {
                Origin = origin,
                BrowserContextId = _contextId,
                Permissions = Array.ConvertAll(permissions, permission => WebPermissionToProtocol[permission]),
            });
        }
    }
}
