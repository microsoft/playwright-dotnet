using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForUrlTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForUrlTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html");
            await Page.WaitForURLAsync("**/grid.html");
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var task = Page.WaitForURLAsync("**/frame.html", timeout: 2500);
            await Page.GoToAsync(TestConstants.EmptyPage);
            var exception = await Assert.ThrowsAsync<TimeoutException>(() => task);
            Assert.Contains("Timeout 2500ms exceeded.", exception.Message);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with both domcontentloaded and load")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBothDomcontentloadedAndLoad()
        {
            var responseTask = new TaskCompletionSource<bool>();
            Server.SetRoute("/one-style.css", async (ctx) =>
            {
                if (await responseTask.Task)
                {
                    await ctx.Response.WriteAsync("Some css");
                }
            });

            var waitForRequestTask = Server.WaitForRequest("/one-style.css");
            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            var domContentLoadedTask = Page.WaitForURLAsync("**/one-style.html", waitUntil: WaitUntilState.DOMContentLoaded);
            var bothFiredTask = Task.WhenAll(
                Page.WaitForURLAsync("**/one-style.html", waitUntil: WaitUntilState.Load),
                domContentLoadedTask);

            await waitForRequestTask;
            await domContentLoadedTask;
            Assert.False(bothFiredTask.IsCompleted);
            responseTask.TrySetResult(true);
            await bothFiredTask;
            await navigationTask;
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with clicking on anchor links")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithClickingOnAnchorLinks()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
            await Page.ClickAsync("a");
            await Page.WaitForURLAsync("**/*#foobar");
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with history.pushState()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHistoryPushState()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync(@"
                <a onclick='javascript:replaceState()'>SPA</a>
                <script>
                  function replaceState() { history.replaceState({}, '', '/replaced.html') }
                </script>
            ");
            await Page.ClickAsync("a");
            await Page.WaitForURLAsync("**/replaced.html");
            Assert.Equal(TestConstants.ServerUrl + "/replaced.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with DOM history.back()/history.forward()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDOMHistoryBackHistoryForward()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
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
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);

            await Page.ClickAsync("a#back");
            await Page.WaitForURLAsync("**/first.html");
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);

            await Page.ClickAsync("a#forward");
            await Page.WaitForURLAsync("**/second.html");
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-url.spec.ts", "should work with url match for same document navigations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkOnFrame()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);

            await frame.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html");
            await frame.WaitForURLAsync("**/grid.html");
        }
    }
}
