using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Chromium.Protocol.Browser;
using PlaywrightSharp.Chromium.Protocol.Emulation;
using PlaywrightSharp.Chromium.Protocol.Network;
using PlaywrightSharp.Chromium.Protocol.Storage;
using PlaywrightSharp.Chromium.Protocol.Target;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserContextDelegate"/>
    public class ChromiumBrowserContext : IBrowserContextDelegate
    {
        private static readonly IReadOnlyDictionary<ContextPermission, PermissionType> _webPermissionToProtocol = new Dictionary<ContextPermission, PermissionType>
        {
            [ContextPermission.Geolocation] = PermissionType.Geolocation,
            [ContextPermission.Midi] = PermissionType.Midi,
            [ContextPermission.Notifications] = PermissionType.Notifications,
            [ContextPermission.Camera] = PermissionType.VideoCapture,
            [ContextPermission.Microphone] = PermissionType.AudioCapture,
            [ContextPermission.BackgroundSync] = PermissionType.BackgroundSync,
            [ContextPermission.AmbientLightSensor] = PermissionType.Sensors,
            [ContextPermission.Accelerometer] = PermissionType.Sensors,
            [ContextPermission.Gyroscope] = PermissionType.Sensors,
            [ContextPermission.Magnetometer] = PermissionType.Sensors,
            [ContextPermission.AccessibilityEvents] = PermissionType.AccessibilityEvents,
            [ContextPermission.ClipboardRead] = PermissionType.ClipboardReadWrite,
            [ContextPermission.ClipboardWrite] = PermissionType.ClipboardSanitizedWrite,
            [ContextPermission.PaymentHandler] = PermissionType.PaymentHandler,
            [ContextPermission.MidiSysex] = PermissionType.MidiSysex,
        };

        private readonly ChromiumSession _client;
        private readonly string _contextId;
        private readonly ChromiumBrowser _browser;

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
            _browser = chromiumBrowser;
            _contextId = contextId;
            Options = options;
        }

        /// <inheritdoc cref="IBrowserContext"/>
        public BrowserContextOptions Options { get; }

        /// <inheritdoc cref="IBrowserContextDelegate.BrowserContext"/>
        public BrowserContext BrowserContext { get; set; }

        /// <inheritdoc cref="IBrowserContextDelegate.GetPagesAsync"/>
        public async Task<IPage[]> GetPagesAsync()
        {
            var pageTasks =
                _browser.GetAllTargets()
                .Where(target => target.BrowserContext == BrowserContext && target.Type == TargetType.Page)
                .Select(t => t.GetPageAsync());

            var pages = await Task.WhenAll(pageTasks).ConfigureAwait(false);

            return pages.Where(p => p != null).ToArray();
        }

        /// <inheritdoc cref="IBrowserContextDelegate.NewPageAsync"/>
        public async Task<IPage> NewPageAsync()
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
            var target = _browser.TargetsMap[targetId];
            await target.InitializedTask.ConfigureAwait(false);
            return await target.GetPageAsync().ConfigureAwait(false);
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

        /// <inheritdoc cref="IBrowserContextDelegate.CloseAsync"/>
        public async Task CloseAsync()
        {
            if (string.IsNullOrEmpty(_contextId))
            {
                throw new PlaywrightSharpException("Non-incognito profiles cannot be closed!");
            }

            await _client.SendAsync(new TargetDisposeBrowserContextRequest { BrowserContextId = _contextId }).ConfigureAwait(false);
            _browser.RemoveContext(_contextId);
        }

        /// <inheritdoc cref="IBrowserContextDelegate.GetExistingPages"/>
        public IEnumerable<IPage> GetExistingPages()
        {
            foreach (var target in _browser.GetAllTargets())
            {
                if (target.BrowserContext == BrowserContext && target.ChromiumPage != null)
                {
                    yield return target.ChromiumPage.Page;
                }
            }
        }

        /// <inheritdoc cref="IBrowserContextDelegate.SetPermissionsAsync(string, ContextPermission[])"/>
        public Task SetPermissionsAsync(string origin, params ContextPermission[] permissions)
            => _client.SendAsync(new BrowserGrantPermissionsRequest
            {
                Origin = origin,
                BrowserContextId = _contextId,
                Permissions = Array.ConvertAll(permissions, permission => _webPermissionToProtocol[permission]),
            });

        /// <inheritdoc cref="IBrowserContextDelegate.ClearPermissionsAsync"/>
        public Task ClearPermissionsAsync() => throw new System.NotImplementedException();

        /// <inheritdoc cref="IBrowserContextDelegate.SetCookiesAsync(SetNetworkCookieParam[])"/>
        public Task SetCookiesAsync(params SetNetworkCookieParam[] cookies)
            => _client.SendAsync(new StorageSetCookiesRequest
            {
                Cookies = Array.ConvertAll(cookies, c => (CookieParam)c),
                BrowserContextId = _contextId,
            });

        /// <inheritdoc cref="IBrowserContextDelegate.GetCookiesAsync"/>
        public async Task<IEnumerable<NetworkCookie>> GetCookiesAsync()
            => (await _client.SendAsync(new StorageGetCookiesRequest { BrowserContextId = _contextId }).ConfigureAwait(false))
                .Cookies.Select(c => (NetworkCookie)c);

        /// <inheritdoc cref="IBrowserContextDelegate.ClearCookiesAsync"/>
        public Task ClearCookiesAsync() =>
            _client.SendAsync(new StorageClearCookiesRequest { BrowserContextId = _contextId });
    }
}
