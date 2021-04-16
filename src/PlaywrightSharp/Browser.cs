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
            Channel = new PlaywrightSharp.Transport.Channels.BrowserChannel(guid, parent.Connection, this);
            IsConnected = true;
            Channel.Closed += (_, _) => DidClose();
            _initializer = initializer;
        }

        /// <inheritdoc/>
        public event EventHandler<IBrowser> Disconnected;

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

        internal PlaywrightSharp.Transport.Channels.BrowserChannel Channel { get; }

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
            bool? acceptDownloads = default,
            bool? bypassCSP = default,
            ColorScheme colorScheme = default,
            float? deviceScaleFactor = default,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = default,
            Geolocation geolocation = default,
            bool? hasTouch = default,
            HttpCredentials httpCredentials = default,
            bool? ignoreHTTPSErrors = default,
            bool? isMobile = default,
            bool? javaScriptEnabled = default,
            string locale = default,
            bool? offline = default,
            IEnumerable<string> permissions = default,
            Proxy proxy = default,
            bool? recordHarOmitContent = default,
            string recordHarPath = default,
            string recordVideoDir = default,
            RecordVideoSize recordVideoSize = default,
            ScreenSize screenSize = default,
            string storageState = default,
            string storageStatePath = default,
            string timezoneId = default,
            string userAgent = default,
            ViewportSize viewportSize = default)
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
                userAgent,
                viewportSize).ConfigureAwait(false)).Object;

            // TODO: this might be a useful thing to rethink down the line
            context.VideoPath = recordVideoDir;

            BrowserContextsList.Add(context);
            return context;
        }

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync(
            bool? acceptDownloads = default,
            bool? bypassCSP = default,
            ColorScheme colorScheme = default,
            float? deviceScaleFactor = default,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = default,
            Geolocation geolocation = default,
            bool? hasTouch = default,
            HttpCredentials httpCredentials = default,
            bool? ignoreHTTPSErrors = default,
            bool? isMobile = default,
            bool? javaScriptEnabled = default,
            string locale = default,
            bool? offline = default,
            IEnumerable<string> permissions = default,
            Proxy proxy = default,
            bool? recordHarOmitContent = default,
            string recordHarPath = default,
            string recordVideoDir = default,
            RecordVideoSize recordVideoSize = default,
            ScreenSize screenSize = default,
            string storageState = default,
            string storageStatePath = default,
            string timezoneId = default,
            string userAgent = default,
            ViewportSize viewportSize = default)
        {
            var context = (BrowserContext)await NewContextAsync(
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
                screenSize,
                storageState,
                storageStatePath,
                timezoneId,
                userAgent,
                viewportSize).ConfigureAwait(false);
            var page = (Page)await context.NewPageAsync().ConfigureAwait(false);
            page.OwnedContext = context;
            context.OwnerPage = page;
            return page;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public Task<IBrowserContext> NewContextAsync(BrowserContextOptions options)
        {
            options ??= new BrowserContextOptions();
            return NewContextAsync(
                options.AcceptDownloads,
                options.BypassCSP,
                options.ColorScheme,
                options.DeviceScaleFactor,
                options.ExtraHTTPHeaders,
                options.Geolocation,
                options.HasTouch,
                options.HttpCredentials,
                options.IgnoreHTTPSErrors,
                options.IsMobile,
                options.JavaScriptEnabled,
                options.Locale,
                options.Offline,
                options.Permissions,
                options.Proxy,
                null,
                null,
                null,
                null,
                screenSize: null,
                options.StorageState,
                options.StorageStatePath,
                options.TimezoneId,
                options.UserAgent,
                options.Viewport);
        }

        private void DidClose()
        {
            IsConnected = false;
            _isClosedOrClosing = true;
            Disconnected?.Invoke(this, this);
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
