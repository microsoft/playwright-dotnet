using System;
using System.Collections.Generic;
using System.Text.Json;
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

        /// <inheritdoc />
        public Task<IBrowser> LaunchAsync(
            bool? headless = null,
            string[] args = null,
            string userDataDir = null,
            bool? devtools = null,
            string executablePath = null,
            string downloadsPath = null,
            bool? ignoreHTTPSErrors = null,
            int? timeout = null,
            bool? dumpIO = null,
            int? slowMo = null,
            bool? ignoreDefaultArgs = null,
            string[] ignoredDefaultArgs = null,
            IDictionary<string, string> env = null,
            IDictionary<string, object> firefoxUserPrefs = null,
            ProxySettings proxy = null)
            => LaunchAsync(new LaunchOptions
            {
                Headless = headless,
                Args = args,
                UserDataDir = userDataDir,
                Devtools = devtools,
                ExecutablePath = executablePath,
                DownloadsPath = downloadsPath,
                IgnoreHTTPSErrors = ignoreHTTPSErrors,
                Timeout = timeout,
                DumpIO = dumpIO,
                SlowMo = slowMo,
                IgnoreDefaultArgs = ignoreHTTPSErrors,
                IgnoredDefaultArgs = ignoredDefaultArgs,
                Env = env,
                FirefoxUserPrefs = firefoxUserPrefs,
                Proxy = proxy,
            });

        /// <inheritdoc />
        public async Task<IBrowser> LaunchAsync(LaunchOptions options = null)
        {
            if (!string.IsNullOrEmpty(options?.UserDataDir))
            {
                throw new ArgumentException("UserDataDir option is not supported in LaunchAsync. Use LaunchPersistentContextAsync instead");
            }

            return (await _channel.LaunchAsync(options ?? new LaunchOptions()).ConfigureAwait(false)).Object;
        }

        /// <inheritdoc />
        public Task<IBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            ViewportSize viewport,
            bool? headless = null,
            string[] args = null,
            bool? devtools = null,
            string executablePath = null,
            string downloadsPath = null,
            bool? ignoreHTTPSErrors = null,
            int? timeout = null,
            bool? dumpIO = null,
            int? slowMo = null,
            bool? ignoreDefaultArgs = null,
            string[] ignoredDefaultArgs = null,
            IDictionary<string, string> env = null,
            IDictionary<string, object> firefoxUserPrefs = null,
            ProxySettings proxy = null,
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
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null)
            => LaunchPersistentContextAsync(
                userAgent,
                new LaunchPersistentOptions
                {
                    Headless = headless,
                    Args = args,
                    UserDataDir = userDataDir,
                    Devtools = devtools,
                    ExecutablePath = executablePath,
                    DownloadsPath = downloadsPath,
                    IgnoreHTTPSErrors = ignoreHTTPSErrors,
                    Timeout = timeout,
                    DumpIO = dumpIO,
                    SlowMo = slowMo,
                    IgnoreDefaultArgs = ignoreHTTPSErrors,
                    IgnoredDefaultArgs = ignoredDefaultArgs,
                    Env = env,
                    FirefoxUserPrefs = firefoxUserPrefs,
                    Proxy = proxy,
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
                    ExtraHttpHeaders = extraHttpHeaders,
                });

        /// <inheritdoc />
        public Task<IBrowserContext> LaunchPersistentContextAsync(
            string userDataDir,
            bool? headless = null,
            string[] args = null,
            bool? devtools = null,
            string executablePath = null,
            string downloadsPath = null,
            bool? ignoreHTTPSErrors = null,
            int? timeout = null,
            bool? dumpIO = null,
            int? slowMo = null,
            bool? ignoreDefaultArgs = null,
            string[] ignoredDefaultArgs = null,
            IDictionary<string, string> env = null,
            IDictionary<string, object> firefoxUserPrefs = null,
            ProxySettings proxy = null,
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
            ColorScheme? colorScheme = null,
            string locale = null,
            Dictionary<string, string> extraHttpHeaders = null)
            => LaunchPersistentContextAsync(
                userAgent,
                new LaunchPersistentOptions
                {
                    Headless = headless,
                    Args = args,
                    UserDataDir = userDataDir,
                    Devtools = devtools,
                    ExecutablePath = executablePath,
                    DownloadsPath = downloadsPath,
                    IgnoreHTTPSErrors = ignoreHTTPSErrors,
                    Timeout = timeout,
                    DumpIO = dumpIO,
                    SlowMo = slowMo,
                    IgnoreDefaultArgs = ignoreHTTPSErrors,
                    IgnoredDefaultArgs = ignoredDefaultArgs,
                    Env = env,
                    FirefoxUserPrefs = firefoxUserPrefs,
                    Proxy = proxy,
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
                    ExtraHttpHeaders = extraHttpHeaders,
                });

        /// <inheritdoc />
        public Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchOptions options)
            => LaunchPersistentContextAsync(userDataDir, options?.ToPersistentOptions() ?? new LaunchPersistentOptions());

        /// <inheritdoc />
        public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, LaunchPersistentOptions options)
            => (await _channel.LaunchPersistentContextAsync(userDataDir, options ?? new LaunchPersistentOptions()).ConfigureAwait(false)).Object;
    }
}
