using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.Tests
{
    internal static class TestConstants
    {
        public static string BrowserName => NUnit.PlaywrightTest.BrowserName;

        public const int DefaultTestTimeout = 30_000;
        public const int DefaultTimeout = 10_000;
        public const int DefaultTaskTimeout = 5_000;

        public const string AboutBlank = "about:blank";

        internal static readonly bool IsWebKit = BrowserName == "webkit";
        internal static readonly bool IsFirefox = BrowserName == "firefox";
        internal static readonly bool IsChromium = BrowserName == "chromium";
        internal static readonly bool IsMacOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        internal static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static string FileToUpload => TestUtils.GetWebServerFile("file-to-upload.txt");

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
