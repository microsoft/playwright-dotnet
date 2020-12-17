using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IChromiumBrowser"/>
    public class ChromiumBrowser : Browser, IChromiumBrowser
    {
        internal ChromiumBrowser(IChannelOwner parent, string guid, BrowserInitializer initializer) : base(parent, guid, initializer)
        {
        }

        /// <inheritdoc/>
        public new IChromiumBrowserContext[] Contexts => base.Contexts.Cast<IChromiumBrowserContext>().ToArray();

        /// <inheritdoc/>
        public Task StartTracingAsync(IPage page = null, bool screenshots = false, string path = null, IEnumerable<string> categories = null)
            => Channel.StartTracingAsync(page, screenshots, path, categories);

        /// <inheritdoc/>
        public async Task<string> StopTracingAsync()
        {
            string result = await Channel.StopTracingAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(Convert.FromBase64String(result));
        }

        /// <inheritdoc/>
        public async Task<ICDPSession> NewBrowserCDPSessionAsync() => (await Channel.NewBrowserCDPSessionAsync().ConfigureAwait(false)).Object;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> NewContextAsync(
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
            Dictionary<string, string> extraHttpHeaders = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null,
            ProxySettings proxy = null,
            string storageStatePath = null,
            StorageState storageState = null)
            => await base.NewContextAsync(
                viewport,
                userAgent,
                bypassCSP,
                javaScriptEnabled,
                timezoneId,
                geolocation,
                permissions,
                isMobile,
                offline,
                deviceScaleFactor,
                httpCredentials,
                hasTouch,
                acceptDownloads,
                ignoreHTTPSErrors,
                colorScheme,
                locale,
                extraHttpHeaders,
                recordHar,
                recordVideo,
                proxy,
                storageStatePath,
                storageState).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> NewContextAsync(
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
            Dictionary<string, string> extraHttpHeaders = null,
            RecordHarOptions recordHar = null,
            RecordVideoOptions recordVideo = null,
            ProxySettings proxy = null,
            string storageStatePath = null,
            StorageState storageState = null)
            => await base.NewContextAsync(
                userAgent,
                bypassCSP,
                javaScriptEnabled,
                timezoneId,
                geolocation,
                permissions,
                isMobile,
                offline,
                deviceScaleFactor,
                httpCredentials,
                hasTouch,
                acceptDownloads,
                ignoreHTTPSErrors,
                colorScheme,
                locale,
                extraHttpHeaders,
                recordHar,
                recordVideo,
                proxy,
                storageStatePath,
                storageState).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> NewContextAsync(BrowserContextOptions options)
            => await base.NewContextAsync(options).ConfigureAwait(false) as IChromiumBrowserContext;
    }
}
