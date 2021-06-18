using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    internal partial class BrowserType : ChannelOwnerBase, IChannelOwner<BrowserType>, IBrowserType
    {
        private readonly BrowserTypeInitializer _initializer;
        private readonly BrowserTypeChannel _channel;

        internal BrowserType(IChannelOwner parent, string guid, BrowserTypeInitializer initializer) : base(parent, guid)
        {
            _initializer = initializer;
            _channel = new(guid, parent.Connection, this);
        }

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<BrowserType> IChannelOwner<BrowserType>.Channel => _channel;

        public string ExecutablePath => _initializer.ExecutablePath;

        public string Name => _initializer.Name;

        public async Task<IBrowser> LaunchAsync(BrowserTypeLaunchOptions options = default)
        {
            options ??= new BrowserTypeLaunchOptions();
            return (await _channel.LaunchAsync(
                headless: options.Headless,
                channel: options.Channel,
                executablePath: options.ExecutablePath,
                passedArguments: options.Args,
                proxy: options.Proxy,
                downloadsPath: options.DownloadsPath,
                tracesDir: options.TracesDir,
                chromiumSandbox: options.ChromiumSandbox,
                firefoxUserPrefs: options.FirefoxUserPrefs,
                handleSIGINT: options.HandleSIGINT,
                handleSIGTERM: options.HandleSIGTERM,
                handleSIGHUP: options.HandleSIGHUP,
                timeout: options.Timeout,
                env: options.Env,
                devtools: options.Devtools,
                slowMo: options.SlowMo,
                ignoreDefaultArgs: options.IgnoreDefaultArgs,
                ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs).ConfigureAwait(false)).Object;
        }

        public async Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, BrowserTypeLaunchPersistentContextOptions options = default)
        {
            options ??= new BrowserTypeLaunchPersistentContextOptions();
            return (await _channel.LaunchPersistentContextAsync(
                userDataDir,
                args: options.Args,
                channel: options.Channel,
                chromiumSandbox: options.ChromiumSandbox,
                devtools: options.Devtools,
                downloadsPath: options.DownloadsPath,
                env: options.Env,
                executablePath: options.ExecutablePath,
                handleSIGINT: options.HandleSIGINT,
                handleSIGTERM: options.HandleSIGTERM,
                handleSIGHUP: options.HandleSIGHUP,
                headless: options.Headless,
                proxy: options.Proxy,
                timeout: options.Timeout,
                tracesDir: options.TracesDir,
                slowMo: options.SlowMo,
                ignoreDefaultArgs: options.IgnoreDefaultArgs,
                ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs,
                acceptDownloads: options.AcceptDownloads,
                ignoreHTTPSErrors: options.IgnoreHTTPSErrors,
                bypassCSP: options.BypassCSP,
                viewportSize: options.ViewportSize,
                screenSize: options.ScreenSize,
                userAgent: options.UserAgent,
                deviceScaleFactor: options.DeviceScaleFactor,
                isMobile: options.IsMobile,
                hasTouch: options.HasTouch,
                javaScriptEnabled: options.JavaScriptEnabled,
                timezoneId: options.TimezoneId,
                geolocation: options.Geolocation,
                locale: options.Locale,
                permissions: options.Permissions,
                extraHTTPHeaders: options.ExtraHTTPHeaders,
                offline: options.Offline,
                httpCredentials: options.HttpCredentials,
                colorScheme: options.ColorScheme,
                reducedMotion: options.ReducedMotion,
                recordHarPath: options.RecordHarPath,
                recordHarOmitContent: options.RecordHarOmitContent,
                recordVideo: Browser.GetVideoArgs(options.RecordVideoDir, options.RecordVideoSize)).ConfigureAwait(false)).Object;
        }
    }
}
