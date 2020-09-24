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
        private readonly BrowserChannel _channel;
        private readonly BrowserInitializer _initializer;
        private readonly TaskCompletionSource<bool> _closedTcs = new TaskCompletionSource<bool>();
        private bool _isClosedOrClosing;

        internal Browser(IChannelOwner parent, string guid, BrowserInitializer initializer) : base(parent, guid)
        {
            _channel = new BrowserChannel(guid, parent.Connection, this);
            IsConnected = true;
            _channel.Closed += (sender, e) => DidClose();
            _initializer = initializer;
        }

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Browser> IChannelOwner<Browser>.Channel => _channel;

        /// <inheritdoc/>
        public IEnumerable<IBrowserContext> BrowserContexts => BrowserContextsList.ToArray();

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public string Version => _initializer.Version;

        /// <inheritdoc/>
        public IBrowserContext[] Contexts => BrowserContextsList.ToArray();

        internal List<BrowserContext> BrowserContextsList { get; } = new List<BrowserContext>();

        /// <inheritdoc/>
        public Task StartTracingAsync(IPage page = null, bool screenshots = false, string path = null, IEnumerable<string> categories = null)
            => _channel.StartTracingAsync(page, screenshots, path, categories);

        /// <inheritdoc/>
        public async Task<string> StopTracingAsync()
        {
            string result = await _channel.StopTracingAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(Convert.FromBase64String(result));
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                await _channel.CloseAsync().ConfigureAwait(false);
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
                ColorScheme = colorScheme,
                Locale = locale,
                ExtraHttpHeaders = extraHttpHeaders,
            });

        /// <inheritdoc/>
        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options)
        {
            var context = (await _channel.NewContextAsync(options ?? new BrowserContextOptions()).ConfigureAwait(false)).Object;
            BrowserContextsList.Add(context);
            context.Browser = this;
            return context;
        }

        /// <inheritdoc/>
        public async Task<ICDPSession> NewBrowserCDPSessionAsync() => (await _channel.NewBrowserCDPSessionAsync().ConfigureAwait(false)).Object;

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
