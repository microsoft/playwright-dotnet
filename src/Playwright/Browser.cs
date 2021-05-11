using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    /// <inheritdoc cref="IBrowser"/>
    public class Browser : ChannelOwnerBase, IChannelOwner<Browser>, IBrowser
    {
        private readonly BrowserInitializer _initializer;
        private readonly TaskCompletionSource<bool> _closedTcs = new TaskCompletionSource<bool>();
        private bool _isClosedOrClosing;

        internal Browser(IChannelOwner parent, string guid, BrowserInitializer initializer) : base(parent, guid)
        {
            Channel = new Microsoft.Playwright.Transport.Channels.BrowserChannel(guid, parent.Connection, this);
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

        internal Microsoft.Playwright.Transport.Channels.BrowserChannel Channel { get; }

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
            bool? acceptDownloads,
            bool? ignoreHTTPSErrors,
            bool? bypassCSP,
            ViewportSize viewportSize,
            ScreenSize screenSize,
            string userAgent,
            float? deviceScaleFactor,
            bool? isMobile,
            bool? hasTouch,
            bool? javaScriptEnabled,
            string timezoneId,
            Geolocation geolocation,
            string locale,
            IEnumerable<string> permissions,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders,
            bool? offline,
            HttpCredentials httpCredentials,
            ColorScheme colorScheme,
            string recordHarPath,
            bool? recordHarOmitContent,
            string recordVideoDir,
            RecordVideoSize recordVideoSize,
            Proxy proxy,
            string storageState,
            string storageStatePath)
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

            context.RecordVideo = !string.IsNullOrEmpty(recordVideoDir);

            BrowserContextsList.Add(context);
            return context;
        }

        /// <inheritdoc/>
        public async Task<IPage> NewPageAsync(
            bool? acceptDownloads,
            bool? ignoreHTTPSErrors,
            bool? bypassCSP,
            ViewportSize viewportSize,
            ScreenSize screenSize,
            string userAgent,
            float? deviceScaleFactor,
            bool? isMobile,
            bool? hasTouch,
            bool? javaScriptEnabled,
            string timezoneId,
            Geolocation geolocation,
            string locale,
            IEnumerable<string> permissions,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders,
            bool? offline,
            HttpCredentials httpCredentials,
            ColorScheme colorScheme,
            string recordHarPath,
            bool? recordHarOmitContent,
            string recordVideoDir,
            RecordVideoSize recordVideoSize,
            Proxy proxy,
            string storageState,
            string storageStatePath)
        {
            var context = (BrowserContext)await NewContextAsync(
                acceptDownloads,
                ignoreHTTPSErrors,
                bypassCSP,
                viewportSize,
                screenSize,
                userAgent,
                deviceScaleFactor,
                isMobile,
                hasTouch,
                javaScriptEnabled,
                timezoneId,
                geolocation,
                locale,
                permissions,
                extraHTTPHeaders,
                offline,
                httpCredentials,
                colorScheme,
                recordHarPath,
                recordHarOmitContent,
                recordVideoDir,
                recordVideoSize,
                proxy,
                storageState,
                storageStatePath).ConfigureAwait(false);
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
                options.IgnoreHTTPSErrors,
                options.BypassCSP,
                options.Viewport,
                options.ScreenSize,
                options.UserAgent,
                options.DeviceScaleFactor,
                options.IsMobile,
                options.HasTouch,
                options.JavaScriptEnabled,
                options.TimezoneId,
                options.Geolocation,
                options.Locale,
                options.Permissions,
                options.ExtraHTTPHeaders,
                options.Offline,
                options.HttpCredentials,
                options.ColorScheme,
                options.RecordHarPath,
                options.RecordHarOmitContent,
                options.RecordVideoDir,
                options.RecordVideoSize,
                options.Proxy,
                options.StorageState,
                options.StorageStatePath);
        }

        /// <inheritdoc/>
        public Task<IPage> NewPageAsync(BrowserContextOptions options)
        {
            options ??= new BrowserContextOptions();
            return NewPageAsync(
                options.AcceptDownloads,
                options.IgnoreHTTPSErrors,
                options.BypassCSP,
                options.Viewport,
                options.ScreenSize,
                options.UserAgent,
                options.DeviceScaleFactor,
                options.IsMobile,
                options.HasTouch,
                options.JavaScriptEnabled,
                options.TimezoneId,
                options.Geolocation,
                options.Locale,
                options.Permissions,
                options.ExtraHTTPHeaders,
                options.Offline,
                options.HttpCredentials,
                options.ColorScheme,
                options.RecordHarPath,
                options.RecordHarOmitContent,
                options.RecordVideoDir,
                options.RecordVideoSize,
                options.Proxy,
                options.StorageState,
                options.StorageStatePath);
        }

        private void DidClose()
        {
            IsConnected = false;
            _isClosedOrClosing = true;
            Disconnected?.Invoke(this, this);
            _closedTcs.TrySetResult(true);
        }
    }
}
