using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
{
    internal partial class BrowserType : ChannelOwnerBase, IChannelOwner<BrowserType>, IBrowserType
    {
        /// <summary>
        /// Browser type Chromium.
        /// </summary>
        public const string Chromium = "chromium";

        /// <summary>
        /// Browser type Firefox.
        /// </summary>
        public const string Firefox = "firefox";

        /// <summary>
        /// Browser type WebKit.
        /// </summary>
        public const string Webkit = "webkit";

        private readonly BrowserTypeInitializer _initializer;
        private readonly BrowserTypeChannel _channel;

        internal BrowserType(IChannelOwner parent, string guid, BrowserTypeInitializer initializer) : base(parent, guid)
        {
            _initializer = initializer;
            _channel = new BrowserTypeChannel(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<BrowserType> IChannelOwner<BrowserType>.Channel => _channel;

        public string ExecutablePath => _initializer.ExecutablePath;

        public string Name => _initializer.Name;

        public async Task<IBrowser> LaunchAsync(
            IEnumerable<string> args = default,
            string channel = default,
            bool? chromiumSandbox = default,
            bool? devtools = default,
            string downloadsPath = default,
            IEnumerable<KeyValuePair<string, string>> env = default,
            string executablePath = default,
            bool? handleSIGINT = default,
            bool? handleSIGTERM = default,
            bool? handleSIGHUP = default,
            bool? headless = default,
            Proxy proxy = default,
            float? timeout = default,
            string tracesDir = default,
            IEnumerable<KeyValuePair<string, object>> firefoxUserPrefs = default,
            float? slowMo = default,
            IEnumerable<string> ignoreDefaultArgs = default,
            bool? ignoreAllDefaultArgs = default)
            => (await _channel.LaunchAsync(
                headless,
                channel,
                executablePath,
                args,
                proxy,
                downloadsPath,
                tracesDir,
                chromiumSandbox,
                firefoxUserPrefs,
                handleSIGINT,
                handleSIGTERM,
                handleSIGHUP,
                timeout,
                env,
                devtools,
                slowMo,
                ignoreDefaultArgs,
                ignoreAllDefaultArgs).ConfigureAwait(false)).Object;

        public async Task<IBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            IEnumerable<string> args = default,
            string channel = default,
            bool? chromiumSandbox = default,
            bool? devtools = default,
            string downloadsPath = default,
            IEnumerable<KeyValuePair<string, string>> env = default,
            string executablePath = default,
            bool? handleSIGINT = default,
            bool? handleSIGTERM = default,
            bool? handleSIGHUP = default,
            bool? headless = default,
            Proxy proxy = default,
            float? timeout = default,
            string tracesDir = default,
            float? slowMo = default,
            IEnumerable<string> ignoreDefaultArgs = default,
            bool? ignoreAllDefaultArgs = default,
            bool? acceptDownloads = default,
            bool? ignoreHTTPSErrors = default,
            bool? bypassCSP = default,
            ViewportSize viewportSize = default,
            ScreenSize screenSize = default,
            string userAgent = default,
            float? deviceScaleFactor = default,
            bool? isMobile = default,
            bool? hasTouch = default,
            bool? javaScriptEnabled = default,
            string timezoneId = default,
            Geolocation geolocation = default,
            string locale = default,
            IEnumerable<string> permissions = default,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = default,
            bool? offline = default,
            HttpCredentials httpCredentials = default,
            ColorScheme? colorScheme = default,
            ReducedMotion? reducedMotion = default,
            string recordHarPath = default,
            bool? recordHarOmitContent = default,
            string recordVideoDir = default,
            RecordVideoSize recordVideoSize = default) =>
            (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                headless,
                channel,
                executablePath,
                args,
                proxy,
                downloadsPath,
                tracesDir,
                chromiumSandbox,
                handleSIGINT,
                handleSIGTERM,
                handleSIGHUP,
                timeout,
                env,
                devtools,
                slowMo,
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
                reducedMotion,
                recordHarPath,
                recordHarOmitContent,
                recordVideoDir,
                recordVideoSize,
                ignoreDefaultArgs,
                ignoreAllDefaultArgs).ConfigureAwait(false)).Object;
    }
}
