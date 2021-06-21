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

using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ElementHandleScrollIntoViewTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");
            for (int i = 0; i < 11; ++i)
            {
                var button = await Page.QuerySelectorAsync("#btn" + i);
                double before = await button.EvaluateAsync<double>(@"button => {
                    return button.getBoundingClientRect().right - window.innerWidth;
                }");
                Assert.AreEqual(10 * i, before);
                await button.ScrollIntoViewIfNeededAsync();
                double after = await button.EvaluateAsync<double>(@"button => {
                    return button.getBoundingClientRect().right - window.innerWidth;
                }");
                Assert.True(after <= 0);
                await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
            }
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should throw for detached element")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDetachedElement()
        {
            await Page.SetContentAsync("<div>Hello</div>");
            var div = await Page.QuerySelectorAsync("div");
            await div.EvaluateAsync("div => div.remove()");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => div.ScrollIntoViewIfNeededAsync());
            StringAssert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for display:none to become visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDisplayNoneToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"display: none\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.display = 'block'");
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for display:contents to become visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDisplayContentsToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"display: contents\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.display = 'block'");
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for visibility:hidden to become visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisibilityHiddenToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"visibility:hidden\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.visibility = 'visible'");
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for zero-sized element to become visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForZeroSizedElementToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"height:0\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.height = '100px'");
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for nested display:none to become visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForNestedDisplayNoneToBecomeVisible()
        {
            await Page.SetContentAsync("<span style=\"display: none\"><div>Hello</div></span>");
            await TestWaitingAsync(Page, "div => div.parentElement.style.display = 'block'");
        }

        [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should timeout waiting for visible")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWaitingForVisible()
        {
            await Page.SetContentAsync("<div style=\"display: none\">Hello</div>");
            var div = await Page.QuerySelectorAsync("div");
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => div.ScrollIntoViewIfNeededAsync(new() { Timeout = 3000 }));
            StringAssert.Contains("element is not visible", exception.Message);
        }

        private async Task TestWaitingAsync(IPage page, string after)
        {
            var div = await page.QuerySelectorAsync("div");
            var task = div.ScrollIntoViewIfNeededAsync();
            await page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
            Assert.False(task.IsCompleted);
            await div.EvaluateAsync(after);
            await task;
        }
    }
}
