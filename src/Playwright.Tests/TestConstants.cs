using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.Tests.Helpers;

namespace Microsoft.Playwright.Tests
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

        public const string TestFixtureBrowserCollectionName = "PlaywrightBrowserLoaderFixture collection";
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
            BrowserTypeLaunchPersistentContextOptions options = null,
            bool? headless = null)
        {
            options ??= new BrowserTypeLaunchPersistentContextOptions();
            options.Args ??= args;
            options.Headless ??= headless;
            return browserType.LaunchPersistentContextAsync(userDataDir, options);
        }

        internal static Task<IBrowser> LaunchDefaultAsync(
            this IBrowserType browserType,
            BrowserNewContextOptions options = null,
            string downloadsPath = null,
            string[] args = null)
        {
            options ??= new BrowserNewContextOptions();
            return browserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Args = args,
                DownloadsPath = downloadsPath,
                SlowMo = Convert.ToInt32(Environment.GetEnvironmentVariable("SLOW_MO")),
                Headless = Convert.ToBoolean(Environment.GetEnvironmentVariable("HEADLESS") ?? "true"),
                Timeout = 0
            }
            );
        }

        internal static Task<IBrowser> LaunchDefaultHeadful(
           this IBrowserType browserType,
           BrowserNewContextOptions options = null,
           string downloadsPath = null)
        {
            options ??= new BrowserNewContextOptions();
            return browserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                DownloadsPath = downloadsPath,
                SlowMo = Convert.ToInt32(Environment.GetEnvironmentVariable("SLOW_MO")),
                Headless = false,
                Timeout = 0
            }
            );
        }

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
    }
}
