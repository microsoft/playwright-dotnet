using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var waitForNavigationResult = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                waitForNavigationResult,
                Page.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html")
            );
            var response = await waitForNavigationResult;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("grid.html", response.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var waitForNavigationResult = Page.WaitForNavigationAsync("**/frame.html", timeout: 5000);

            await Page.GotoAsync(TestConstants.EmptyPage);

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => waitForNavigationResult);

            Assert.Contains("Timeout 5000ms exceeded", exception.Message);
            Assert.Contains("waiting for navigation to \"**/frame.html\" until \"Load\"", exception.Message);
            Assert.Contains($"navigated to \"{TestConstants.EmptyPage}\"", exception.Message);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with both domcontentloaded and load")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBothDomcontentloadedAndLoad()
        {
            var responseCompleted = new TaskCompletionSource<bool>();
            Server.SetRoute("/one-style.css", _ => responseCompleted.Task);

            var waitForRequestTask = Server.WaitForRequest("/one-style.css");
            var navigationTask = Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            var domContentLoadedTask = Page.WaitForNavigationAsync(WaitUntilState.DOMContentLoaded);

            bool bothFired = false;
            var bothFiredTask = TaskUtils.WhenAll(
                domContentLoadedTask,
                Page.WaitForNavigationAsync(WaitUntilState.Load)).ContinueWith(_ => bothFired = true);

            await waitForRequestTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            await domContentLoadedTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.False(bothFired);
            responseCompleted.SetResult(true);
            await bothFiredTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            await navigationTask.WithTimeout(TestConstants.DefaultTaskTimeout);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with clicking on anchor links")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithClickingOnAnchorLinks()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
            var navigationTask = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.EmptyPage + "#foobar", Page.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with clicking on links which do not commit navigation")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldWorkWithClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync($"<a href='{TestConstants.HttpsPrefix}/empty.html'>foobar</a>");
            var navigationTask = Page.WaitForNavigationAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            ));
            TestUtils.AssertSSLError(exception.Message);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with history.pushState()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHistoryPushState()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync(@"
              <a onclick='javascript:pushState()'>SPA</a>
              <script>
                function pushState() { history.pushState({}, '', 'wow.html') }
              </script>
            ");
            var navigationTask = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/wow.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with history.replaceState()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHistoryReplaceState()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync(@"
              <a onclick='javascript:pushState()'>SPA</a>
              <script>
                function pushState() { history.pushState({}, '', 'replaced.html') }
              </script>
            ");
            var navigationTask = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/replaced.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with DOM history.back()/history.forward()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDOMHistoryBackAndHistoryForward()
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
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);
            var navigationTask = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a#back")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);
            navigationTask = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a#forward")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work when subframe issues window.stop()")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenSubframeIssuesWindowStop()
        {
            //This test is slightly different from the one in PW because of .NET Threads (or thanks to .NET Threads)
            var framesNavigated = new List<IFrame>();
            IFrame frame = null;

            var frameAttachedTaskSource = new TaskCompletionSource<IFrame>();
            Page.FrameAttached += (_, e) =>
            {
                frameAttachedTaskSource.SetResult(e);
            };
            var frameNavigatedTaskSource = new TaskCompletionSource<bool>();
            Page.FrameNavigated += (_, e) =>
            {
                if (frame != null)
                {
                    if (e == frame)
                    {
                        frameNavigatedTaskSource.TrySetResult(true);
                    }
                }
                else
                {
                    framesNavigated.Add(frame);
                }
            };

            Server.SetRoute("/frames/style.css", _ => Task.CompletedTask);
            var navigationTask = Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");

            frame = await frameAttachedTaskSource.Task;

            if (framesNavigated.Contains(frame))
            {
                frameNavigatedTaskSource.TrySetResult(true);
            }

            await frameNavigatedTaskSource.Task;
            await TaskUtils.WhenAll(
                frame.EvaluateAsync("() => window.stop()"),
                navigationTask
            );
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with url match")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatch()
        {
            IResponse response1 = null;
            var response1Task = Page.WaitForNavigationAsync(new Regex("one-style\\.html")).ContinueWith(t => response1 = t.Result);
            IResponse response2 = null;
            var response2Task = Page.WaitForNavigationAsync(new Regex("\\/frame.html")).ContinueWith(t => response2 = t.Result);
            IResponse response3 = null;
            var response3Task = Page.WaitForNavigationAsync(
                (url) =>
                {
                    var query = new Uri(url).Query.ParseQueryString();
                    return query.ContainsKey("foo") && query["foo"] == "bar";
                }).ContinueWith(t => response3 = t.Result);

            Assert.Null(response1);
            Assert.Null(response2);
            Assert.Null(response3);
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Null(response1);
            Assert.Null(response2);
            Assert.Null(response3);
            await Page.GotoAsync(TestConstants.ServerUrl + "/frame.html");
            Assert.Null(response1);
            await response2Task;
            Assert.NotNull(response2);
            Assert.Null(response3);
            await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            await response1Task;
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.Null(response3);
            await Page.GotoAsync(TestConstants.ServerUrl + "/frame.html?foo=bar");
            await response3Task;
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.NotNull(response3);
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.ServerUrl + "/one-style.html", response1.Url);
            Assert.Equal(TestConstants.ServerUrl + "/frame.html", response2.Url);
            Assert.Equal(TestConstants.ServerUrl + "/frame.html?foo=bar", response3.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work with url match for same document navigations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            bool resolved = false;
            var waitTask = Page.WaitForNavigationAsync(new Regex("third\\.html")).ContinueWith(_ => resolved = true);

            Assert.False(resolved);

            await Page.EvaluateAsync("() => history.pushState({}, '', '/first.html')");
            Assert.False(resolved);

            await Page.EvaluateAsync("() => history.pushState({}, '', '/second.html')");
            Assert.False(resolved);

            await Page.EvaluateAsync("() => history.pushState({}, '', '/third.html')");
            await waitTask;
            Assert.True(resolved);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work for cross-process navigations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCrossProcessNavigations()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var waitTask = Page.WaitForNavigationAsync(WaitUntilState.DOMContentLoaded);

            string url = TestConstants.CrossProcessHttpPrefix + "/empty.html";
            var gotoTask = Page.GotoAsync(url);
            var response = await waitTask;
            Assert.Equal(url, response.Url);
            Assert.Equal(url, Page.Url);
            Assert.Equal(url, await Page.EvaluateAsync<string>("document.location.href"));
            await gotoTask;
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should work on frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkOnFrame()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);
            var (response, _) = await TaskUtils.WhenAll(
                frame.WaitForNavigationAsync(),
                frame.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html")
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("grid.html", response.Url);
            Assert.Same(frame, response.Frame);
            Assert.Contains("/frames/one-frame.html", Page.Url);
        }

        [PlaywrightTest("page-wait-for-navigation.spec.ts", "should fail when frame detaches")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenFrameDetaches()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);
            Server.SetRoute("/empty.html", _ => Task.Delay(6000));
            var exceptionTask = Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => frame.WaitForNavigationAsync());

            try
            {
                await TaskUtils.WhenAll(
                    exceptionTask,
                    frame.EvaluateAsync("window.location.href = '/empty.html'"),
                    Page.EvaluateAsync("setTimeout(() => document.querySelector(\"iframe\").remove())"));
            }
            catch
            { }

            Assert.Contains("waiting for navigation until \"Load\"", exceptionTask.Result.Message);
            Assert.Contains("frame was detached", exceptionTask.Result.Message);
        }
    }
}
