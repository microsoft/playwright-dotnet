using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="IChromiumBrowserType"/>
    public class ChromiumBrowserType : BrowserType, IChromiumBrowserType
    {
        internal ChromiumBrowserType(IChannelOwner parent, string guid, BrowserTypeInitializer initializer) : base(parent, guid, initializer)
        {
        }

        /// <inheritdoc/>
        public new async Task<IChromiumBrowser> LaunchAsync(LaunchOptions options)
            => await base.LaunchAsync(options).ConfigureAwait(false) as IChromiumBrowser;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowser> LaunchAsync(
            bool? headless,
            string[] args,
            string userDataDir,
            bool? devtools,
            string executablePath,
            string downloadsPath,
            bool? ignoreHTTPSErrors,
            int? timeout,
            bool? dumpIO,
            int? slowMo,
            bool? ignoreDefaultArgs,
            string[] ignoredDefaultArgs,
            Dictionary<string, string> env,
            Dictionary<string, object> firefoxUserPrefs,
            ProxySettings proxy,
            bool? chromiumSandbox,
            bool? handleSIGINT,
            bool? handleSIGTERM,
            bool? handleSIGHUP)
            => await base.LaunchAsync(
                headless,
                args,
                userDataDir,
                devtools,
                executablePath,
                downloadsPath,
                ignoreHTTPSErrors,
                timeout,
                dumpIO,
                slowMo,
                ignoreDefaultArgs,
                ignoredDefaultArgs,
                env,
                firefoxUserPrefs,
                proxy,
                chromiumSandbox,
                handleSIGINT,
                handleSIGTERM,
                handleSIGHUP).ConfigureAwait(false) as IChromiumBrowser;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchOptions options)
            => await LaunchPersistentContextAsync(userDataDir, options).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            ViewportSize viewport,
            bool? headless,
            string[] args,
            bool? devtools,
            string executablePath,
            string downloadsPath,
            bool? ignoreHTTPSErrors,
            int? timeout,
            bool? dumpIO,
            int? slowMo,
            bool? ignoreDefaultArgs,
            string[] ignoredDefaultArgs,
            Dictionary<string, string> env,
            Dictionary<string, object> firefoxUserPrefs,
            ProxySettings proxy,
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
            ColorScheme? colorScheme,
            string locale,
            Dictionary<string, string> extraHttpHeaders,
            bool? chromiumSandbox,
            bool? handleSIGINT,
            bool? handleSIGTERM,
            bool? handleSIGHUP)
            => await LaunchPersistentContextAsync(
                userDataDir,
                viewport,
                headless,
                args,
                devtools,
                executablePath,
                downloadsPath,
                ignoreHTTPSErrors,
                timeout,
                dumpIO,
                slowMo,
                ignoreDefaultArgs,
                ignoredDefaultArgs,
                env,
                firefoxUserPrefs,
                proxy,
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
                colorScheme,
                locale,
                extraHttpHeaders,
                chromiumSandbox,
                handleSIGINT,
                handleSIGTERM,
                handleSIGHUP).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            bool? headless,
            string[] args,
            bool? devtools,
            string executablePath,
            string downloadsPath,
            bool? ignoreHTTPSErrors,
            int? timeout,
            bool? dumpIO,
            int? slowMo,
            bool? ignoreDefaultArgs,
            string[] ignoredDefaultArgs,
            Dictionary<string, string> env,
            Dictionary<string, object> firefoxUserPrefs,
            ProxySettings proxy,
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
            ColorScheme? colorScheme,
            string locale,
            Dictionary<string, string> extraHttpHeaders,
            bool? chromiumSandbox,
            bool? handleSIGINT,
            bool? handleSIGTERM,
            bool? handleSIGHUP)
            => await LaunchPersistentContextAsync(
                userDataDir,
                headless,
                args,
                devtools,
                executablePath,
                downloadsPath,
                ignoreHTTPSErrors,
                timeout,
                dumpIO,
                slowMo,
                ignoreDefaultArgs,
                ignoredDefaultArgs,
                env,
                firefoxUserPrefs,
                proxy,
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
                colorScheme,
                locale,
                extraHttpHeaders,
                chromiumSandbox,
                handleSIGINT,
                handleSIGTERM,
                handleSIGHUP).ConfigureAwait(false) as IChromiumBrowserContext;

        /// <inheritdoc/>
        public new async Task<IChromiumBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options)
            => await base.LaunchPersistentContextAsync(userDataDir, options).ConfigureAwait(false) as IChromiumBrowserContext;
    }
}
