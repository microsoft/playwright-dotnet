using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Firefox;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    internal static class TestConstants
    {
        public const string BrowserCDN = "https://playwright2.blob.core.windows.net";
        public const string ChromiumProduct = "CHROMIUM";
        public const string WebkitProduct = "WEBKIT";
        public const string FirefoxProduct = "FIREFOX";

        public static string Product => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PRODUCT")) ?
            ChromiumProduct :
            Environment.GetEnvironmentVariable("PRODUCT");

        public const string TestFixtureCollectionName = "PlaywrightSharpLoaderFixture collection";
        public const string TestFixtureBrowserCollectionName = "PlaywrightSharpBrowserLoaderFixture collection";
        public const string TestFixtureWebCollectionName = "PlaywrightSharpWebLoaderFixture collection";
        public const int Port = 8081;
        public const int HttpsPort = Port + 1;
        public const string ServerUrl = "http://localhost:8081";
        public const string ServerIpUrl = "http://127.0.0.1:8081";
        public const string HttpsPrefix = "https://localhost:8082";

        internal static IBrowserType GetNewBrowserType()
            => Product switch
            {
                WebkitProduct => null,
                FirefoxProduct => new FirefoxBrowserType(),
                ChromiumProduct => new ChromiumBrowserType(),
                _ => throw new ArgumentOutOfRangeException($"product {Product} does not exist")
            };


        public const string AboutBlank = "about:blank";
        public const string CrossProcessHttpPrefix = "http://127.0.0.1:8081";
        public static readonly string EmptyPage = $"{ServerUrl}/empty.html";
        public const string CrossProcessUrl = ServerIpUrl;

        internal static LaunchOptions GetDefaultBrowserOptions()
            => new LaunchOptions
            {
                SlowMo = Convert.ToInt32(Environment.GetEnvironmentVariable("SLOW_MO")),
                Headless = Convert.ToBoolean(Environment.GetEnvironmentVariable("HEADLESS") ?? "true"),
                Timeout = 0,
                LogProcess = true,
            };

        public static LaunchOptions GetHeadfulOptions()
        {
            var options = GetDefaultBrowserOptions();
            options.Headless = false;
            return options;
        }

        public static readonly string ExtensionPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "simple-extension");
        public static readonly DeviceDescriptor IPhone = GetNewBrowserType().Devices[DeviceDescriptorName.IPhone6];
        public static readonly DeviceDescriptor IPhoneLandscape = GetNewBrowserType().Devices[DeviceDescriptorName.IPhone6Landscape];
        private static ILoggerFactory LoggerFactory { get; set; }
        public static string FileToUpload => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "file-to-upload.txt");

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

        public static void SetupLogging(ITestOutputHelper output)
        {
            if (Debugger.IsAttached && LoggerFactory == null)
            {
                LoggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
            }
        }
    }
}
