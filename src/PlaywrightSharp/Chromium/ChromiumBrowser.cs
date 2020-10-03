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
            string userAgent,
            bool? bypassCSP,
            bool? javaScriptEnabled,
            string timezoneId,
            Geolocation geolocation,
            ContextPermission[] permissions,
            bool? isMobile,
            bool? offline,
            decimal? deviceScaleFactor,
            Credentials httpCredentials,
            bool? hasTouch,
            bool? acceptDownloads,
            bool? ignoreHTTPSErrors,
            ColorScheme? colorScheme,
            string locale,
            Dictionary<string, string> extraHttpHeaders)
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
                extraHttpHeaders).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> NewContextAsync(
            string userAgent,
            bool? bypassCSP,
            bool? javaScriptEnabled,
            string timezoneId,
            Geolocation geolocation,
            ContextPermission[] permissions,
            bool? isMobile,
            bool? offline,
            decimal? deviceScaleFactor,
            Credentials httpCredentials,
            bool? hasTouch,
            bool? acceptDownloads,
            bool? ignoreHTTPSErrors,
            ColorScheme? colorScheme,
            string locale,
            Dictionary<string, string> extraHttpHeaders)
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
                extraHttpHeaders).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> NewContextAsync(BrowserContextOptions options)
            => await base.NewContextAsync(options).ConfigureAwait(false) as IChromiumBrowserContext;
    }
}
