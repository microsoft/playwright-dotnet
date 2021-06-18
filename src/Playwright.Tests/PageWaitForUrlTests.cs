using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync("url => window.location.href = url", Server.Prefix + "/grid.html");
            await Page.WaitForURLAsync("**/grid.html");
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var task = Page.WaitForURLAsync("**/frame.html", new() { Timeout = 2500 });
            await Page.GotoAsync(Server.EmptyPage);
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => task);
            StringAssert.Contains("Timeout 2500ms exceeded.", exception.Message);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with both domcontentloaded and load")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task UrlShouldWorkWithBothDomcontentloadedAndLoad()
        {
            var responseTask = new TaskCompletionSource<bool>();
            Server.SetRoute("/one-style.css", async ctx =>
            {
                if (await responseTask.Task)
                {
                    await ctx.Response.WriteAsync("Some css");
                }
            });

            var waitForRequestTask = Server.WaitForRequest("/one-style.css");
            var navigationTask = Page.GotoAsync(Server.Prefix + "/one-style.html");
            var domContentLoadedTask = Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            var bothFiredTask = Task.WhenAll(
                Page.WaitForURLAsync("**/one-style.html", new() { WaitUntil = WaitUntilState.Load }),
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
            await Page.GotoAsync(Server.EmptyPage);
            await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
            await Page.ClickAsync("a");
            await Page.WaitForURLAsync("**/*#foobar");
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with history.pushState()")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHistoryPushState()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.SetContentAsync(@"
                <a onclick='javascript:replaceState()'>SPA</a>
                <script>
                  function replaceState() { history.replaceState({}, '', '/replaced.html') }
                </script>
            ");
            await Page.ClickAsync("a");
            await Page.WaitForURLAsync("**/replaced.html");
            Assert.AreEqual(Server.Prefix + "/replaced.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with DOM history.back()/history.forward()")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDOMHistoryBackHistoryForward()
        {
            await Page.GotoAsync(Server.EmptyPage);
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
            Assert.AreEqual(Server.Prefix + "/second.html", Page.Url);

            await Task.WhenAll(Page.WaitForURLAsync("**/first.html"), Page.ClickAsync("a#back"));
            Assert.AreEqual(Server.Prefix + "/first.html", Page.Url);

            await Task.WhenAll(Page.WaitForURLAsync("**/second.html"), Page.ClickAsync("a#forward"));
            Assert.AreEqual(Server.Prefix + "/second.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with url match for same document navigations")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
        {
            await Page.GotoAsync(Server.EmptyPage);
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
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);

            await TaskUtils.WhenAll(
                frame.WaitForURLAsync("**/grid.html"),
                frame.EvaluateAsync("url => window.location.href = url", Server.Prefix + "/grid.html"));
        }
    }
}
