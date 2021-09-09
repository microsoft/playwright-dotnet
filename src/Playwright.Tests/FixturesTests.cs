/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>fixtures.spec.ts</playwright-file>
    ///<playwright-describe>Fixtures</playwright-describe>
    public class FixturesTests : PlaywrightTestEx
    {
        [PlaywrightTest("fixtures.spec.ts", "should close the browser when the node process closes")]
        [Ignore("We don't need to test process handling")]
        public void ShouldCloseTheBrowserWhenTheNodeProcessCloses() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should report browser close signal")]
        [Ignore("We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should report browser close signal 2")]
        [Ignore("We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal2() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGINT")]
        [Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGTERM")]
        [Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGHUP")]
        [Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on double SIGINT")]
        [Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGINT + SIGERM")]
        [Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }

        [PlaywrightTest("fixtures.spec.ts", "should report browser close signal")]
        [Ignore("We don't need to test stacktrace")]
        public void CallerFilePath() { }
    }
}
