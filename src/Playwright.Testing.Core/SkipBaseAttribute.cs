/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Playwright.Testing.Core
{
    public class SkipBaseAttribute : Attribute
    {
        private readonly TestTargets[] _combinations;

        /// <summary>
        /// Skips the combinations provided.
        /// </summary>
        /// <param name="pairs"></param>
        public SkipBaseAttribute(params TestTargets[] combinations)
        {
            _combinations = combinations;
        }

        protected bool ShouldSkip()
        {
            if (_combinations.Any(combination =>
            {
                var requirements = (Enum.GetValues(typeof(TestTargets)) as TestTargets[])?.Where(x => combination.HasFlag(x));
                return requirements.All(flag =>
                    flag switch
                    {
                        TestTargets.Windows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                        TestTargets.Linux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                        TestTargets.OSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                        TestTargets.Chromium => PlaywrightBaseTest.BrowserName == BrowserType.Chromium,
                        TestTargets.Firefox => PlaywrightBaseTest.BrowserName == BrowserType.Firefox,
                        TestTargets.Webkit => PlaywrightBaseTest.BrowserName == BrowserType.Webkit,
                        _ => false,
                    });
            }))
            {
                return true;
            }

            return false;
        }
    }
}
