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

using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browser.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class BrowserTests : BrowserTestEx
    {
        [PlaywrightTest("browser.spec.ts", "should create new page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCreateNewPage()
        {
            var browser = await Playwright[TestConstants.BrowserName].LaunchAsync();
            var page1 = await browser.NewPageAsync();
            Assert.That(browser.Contexts, Has.Length.EqualTo(1));

            var page2 = await browser.NewPageAsync();
            Assert.AreEqual(2, browser.Contexts.Count);

            await page1.CloseAsync();
            Assert.That(browser.Contexts, Has.Length.EqualTo(1));

            await page2.CloseAsync();
        }

        [PlaywrightTest("browser.spec.ts", "should throw upon second create new page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowUponSecondCreateNewPage()
        {
            var page = await Browser.NewPageAsync();
            var ex = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.Context.NewPageAsync());
            await page.CloseAsync();
            StringAssert.Contains("Please use Browser.NewContextAsync()", ex.Message);
        }

        [PlaywrightTest("browser.spec.ts", "version should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void VersionShouldWork()
        {
            string version = Browser.Version;

            if (TestConstants.IsChromium)
            {
                Assert.That(version, Does.Match("\\d+\\.\\d+\\.\\d+\\.\\d+"));
            }
            else
            {
                Assert.That(version, Does.Match("\\d+\\.\\d+"));
            }
        }
    }
}
