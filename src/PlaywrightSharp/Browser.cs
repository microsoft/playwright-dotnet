using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowser"/>
    public class Browser : ChannelOwnerBase, IChannelOwner<Browser>, IBrowser
    {
        private readonly BrowserInitializer _initializer;
        private readonly TaskCompletionSource<bool> _closedTcs = new TaskCompletionSource<bool>();
        private bool _isClosedOrClosing;

        internal Browser(IChannelOwner parent, string guid, BrowserInitializer initializer) : base(parent, guid)
        {
            Channel = new BrowserChannel(guid, parent.Connection, this);
            IsConnected = true;
            Channel.Closed += (sender, e) => DidClose();
            _initializer = initializer;
        }

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => Channel;

        /// <inheritdoc/>
        IChannel<Browser> IChannelOwner<Browser>.Channel => Channel;

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public string Version => _initializer.Version;

        /// <inheritdoc/>
        public IBrowserContext[] Contexts => BrowserContextsList.ToArray();

        internal List<BrowserContext> BrowserContextsList { get; } = new List<BrowserContext>();

        internal BrowserChannel Channel { get; }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                await Channel.CloseAsync().ConfigureAwait(false);
            }

            await _closedTcs.Task.ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<IBrowserContext> NewContextAsync(
            ViewportSize viewport,
            string userAgent = null,
            bool? bypassCSP = null,
            bool? javaScriptEnabled = null,
            string timezoneId = null,
            Geolocation geolocation = null,
            ContextPermission[] permissions = null,
            bool? isMobile = null,
            bool? offline = null,
            decimal? deviceScaleFactor = null,
            Credentials httpCredentials = null,
            bool? hasTouch = null,
            bool? acceptDownloads = null,
            bool? ignoreHTTPSErrors = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null)
            => NewContextAsync(new BrowserContextOptions
            {
                Viewport = viewport,
                UserAgent = userAgent,
                BypassCSP = bypassCSP,
                JavaScriptEnabled = javaScriptEnabled,
                TimezoneId = timezoneId,
                Geolocation = geolocation,
                Permissions = permissions,
                IsMobile = isMobile,
                Offline = offline,
                DeviceScaleFactor = deviceScaleFactor,
                HttpCredentials = httpCredentials,
                HasTouch = hasTouch,
                AcceptDownloads = acceptDownloads,
                ColorScheme = colorScheme,
                Locale = locale,
                IgnoreHTTPSErrors = ignoreHTTPSErrors,
                ExtraHttpHeaders = extraHttpHeaders,
            });

        /// <inheritdoc/>
        public Task<IBrowserContext> NewContextAsync(
            string userAgent = null,
            bool? bypassCSP = null,
            bool? javaScriptEnabled = null,
            string timezoneId = null,
            Geolocation geolocation = null,
            ContextPermission[] permissions = null,
            bool? isMobile = null,
            bool? offline = null,
            decimal? deviceScaleFactor = null,
            Credentials httpCredentials = null,
            bool? hasTouch = null,
            bool? acceptDownloads = null,
            bool? ignoreHTTPSErrors = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null)
            => NewContextAsync(new BrowserContextOptions
            {
                UserAgent = userAgent,
                BypassCSP = bypassCSP,
                JavaScriptEnabled = javaScriptEnabled,
                TimezoneId = timezoneId,
                Geolocation = geolocation,
                Permissions = permissions,
                IsMobile = isMobile,
                Offline = offline,
                DeviceScaleFactor = deviceScaleFactor,
                HttpCredentials = httpCredentials,
                HasTouch = hasTouch,
                AcceptDownloads = acceptDownloads,
                IgnoreHTTPSErrors = ignoreHTTPSErrors,
                ColorScheme = colorScheme,
                Locale = locale,
                ExtraHttpHeaders = extraHttpHeaders,
            });

        /// <inheritdoc/>
        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options)
        {
            var context = (await Channel.NewContextAsync(options ?? new BrowserContextOptions()).ConfigureAwait(false)).Object;
            BrowserContextsList.Add(context);
            return context;
        }

        /// <inheritdoc/>
        public Task<IPage> NewPageAsync(
            ViewportSize viewport,
            string userAgent = null,
            bool? bypassCSP = null,
            bool? javaScriptEnabled = null,
            string timezoneId = null,
            Geolocation geolocation = null,
            ContextPermission[] permissions = null,
            bool? isMobile = null,
            bool? offline = null,
            decimal? deviceScaleFactor = null,
            Credentials httpCredentials = null,
            bool? hasTouch = null,
            bool? acceptDownloads = null,
            bool? ignoreHTTPSErrors = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null)
            => NewPageAsync(new BrowserContextOptions
            {
                Viewport = viewport,
                UserAgent = userAgent,
                BypassCSP = bypassCSP,
                JavaScriptEnabled = javaScriptEnabled,
                TimezoneId = timezoneId,
                Geolocation = geolocation,
                Permissions = permissions,
                IsMobile = isMobile,
                Offline = offline,
                DeviceScaleFactor = deviceScaleFactor,
                HttpCredentials = httpCredentials,
                HasTouch = hasTouch,
                AcceptDownloads = acceptDownloads,
                IgnoreHTTPSErrors = ignoreHTTPSErrors,
                ColorScheme = colorScheme,
                Locale = locale,
                ExtraHttpHeaders = extraHttpHeaders,
            });

        /// <inheritdoc/>
        public Task<IPage> NewPageAsync(
            string userAgent = null,
            bool? bypassCSP = null,
            bool? javaScriptEnabled = null,
            string timezoneId = null,
            Geolocation geolocation = null,
            ContextPermission[] permissions = null,
            bool? isMobile = null,
            bool? offline = null,
            decimal? deviceScaleFactor = null,
            Credentials httpCredentials = null,
            bool? hasTouch = null,
            bool? acceptDownloads = null,
            bool? ignoreHTTPSErrors = null,
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null)
            => NewPageAsync(new BrowserContextOptions
            {
                UserAgent = userAgent,
                BypassCSP = bypassCSP,
                JavaScriptEnabled = javaScriptEnabled,
                TimezoneId = timezoneId,
                Geolocation = geolocation,
                Permissions = permissions,
                IsMobile = isMobile,
                Offline = offline,
                DeviceScaleFactor = deviceScaleFactor,
                HttpCredentials = httpCredentials,
                HasTouch = hasTouch,
                AcceptDownloads = acceptDownloads,
                IgnoreHTTPSErrors = ignoreHTTPSErrors,
                ColorScheme = colorScheme,
                Locale = locale,
                ExtraHttpHeaders = extraHttpHeaders,
            });

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync(BrowserContextOptions options)
        {
            var context = await NewContextAsync(options).ConfigureAwait(false) as BrowserContext;
            var page = await context.NewPageAsync().ConfigureAwait(false) as Page;
            page.OwnedContext = context;
            context.OwnerPage = page;
            return page;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        private void DidClose()
        {
            IsConnected = false;
            _isClosedOrClosing = true;
            Disconnected?.Invoke(this, EventArgs.Empty);
            _closedTcs.TrySetResult(true);
        }
    }
}
