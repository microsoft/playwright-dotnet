using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    public partial interface IBrowser : IAsyncDisposable
    {
    }

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
            Channel.Closed += (_, _) => DidClose();
            _initializer = initializer;
        }

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => Channel;

        /// <inheritdoc/>
        IChannel<Browser> IChannelOwner<Browser>.Channel => Channel;

        /// <inheritdoc/>
        public IReadOnlyCollection<IBrowserContext> Contexts => BrowserContextsList.ToArray();

        /// <inheritdoc/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc/>
        public string Version => _initializer.Version;

        internal BrowserChannel Channel { get; }

        internal List<BrowserContext> BrowserContextsList { get; } = new List<BrowserContext>();

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
        public async Task<IBrowserContext> NewContextAsync(
            bool? acceptDownloads = null,
            bool? bypassCSP = null,
            ColorScheme colorScheme = ColorScheme.Undefined,
            float? deviceScaleFactor = null,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = null,
            Geolocation geolocation = null,
            bool? hasTouch = null,
            HttpCredentials httpCredentials = null,
            bool? ignoreHTTPSErrors = null,
            bool? isMobile = null,
            bool? javaScriptEnabled = null,
            string locale = null,
            bool? offline = null,
            IEnumerable<string> permissions = null,
            Proxy proxy = null,
            bool? recordHarOmitContent = null,
            string recordHarPath = null,
            string recordVideoDir = null,
            RecordVideoSize recordVideoSize = null,
            string storageState = null,
            string storageStatePath = null,
            string timezoneId = null,
            string userAgent = null)
        {
            var context = (await Channel.NewContextAsync(
                acceptDownloads,
                bypassCSP,
                colorScheme,
                deviceScaleFactor,
                extraHTTPHeaders,
                geolocation,
                hasTouch,
                httpCredentials,
                ignoreHTTPSErrors,
                isMobile,
                javaScriptEnabled,
                locale,
                offline,
                permissions,
                proxy,
                recordHarOmitContent,
                recordHarPath,
                recordVideoDir,
                recordVideoSize,
                storageState,
                storageStatePath,
                timezoneId,
                userAgent).ConfigureAwait(false)).Object;

            BrowserContextsList.Add(context);
            return context;
        }

        /// <inheritdoc/>
        public Task<IPage> NewPageAsync(
            bool? acceptDownloads = null,
            bool? bypassCSP = null,
            ColorScheme colorScheme = ColorScheme.Undefined,
            float? deviceScaleFactor = null,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = null,
            Geolocation geolocation = null,
            bool? hasTouch = null,
            HttpCredentials httpCredentials = null,
            bool? ignoreHTTPSErrors = null,
            bool? isMobile = null,
            bool? javaScriptEnabled = null,
            string locale = null,
            bool? offline = null,
            IEnumerable<string> permissions = null,
            Proxy proxy = null,
            bool? recordHarOmitContent = null,
            string recordHarPath = null,
            string recordVideoDir = null,
            RecordVideoSize recordVideoSize = null,
            string storageState = null,
            string storageStatePath = null,
            string timezoneId = null,
            string userAgent = null) => throw new NotImplementedException();

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        private void DidClose()
        {
            IsConnected = false;
            _isClosedOrClosing = true;
            Disconnected?.Invoke(this, EventArgs.Empty);
            _closedTcs.TrySetResult(true);
        }

        /*

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
            Dictionary<string, string> extraHTTPHeaders = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null,
            ProxySettings proxy = null,
            string storageStatePath = null,
            StorageState storageState = null)
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
                ExtraHTTPHeaders = extraHTTPHeaders,
                RecordHar = recordHar,
                RecordVideo = recordVideo,
                Proxy = proxy,
                StorageStatePath = storageStatePath,
                StorageState = storageState,
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
            Dictionary<string, string> extraHTTPHeaders = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null,
            ProxySettings proxy = null,
            string storageStatePath = null,
            StorageState storageState = null)
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
                ExtraHTTPHeaders = extraHTTPHeaders,
                RecordHar = recordHar,
                RecordVideo = recordVideo,
                Proxy = proxy,
                StorageStatePath = storageStatePath,
                StorageState = storageState,
            });

        /// <inheritdoc/>
        public async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options)
        {
            var context = (await Channel.NewContextAsync(options ?? new BrowserContextOptions()).ConfigureAwait(false)).Object;
            context.Options = options;
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
            Dictionary<string, string> extraHTTPHeaders = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null,
            ProxySettings proxy = null,
            string storageStatePath = null,
            StorageState storageState = null)
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
                ExtraHTTPHeaders = extraHTTPHeaders,
                RecordHar = recordHar,
                RecordVideo = recordVideo,
                Proxy = proxy,
                StorageStatePath = storageStatePath,
                StorageState = storageState,
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
            Dictionary<string, string> extraHTTPHeaders = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null,
            ProxySettings proxy = null,
            string storageStatePath = null,
            StorageState storageState = null)
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
                ExtraHTTPHeaders = extraHTTPHeaders,
                RecordHar = recordHar,
                RecordVideo = recordVideo,
                Proxy = proxy,
                StorageStatePath = storageStatePath,
                StorageState = storageState,
            });

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync(BrowserContextOptions options)
        {
            var context = (BrowserContext)await NewContextAsync(options).ConfigureAwait(false);
            var page = (Page)await context.NewPageAsync().ConfigureAwait(false);
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
        }*/
    }
}
