using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright
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
        public async Task<IBrowser> LaunchAsync(LaunchOptions options)
        {
            options ??= new LaunchOptions();

            return (await _channel.LaunchAsync(
                options.Headless,
                options.Channel,
                options.ExecutablePath,
                options.Args,
                options.Proxy,
                options.DownloadsPath,
                options.ChromiumSandbox,
                options.FirefoxUserPrefs,
                options.HandleSIGINT,
                options.HandleSIGTERM,
                options.HandleSIGHUP,
                options.Timeout,
                options.Env,
                options.Devtools,
                options.SlowMo,
                options.IgnoreDefaultArgs,
                options.IgnoreAllDefaultArgs).ConfigureAwait(false)).Object;
        }

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
        public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchOptions options)
        {
            if (userDataDir is null)
            {
                throw new ArgumentNullException(nameof(userDataDir));
            }

            options ??= new LaunchOptions();

            return (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                headless: options.Headless,
                channel: options.Channel,
                executablePath: options.ExecutablePath,
                args: options.Args,
                proxy: options.Proxy,
                downloadsPath: options.DownloadsPath,
                chromiumSandbox: options.ChromiumSandbox,
                handleSIGINT: options.HandleSIGINT,
                handleSIGTERM: options.HandleSIGTERM,
                handleSIGHUP: options.HandleSIGHUP,
                timeout: options.Timeout,
                env: options.Env,
                devtools: options.Devtools,
                slowMo: options.SlowMo,
                ignoreHTTPSErrors: options.IgnoreHTTPSErrors,
                recordHarPath: options.RecordHarPath,
                recordHarOmitContent: options.RecordHarOmitContent,
                recordVideoDir: options.RecordVideoDir,
                recordVideoSize: options.RecordVideoSize,
                ignoreDefaultArgs: options.IgnoreDefaultArgs,
                ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs).ConfigureAwait(false)).Object;
        }

        /// <inheritdoc/>
        public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options)
        {
            if (string.IsNullOrEmpty(userDataDir))
            {
                throw new ArgumentException($"'{nameof(userDataDir)}' cannot be null or empty.", nameof(userDataDir));
            }

            options ??= new LaunchPersistentOptions();

            return (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                options.Headless,
                options.Channel,
                options.ExecutablePath,
                options.Args,
                options.Proxy,
                options.DownloadsPath,
                options.ChromiumSandbox,
                options.HandleSIGINT,
                options.HandleSIGTERM,
                options.HandleSIGHUP,
                options.Timeout,
                options.Env,
                options.Devtools,
                options.SlowMo,
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
                options.IgnoreDefaultArgs,
                options.IgnoreAllDefaultArgs).ConfigureAwait(false)).Object;
        }

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
