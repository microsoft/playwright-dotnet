/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Playwright.NUnit;

[assembly: NUnit.Framework.Timeout(Microsoft.Playwright.Tests.TestConstants.DefaultTestTimeout)]
[assembly: NUnit.Framework.Parallelizable(NUnit.Framework.ParallelScope.Fixtures)]

namespace Microsoft.Playwright.Tests
{
    internal static class TestConstants
    {
        public const int DefaultTestTimeout = 30_000;
        public const int DefaultTimeout = 10_000;
        public const int DefaultTaskTimeout = 5_000;

        public const string AboutBlank = "about:blank";

        public static string FileToUpload => TestUtils.GetWebServerFile("file-to-upload.txt");

        public static readonly IEnumerable<string> NestedFramesDumpResult = new List<string>()
        {
            "http://localhost:<PORT>/frames/nested-frames.html",
            "    http://localhost:<PORT>/frames/two-frames.html (2frames)",
            "        http://localhost:<PORT>/frames/frame.html (uno)",
            "        http://localhost:<PORT>/frames/frame.html (dos)",
            "    http://localhost:<PORT>/frames/frame.html (aframe)"
        };

        internal static bool IsWebKit(this PlaywrightTest test) => test.BrowserName == BrowserType.Webkit;
        internal static bool IsFirefox(this PlaywrightTest test) => test.BrowserName == BrowserType.Firefox;
        internal static bool IsChromium(this PlaywrightTest test) => test.BrowserName == BrowserType.Chromium;


        internal static bool IsMacOSX(this PlaywrightTest _) => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        internal static bool IsWindows(this PlaywrightTest  _) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal static bool IsLinux(this PlaywrightTest _) => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
