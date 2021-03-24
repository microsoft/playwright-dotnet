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
            IEnumerable<string> args = null,
            string channel = null,
            bool? chromiumSandbox = null,
            bool? devtools = null,
            string downloadsPath = null,
            IEnumerable<KeyValuePair<string, string>> env = null,
            string executablePath = null,
            IEnumerable<KeyValuePair<string, object>> firefoxUserPrefs = null,
            bool? handleSIGHUP = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? headless = null,
            bool? ignoreAllDefaultArgs = null,
            IEnumerable<string> ignoreDefaultArgs = null,
            Proxy proxy = null,
            float? slowMo = null,
            float? timeout = null)
            => (await _channel.LaunchAsync(
                args,
                channel,
                chromiumSandbox,
                devtools,
                downloadsPath,
                env,
                executablePath,
                firefoxUserPrefs,
                handleSIGHUP,
                handleSIGINT,
                handleSIGTERM,
                headless,
                ignoreAllDefaultArgs,
                ignoreDefaultArgs,
                proxy,
                slowMo,
                timeout).ConfigureAwait(false)).Object;

        /// <inheritdoc/>
        public async Task<IBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            bool? acceptDownloads = null,
            IEnumerable<string> args = null,
            bool? bypassCSP = null,
            string channel = null,
            bool? chromiumSandbox = null,
            ColorScheme colorScheme = ColorScheme.Undefined,
            float? deviceScaleFactor = null,
            bool? devtools = null,
            string downloadsPath = null,
            IEnumerable<KeyValuePair<string, string>> env = null,
            string executablePath = null,
            IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders = null,
            Geolocation geolocation = null,
            bool? handleSIGHUP = null,
            bool? handleSIGINT = null,
            bool? handleSIGTERM = null,
            bool? hasTouch = null,
            bool? headless = null,
            HttpCredentials httpCredentials = null,
            bool? ignoreAllDefaultArgs = null,
            IEnumerable<string> ignoreDefaultArgs = null,
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
            float? slowMo = null,
            float? timeout = null,
            string timezoneId = null,
            string userAgent = null) =>
            (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                acceptDownloads,
                args,
                bypassCSP,
                channel,
                chromiumSandbox,
                colorScheme,
                deviceScaleFactor,
                devtools,
                downloadsPath,
                env,
                executablePath,
                extraHTTPHeaders,
                geolocation,
                handleSIGHUP,
                handleSIGINT,
                handleSIGTERM,
                hasTouch,
                headless,
                httpCredentials,
                ignoreAllDefaultArgs,
                ignoreDefaultArgs,
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
                slowMo,
                timeout,
                timezoneId,
                userAgent).ConfigureAwait(false)).Object;
    }
}
