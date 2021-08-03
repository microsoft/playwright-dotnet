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

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    [TestClass]
    public class PageBasicTests : PageTestEx
    {
        [PlaywrightTest("page-basic.spec.ts", "should reject all promises when page is closed")]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => TaskUtils.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            StringAssert.Contains(exception.Message, "Protocol error");
        }

        [PlaywrightTest("page-basic.spec.ts", "async stacks should work")]
        [Ignore("We don't need to test this in .NET")]
        public async Task AsyncStacksShouldWork()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Abort(); // is this right?
                return Task.CompletedTask;
            });
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains(exception.StackTrace, nameof(PageBasicTests));
        }

        [PlaywrightTest("page-basic.spec.ts", "Page.press should work")]
        public async Task PagePressShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
            await Page.PressAsync("textarea", "a");
            Assert.AreEqual("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-basic.spec.ts", "Frame.press should work")]
        public async Task FramePressShouldWork()
        {
            await Page.SetContentAsync($"<iframe name =inner src=\"{Server.Prefix}/input/textarea.html\"></iframe>");
            var frame = Page.Frames.Single(f => f.Name == "inner");
            await frame.PressAsync("textarea", "a");
            Assert.AreEqual("a", await frame.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page-basic.spec.ts", "page.frame should respect name")]
        public async Task ShouldReturnTheCorrectBrowserInstance()
        {
            await Page.SetContentAsync("<iframe name=target></iframe>");
            Assert.IsNull(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Name == "target");
            Assert.AreEqual(Page.MainFrame.ChildFrames.First(), frame);
        }

        [PlaywrightTest("page-basic.spec.ts", "page.frame should respect url")]
        public async Task ShouldRespectUrl()
        {
            await Page.SetContentAsync($"<iframe src=\"{Server.EmptyPage}\"></iframe>");
            Assert.IsNull(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Url.Contains("empty"));
            Assert.AreEqual(Page.MainFrame.ChildFrames.First(), frame);
        }

        [PlaywrightTest("page-basic.spec.ts", "should provide access to the opener page")]
        public async Task ShouldProvideAccessToTheOpenerPage()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForPopupAsync(),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var opener = await popupEvent.OpenerAsync();
            Assert.AreEqual(Page, opener);
        }

        [PlaywrightTest("page-basic.spec.ts", "should return null if parent page has been closed")]
        public async Task ShouldReturnNullIfParentPageHasBeenClosed()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForPopupAsync(),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            await Page.CloseAsync();
            var opener = await popupEvent.OpenerAsync();
            Assert.IsNull(opener);
        }

        [PlaywrightTest("page-basic.spec.ts", "should return the page title")]
        public async Task ShouldReturnThePageTitle()
        {
            await Page.GotoAsync(Server.Prefix + "/title.html");
            Assert.AreEqual("Woof-Woof", await Page.TitleAsync());
        }

        [PlaywrightTest("page-basic.spec.ts", "page.url should work")]
        public async Task PageUrlShouldWork()
        {
            Assert.AreEqual("about:blank", Page.Url);
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-basic.spec.ts", "should include hashes")]
        public async Task ShouldIncludeHashes()
        {
            await Page.GotoAsync(Server.EmptyPage + "#hash");
            Assert.AreEqual(Server.EmptyPage + "#hash", Page.Url);
            await Page.EvaluateAsync("() => window.location.hash = 'dynamic'");
            Assert.AreEqual(Server.EmptyPage + "#dynamic", Page.Url);
        }

        [PlaywrightTest("page-basic.spec.ts", "should fail with error upon disconnect")]
        public async Task ShouldFailWithErrorUponDisconnect()
        {
            var task = Page.WaitForDownloadAsync();
            await Page.CloseAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => task);
            StringAssert.Contains(exception.Message, "Page closed");
        }

        [PlaywrightTest("page-basic.spec.ts", "should have sane user agent")]
        public async Task ShouldHaveASaneUserAgent()
        {
            string userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
            string[] parts = Regex.Split(userAgent, "[()]").Select(t => t.Trim()).ToArray();

            Assert.AreEqual("Mozilla/5.0", parts[0]);

            if (TestConstants.IsFirefox)
            {
                string[] engineBrowser = parts[2].Split(' ');
                StringAssert.StartsWith(engineBrowser[0], "Gecko");
                StringAssert.StartsWith(engineBrowser[1], "Firefox");
            }
            else
            {
                StringAssert.StartsWith(parts[2], "AppleWebKit/");
                Assert.AreEqual("KHTML, like Gecko", parts[3]);
                string[] engineBrowser = parts[4].Split(' ');
                StringAssert.StartsWith(engineBrowser[1], "Safari/");

                if (TestConstants.IsChromium)
                {
                    StringAssert.Contains(engineBrowser[0], "Chrome/");
                }
                else
                {
                    StringAssert.StartsWith(engineBrowser[0], "Version");
                }
            }
        }

        [PlaywrightTest("page-basic.spec.ts", "should work with window.close")]
        public async Task ShouldWorkWithWindowClose()
        {
            var newPageTask = Page.WaitForPopupAsync();
            await Page.EvaluateAsync<string>("() => window['newPage'] = window.open('about:blank')");
            var newPage = await newPageTask;
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (_, _) => closedTsc.SetResult(true);
            await Page.EvaluateAsync<string>("() => window['newPage'].close()");
            await closedTsc.Task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should work with page.close")]
        public async Task ShouldWorkWithPageClose()
        {
            var newPage = await Context.NewPageAsync();
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (_, _) => closedTsc.SetResult(true);
            await newPage.CloseAsync();
            await closedTsc.Task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should fire load when expected")]
        public Task ShouldFireLoadWhenExpected()
        {
            var loadEvent = new TaskCompletionSource<bool>();
            Page.Load += (_, _) => loadEvent.TrySetResult(true);

            return TaskUtils.WhenAll(
                loadEvent.Task,
                Page.GotoAsync("about:blank")
            );
        }

        [PlaywrightTest("page-basic.spec.ts", "should fire domcontentloaded when expected")]
        public async Task ShouldFireDOMcontentloadedWhenExpected()
        {
            var task = Page.GotoAsync("about:blank");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await task;
        }

        [PlaywrightTest("page-basic.spec.ts", "should set the page close state")]
        public async Task ShouldSetThePageCloseState()
        {
            var newPage = await Context.NewPageAsync();
            Assert.IsFalse(newPage.IsClosed);
            await newPage.CloseAsync();
            Assert.IsTrue(newPage.IsClosed);
        }

        [PlaywrightTest("page-basic.spec.ts", "should terminate network waiters")]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => TaskUtils.WhenAll(
                newPage.WaitForRequestAsync(Server.EmptyPage),
                newPage.WaitForResponseAsync(Server.EmptyPage),
                newPage.CloseAsync()
            ));
            for (int i = 0; i < 2; i++)
            {
                string message = exception.Message;
                StringAssert.Contains(message, "Page closed");
                Assert.IsFalse(message.Contains("Timeout"));
            }
        }

        [PlaywrightTest("page-basic.spec.ts", "should be callable twice")]
        public async Task ShouldBeCallableTwice()
        {
            var newPage = await Context.NewPageAsync();
            await TaskUtils.WhenAll(
                newPage.CloseAsync(),
                newPage.CloseAsync());

            await newPage.CloseAsync();
        }

        [PlaywrightTest("page-basic.spec.ts", "should not be visible in context.pages")]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            Assert.That.Collection(Context.Pages).Contains(newPage);
            await newPage.CloseAsync();
            Assert.That.Collection(Context.Pages).DoesNotContain(newPage);
        }

        [PlaywrightTest("page-basic.spec.ts", "")]
        public async Task DragAndDropShouldWork()
        {
            var page = await Context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await page.DragAndDropAsync("#source", "#target");
            Assert.IsTrue(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"));
        }
    }
}
