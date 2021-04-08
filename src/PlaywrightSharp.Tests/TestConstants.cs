using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Tests.Helpers;

namespace PlaywrightSharp.Tests
{
    internal static class TestConstants
    {
        public const string ChromiumProduct = "CHROMIUM";
        public const string WebkitProduct = "WEBKIT";
        public const string FirefoxProduct = "FIREFOX";
        public const int DefaultTestTimeout = 30_000;
        public const int DefaultPuppeteerTimeout = 10_000;
        public const int DefaultTaskTimeout = 5_000;

        public static string Product => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PRODUCT")) ?
            ChromiumProduct :
            Environment.GetEnvironmentVariable("PRODUCT");

        public const string TestFixtureBrowserCollectionName = "PlaywrightSharpBrowserLoaderFixture collection";
        public const int Port = 8081;
        public const int HttpsPort = Port + 1;
        public const string ServerUrl = "http://localhost:8081";
        public const string ServerIpUrl = "http://127.0.0.1:8081";
        public const string HttpsPrefix = "https://localhost:8082";

        public const string AboutBlank = "about:blank";
        public const string CrossProcessHttpPrefix = "http://127.0.0.1:8081";
        public static readonly string EmptyPage = $"{ServerUrl}/empty.html";
        public const string CrossProcessUrl = ServerIpUrl;

        internal static Task<IBrowserContext> LaunchDefaultPersistentContext(
            this IBrowserType browserType,
            string userDataDir,
            string[] args = null,
            BrowserContextOptions options = null,
            bool? headless = null)
        {
            options ??= new BrowserContextOptions();
            return browserType.LaunchPersistentContextAsync(
                userDataDir: userDataDir,
                acceptDownloads: options.AcceptDownloads,
                args: args,
                options.BypassCSP,
                channel: null,
                chromiumSandbox: null,
                options.ColorScheme,
                options.DeviceScaleFactor,
                devtools: null,
                downloadsPath: null,
                env: null,
                executablePath: null,
                extraHTTPHeaders: options.ExtraHTTPHeaders,
                geolocation: options.Geolocation,
                handleSIGHUP: null,
                handleSIGINT: null,
                handleSIGTERM: null,
                hasTouch: options.HasTouch,
                headless: headless,
                httpCredentials: options.HttpCredentials,
                ignoreAllDefaultArgs: null,
                ignoreHTTPSErrors: options.IgnoreHTTPSErrors,
                isMobile: options.IsMobile,
                javaScriptEnabled: options.JavaScriptEnabled,
                locale: options.Locale,
                offline: options.Offline,
                permissions: options.Permissions,
                proxy: options.Proxy,
                recordHarOmitContent: null,
                recordHarPath: null,
                recordVideoDir: null,
                recordVideoSize: null,
                slowMo: null,
                timeout: null,
                timezoneId: options.TimezoneId,
                userAgent: options.UserAgent
                );
        }

        internal static Task<IBrowser> LaunchDefaultAsync(
            this IBrowserType browserType,
            BrowserContextOptions options = null,
            string downloadsPath = null)
        {
            options ??= new BrowserContextOptions();
            return browserType.LaunchAsync(
                    downloadsPath: downloadsPath,
                    slowMo: Convert.ToInt32(Environment.GetEnvironmentVariable("SLOW_MO")),
                    headless: Convert.ToBoolean(Environment.GetEnvironmentVariable("HEADLESS") ?? "true"),
                    timeout: 0
                );
        }

        internal static Task<IBrowser> LaunchDefaultHeadful(
           this IBrowserType browserType,
           BrowserContextOptions options = null,
           string downloadsPath = null)
        {
            options ??= new BrowserContextOptions();
            return browserType.LaunchAsync(
                    downloadsPath: downloadsPath,
                    slowMo: Convert.ToInt32(Environment.GetEnvironmentVariable("SLOW_MO")),
                    headless: false,
                    timeout: 0
                );
        }

        /*
        internal static LaunchOptions GetDefaultBrowserOptions()
            => new LaunchOptions
            {
                SlowMo = Convert.ToInt32(Environment.GetEnvironmentVariable("SLOW_MO")),
                Headless = Convert.ToBoolean(Environment.GetEnvironmentVariable("HEADLESS") ?? "true"),
                Timeout = 0,
            };

        public static LaunchOptions GetHeadfulOptions()
        {
            var options = GetDefaultBrowserOptions();
            options.Headless = false;
            return options;
        }*/

        public static string FileToUpload => TestUtils.GetWebServerFile("file-to-upload.txt");

        internal static ILoggerFactory LoggerFactory { get; set; } = LoggerFactory = new LoggerFactory();
        internal static readonly bool IsWebKit = Product.Equals(WebkitProduct);
        internal static readonly bool IsFirefox = Product.Equals(FirefoxProduct);
        internal static readonly bool IsChromium = Product.Equals(ChromiumProduct);
        internal static readonly bool IsMacOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        internal static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static readonly IEnumerable<string> NestedFramesDumpResult = new List<string>()
        {
            "http://localhost:<PORT>/frames/nested-frames.html",
            "    http://localhost:<PORT>/frames/two-frames.html (2frames)",
            "        http://localhost:<PORT>/frames/frame.html (uno)",
            "        http://localhost:<PORT>/frames/frame.html (dos)",
            "    http://localhost:<PORT>/frames/frame.html (aframe)"
        };

        internal static readonly BrowserContextOptions iPhone6 = new BrowserContextOptions()
        {
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1",
            Viewport = new ViewportSize() { Height = 667, Width = 375 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
        };

        internal static readonly BrowserContextOptions iPhone6Landscape = new BrowserContextOptions()
        {
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1",
            Viewport = new ViewportSize() { Height = 375, Width = 667 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
        };
    }
}
