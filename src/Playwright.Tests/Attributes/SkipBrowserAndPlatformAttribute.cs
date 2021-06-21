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

using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Microsoft.Playwright.Tests
{
    public class SkipBrowserAndPlatformAttribute : NUnitAttribute, IApplyToTest
    {
        private readonly bool _skip = false;

        public SkipBrowserAndPlatformAttribute(
            bool skipFirefox = false,
            bool skipChromium = false,
            bool skipWebkit = false,
            bool skipOSX = false,
            bool skipWindows = false,
            bool skipLinux = false)
        {
            if (SkipBrowser(skipFirefox, skipChromium, skipWebkit) && SkipPlatform(skipOSX, skipWindows, skipLinux))
            {
                _skip = true;
            }
        }

        public void ApplyToTest(global::NUnit.Framework.Internal.Test test)
        {
            if (_skip)
            {
                test.RunState = RunState.Ignored;
                test.Properties.Set(global::NUnit.Framework.Internal.PropertyNames.SkipReason, "Skipped by browser/platform");
            }
        }

        private static bool SkipPlatform(bool skipOSX, bool skipWindows, bool skipLinux)
            =>
            (
                (skipOSX && RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) ||
                (skipWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ||
                (skipLinux && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ) ||
            (
                !skipOSX &&
                !skipLinux &&
                !skipWindows
            );

        private static bool SkipBrowser(bool skipFirefox, bool skipChromium, bool skipWebkit)
            =>
            (
                (skipFirefox && TestConstants.IsFirefox) ||
                (skipWebkit && TestConstants.IsWebKit) ||
                (skipChromium && TestConstants.IsChromium)
            ) ||
            (
                !skipFirefox &&
                !skipWebkit &&
                !skipChromium
            );
    }
}
