using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal partial class Browser : ChannelOwnerBase, IChannelOwner<Browser>, IBrowser
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

        public event EventHandler<IBrowser> Disconnected;

        ChannelBase IChannelOwner.Channel => Channel;

        IChannel<Browser> IChannelOwner<Browser>.Channel => Channel;

        public IReadOnlyList<IBrowserContext> Contexts => BrowserContextsList.ToArray();

        public bool IsConnected { get; private set; }

        public string Version => _initializer.Version;

        internal BrowserChannel Channel { get; }

        internal List<BrowserContext> BrowserContextsList { get; } = new List<BrowserContext>();

        public async Task CloseAsync()
        {
            if (!_isClosedOrClosing)
            {
                _isClosedOrClosing = true;
                await Channel.CloseAsync().ConfigureAwait(false);
            }

            await _closedTcs.Task.ConfigureAwait(false);
        }

        public async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options = default)
        {
            options ??= new BrowserNewContextOptions();
            var context = (await Channel.NewContextAsync(
               acceptDownloads: options.AcceptDownloads,
               bypassCSP: options.BypassCSP,
               colorScheme: options.ColorScheme,
               reducedMotion: options.ReducedMotion,
               deviceScaleFactor: options.DeviceScaleFactor,
               extraHTTPHeaders: options.ExtraHTTPHeaders,
               geolocation: options.Geolocation,
               hasTouch: options.HasTouch,
               httpCredentials: options.HttpCredentials,
               ignoreHTTPSErrors: options.IgnoreHTTPSErrors,
               isMobile: options.IsMobile,
               javaScriptEnabled: options.JavaScriptEnabled,
               locale: options.Locale,
               offline: options.Offline,
               permissions: options.Permissions,
               proxy: options.Proxy,
               recordHarOmitContent: options.RecordHarOmitContent,
               recordHarPath: options.RecordHarPath,
               recordVideo: GetVideoArgs(options.RecordVideoDir, options.RecordVideoSize),
               storageState: options.StorageState,
               storageStatePath: options.StorageStatePath,
               timezoneId: options.TimezoneId,
               userAgent: options.UserAgent,
               viewportSize: options.ViewportSize).ConfigureAwait(false)).Object;

            context.RecordVideo = !string.IsNullOrEmpty(options.RecordVideoDir);

            BrowserContextsList.Add(context);
            return context;
        }

        public async Task<IPage> NewPageAsync(BrowserNewPageOptions options = default)
        {
            options ??= new BrowserNewPageOptions();

            var contextOptions = new BrowserNewContextOptions()
            {
                AcceptDownloads = options.AcceptDownloads,
                IgnoreHTTPSErrors = options.IgnoreHTTPSErrors,
                BypassCSP = options.BypassCSP,
                ViewportSize = options.ViewportSize,
                ScreenSize = options.ScreenSize,
                UserAgent = options.UserAgent,
                DeviceScaleFactor = options.DeviceScaleFactor,
                IsMobile = options.IsMobile,
                HasTouch = options.HasTouch,
                JavaScriptEnabled = options.JavaScriptEnabled,
                TimezoneId = options.TimezoneId,
                Geolocation = options.Geolocation,
                Locale = options.Locale,
                Permissions = options.Permissions,
                ExtraHTTPHeaders = options.ExtraHTTPHeaders,
                Offline = options.Offline,
                HttpCredentials = options.HttpCredentials,
                ColorScheme = options.ColorScheme,
                ReducedMotion = options.ReducedMotion,
                RecordHarPath = options.RecordHarPath,
                RecordHarOmitContent = options.RecordHarOmitContent,
                RecordVideoDir = options.RecordVideoDir,
                RecordVideoSize = options.RecordVideoSize,
                Proxy = options.Proxy,
                StorageState = options.StorageState,
                StorageStatePath = options.StorageStatePath,
            };

            var context = (BrowserContext)await NewContextAsync(contextOptions).ConfigureAwait(false);

            var page = (Page)await context.NewPageAsync().ConfigureAwait(false);
            page.OwnedContext = context;
            context.OwnerPage = page;
            return page;
        }

        public ValueTask DisposeAsync() => CloseAsync();

        internal static Dictionary<string, object> GetVideoArgs(string recordVideoDir, RecordVideoSize recordVideoSize)
        {
            Dictionary<string, object> recordVideoArgs = null;

            if (recordVideoSize != null && string.IsNullOrEmpty(recordVideoDir))
            {
                throw new PlaywrightException("\"RecordVideoSize\" option requires \"RecordVideoDir\" to be specified");
            }

            if (!string.IsNullOrEmpty(recordVideoDir))
            {
                recordVideoArgs = new Dictionary<string, object>()
                {
                    { "dir", recordVideoDir },
                };

                if (recordVideoSize != null)
                {
                    recordVideoArgs["size"] = recordVideoSize;
                }
            }

            return recordVideoArgs;
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
