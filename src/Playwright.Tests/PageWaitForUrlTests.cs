using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageWaitForUrlTests : PageTestEx
    {
        [PlaywrightTest("page-wait-for-url.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html");
            await Page.WaitForURLAsync("**/grid.html");
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var task = Page.WaitForURLAsync("**/frame.html", new PageWaitForURLOptions { Timeout = 2500 });
            await Page.GotoAsync(TestConstants.EmptyPage);
            var exception = await AssertThrowsAsync<TimeoutException>(() => task);
            StringAssert.Contains("Timeout 2500ms exceeded.", exception.Message);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with both domcontentloaded and load")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBothDomcontentloadedAndLoad()
        {
            var responseTask = new TaskCompletionSource<bool>();
            HttpServer.Server.SetRoute("/one-style.css", async (ctx) =>
            {
                if (await responseTask.Task)
                {
                    await ctx.Response.WriteAsync("Some css");
                }
            });

            var waitForRequestTask = HttpServer.Server.WaitForRequest("/one-style.css");
            var navigationTask = Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            var domContentLoadedTask = Page.WaitForURLAsync("**/one-style.html", new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            var bothFiredTask = Task.WhenAll(
                Page.WaitForURLAsync("**/one-style.html", new PageWaitForURLOptions { WaitUntil = WaitUntilState.Load }),
                domContentLoadedTask);

            await waitForRequestTask;
            await domContentLoadedTask;
            Assert.False(bothFiredTask.IsCompleted);
            responseTask.TrySetResult(true);
            await bothFiredTask;
            await navigationTask;
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with clicking on anchor links")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithClickingOnAnchorLinks()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
            await Page.ClickAsync("a");
            await Page.WaitForURLAsync("**/*#foobar");
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with history.pushState()")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHistoryPushState()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync(@"
                <a onclick='javascript:replaceState()'>SPA</a>
                <script>
                  function replaceState() { history.replaceState({}, '', '/replaced.html') }
                </script>
            ");
            await Page.ClickAsync("a");
            await Page.WaitForURLAsync("**/replaced.html");
            Assert.AreEqual(TestConstants.ServerUrl + "/replaced.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with DOM history.back()/history.forward()")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDOMHistoryBackHistoryForward()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync(@"
                <a id=back onclick='javascript:goBack()'>back</a>
                <a id=forward onclick='javascript:goForward()'>forward</a>
                <script>
                  function goBack() { history.back(); }
                  function goForward() { history.forward(); }
                  history.pushState({}, '', '/first.html');
                  history.pushState({}, '', '/second.html');
                </script>
            ");
            Assert.AreEqual(TestConstants.ServerUrl + "/second.html", Page.Url);

            await Task.WhenAll(Page.WaitForURLAsync("**/first.html"), Page.ClickAsync("a#back"));
            Assert.AreEqual(TestConstants.ServerUrl + "/first.html", Page.Url);

            await Task.WhenAll(Page.WaitForURLAsync("**/second.html"), Page.ClickAsync("a#forward"));
            Assert.AreEqual(TestConstants.ServerUrl + "/second.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with url match for same document navigations")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var waitPromise = Page.WaitForURLAsync(new Regex("third\\.html"));
            Assert.False(waitPromise.IsCompleted);

            await Page.EvaluateAsync(@"() => {
                history.pushState({}, '', '/first.html');
            }");
            Assert.False(waitPromise.IsCompleted);

            await Page.EvaluateAsync(@"() => {
                history.pushState({}, '', '/second.html');
            }");
            Assert.False(waitPromise.IsCompleted);

            await Page.EvaluateAsync(@"() => {
                history.pushState({}, '', '/third.html');
            }");
            await waitPromise;
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work on frame")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkOnFrame()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);

            await frame.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html");
            await frame.WaitForURLAsync("**/grid.html");
        }
    }
}
