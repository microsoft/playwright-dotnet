using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IBrowserType" />
    public class BrowserType : ChannelOwnerBase, IChannelOwner<BrowserType>, IBrowserType
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

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<BrowserType> IChannelOwner<BrowserType>.Channel => _channel;

        /// <inheritdoc />
        public string ExecutablePath => _initializer.ExecutablePath;

        /// <inheritdoc />
        public string Name => _initializer.Name;

        /// <inheritdoc/>
        public async Task<IBrowser> LaunchAsync(
            bool? headless = default,
            BrowserChannel channel = default,
            string executablePath = default,
            IEnumerable<string> args = default,
            Proxy proxy = default,
            string downloadsPath = default,
            bool? chromiumSandbox = default,
            IEnumerable<KeyValuePair<string, object>> firefoxUserPrefs = default,
            bool? handleSIGINT = default,
            bool? handleSIGTERM = default,
            bool? handleSIGHUP = default,
            float? timeout = default,
            IEnumerable<KeyValuePair<string, string>> env = default,
            bool? devtools = default,
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

        /// <inheritdoc/>
        public async Task<IBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            bool? headless = default,
            BrowserChannel channel = default,
            string executablePath = default,
            IEnumerable<string> args = default,
            Proxy proxy = default,
            string downloadsPath = default,
            bool? chromiumSandbox = default,
            bool? handleSIGINT = default,
            bool? handleSIGTERM = default,
            bool? handleSIGHUP = default,
            float? timeout = default,
            IEnumerable<KeyValuePair<string, string>> env = default,
            bool? devtools = default,
            float? slowMo = default,
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
            ColorScheme colorScheme = default,
            string recordHarPath = default,
            bool? recordHarOmitContent = default,
            string recordVideoDir = default,
            RecordVideoSize recordVideoSize = default,
            IEnumerable<string> ignoreDefaultArgs = default,
            bool? ignoreAllDefaultArgs = default) =>
            (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                headless,
                channel,
                executablePath,
                args,
                proxy,
                downloadsPath,
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
                recordHarPath,
                recordHarOmitContent,
                recordVideoDir,
                recordVideoSize,
                ignoreDefaultArgs,
                ignoreAllDefaultArgs).ConfigureAwait(false)).Object;
    }
}
