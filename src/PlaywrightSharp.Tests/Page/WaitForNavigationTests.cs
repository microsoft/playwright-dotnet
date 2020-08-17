using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForNavigation</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WaitForNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var waitForNavigationResult = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                waitForNavigationResult,
                Page.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html")
            );
            var response = await waitForNavigationResult;
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("grid.html", response.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var waitForNavigationResult = Page.WaitForNavigationAsync(null, "**/frame.html", 5000);

            await Page.GoToAsync(TestConstants.EmptyPage);

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => waitForNavigationResult);

            Assert.Contains("Timeout 5000ms exceeded during page.waitForNavigation.", exception.Message);
            Assert.Contains("waiting for navigation to \"**/frame.html\" until \"load\"", exception.Message);
            Assert.Contains($"navigated to \"{TestConstants.EmptyPage}\"", exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithBothDomcontentloadedAndLoad()
        {
            var responseCompleted = new TaskCompletionSource<bool>();
            Server.SetRoute("/one-style.css", context => responseCompleted.Task);

            var waitForRequestTask = Server.WaitForRequest("/one-style.css");
            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            var domContentLoadedTask = Page.WaitForNavigationAsync(LifecycleEvent.DOMContentLoaded);

            bool bothFired = false;
            var bothFiredTask = TaskUtils.WhenAll(
                domContentLoadedTask,
                Page.WaitForNavigationAsync(LifecycleEvent.Load)).ContinueWith(_ => bothFired = true);

            await waitForRequestTask.WithTimeout();
            await domContentLoadedTask.WithTimeout();
            Assert.False(bothFired);
            responseCompleted.SetResult(true);
            await bothFiredTask.WithTimeout();
            await navigationTask.WithTimeout();
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with clicking on anchor links</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithClickingOnAnchorLinks()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
            var navigationTask = Page.WaitForNavigationAsync();
            await TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.EmptyPage + "#foobar", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with clicking on links which do not commit navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync($"<a href='{TestConstants.HttpsPrefix}/empty.html'>foobar</a>");
            var navigationTask = Page.WaitForNavigationAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => TaskUtils.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            ));
            TestUtils.AssertSSLError(exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with history.pushState()</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithHistoryPushState()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
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

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with history.replaceState()</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithHistoryReplaceState()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
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

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with DOM history.back()/history.forward()</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithDOMHistoryBackAndHistoryForward()
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

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work when subframe issues window.stop()</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenSubframeIssuesWindowStop()
        {
            //This test is slightly different from the one in PW because of .NET Threads (or thanks to .NET Threads)
            var framesNavigated = new List<IFrame>();
            IFrame frame = null;

            var frameAttachedTaskSource = new TaskCompletionSource<IFrame>();
            Page.FrameAttached += (sender, e) =>
            {
                frameAttachedTaskSource.SetResult(e.Frame);
            };
            var frameNavigatedTaskSource = new TaskCompletionSource<bool>();
            Page.FrameNavigated += (sender, e) =>
            {
                if (frame != null)
                {
                    if (e.Frame == frame)
                    {
                        frameNavigatedTaskSource.TrySetResult(true);
                    }
                }
                else
                {
                    framesNavigated.Add(frame);
                }
            };

            Server.SetRoute("/frames/style.css", (context) => Task.CompletedTask);
            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");

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

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with url match</playwright-it>
        [Fact(Skip = "Not supported yet")]
        public void ShouldWorkWithUrlMatch()
        {
            /*
            IResponse response1 = null;
            var response1Task = Page.WaitForNavigationAsync(new WaitForNavigationOptions
            {
                UrlRegEx = new Regex("one-style\\.html")
            }).ContinueWith(t => response1 = t.Result);
            IResponse response2 = null;
            var response2Task = Page.WaitForNavigationAsync(new WaitForNavigationOptions
            {
                UrlRegEx = new Regex("\\/frame.html")
            }).ContinueWith(t => response2 = t.Result);
            IResponse response3 = null;
            var response3Task = Page.WaitForNavigationAsync(new WaitForNavigationOptions
            {
                UrlPredicate = (url) =>
                {
                    var query = new Uri(url).Query.ParseQueryString();
                    return query.ContainsKey("foo") && query["foo"] == "bar";
                }
            }).ContinueWith(t => response3 = t.Result);

            Assert.Null(response1);
            Assert.Null(response2);
            Assert.Null(response3);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Null(response1);
            Assert.Null(response2);
            Assert.Null(response3);
            await Page.GoToAsync(TestConstants.ServerUrl + "/frame.html");
            Assert.Null(response1);
            await response2Task;
            Assert.NotNull(response2);
            Assert.Null(response3);
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await response1Task;
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.Null(response3);
            await Page.GoToAsync(TestConstants.ServerUrl + "/frame.html?foo=bar");
            await response3Task;
            Assert.NotNull(response1);
            Assert.NotNull(response2);
            Assert.NotNull(response3);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.ServerUrl + "/one-style.html", response1.Url);
            Assert.Equal(TestConstants.ServerUrl + "/frame.html", response2.Url);
            Assert.Equal(TestConstants.ServerUrl + "/frame.html?foo=bar", response3.Url);
            */
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with url match for same document navigations</playwright-it>
        [Fact(Skip = "Not supported yet")]
        public void ShouldWorkWithUrlMatchForSameDocumentNavigations()
        {
            /*
            await Page.GoToAsync(TestConstants.EmptyPage);
            bool resolved = false;
            var waitTask = Page.WaitForNavigationAsync(new WaitForNavigationOptions { UrlRegEx = new Regex("third\\.html") })
                .ContinueWith(t => resolved = true);

            Assert.False(resolved);

            await Page.EvaluateAsync("() => history.pushState({}, '', '/first.html')");
            Assert.False(resolved);

            await Page.EvaluateAsync("() => history.pushState({}, '', '/second.html')");
            Assert.False(resolved);

            await Page.EvaluateAsync("() => history.pushState({}, '', '/third.html')");
            await waitTask;
            Assert.True(resolved);
            */
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work for cross-process navigations</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkForCrossProcessNavigations()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var waitTask = Page.WaitForNavigationAsync(LifecycleEvent.DOMContentLoaded);

            string url = TestConstants.CrossProcessHttpPrefix + "/empty.html";
            var gotoTask = Page.GoToAsync(url);
            var response = await waitTask;
            Assert.Equal(url, response.Url);
            Assert.Equal(url, Page.Url);
            Assert.Equal(url, await Page.EvaluateAsync<string>("document.location.href"));
            await gotoTask;
        }
    }
}
