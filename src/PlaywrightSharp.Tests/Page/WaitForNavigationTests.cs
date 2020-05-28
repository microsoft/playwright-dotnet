using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForNavigation</playwright-describe>
    [Trait("Category", "chromium")]
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
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var waitForNavigationResult = Page.WaitForNavigationAsync();
            await Task.WhenAll(
                waitForNavigationResult,
                Page.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html")
            );
            var response = await waitForNavigationResult;
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("grid.html", response.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWorkWithBothDomcontentloadedAndLoad()
        {
            var responseCompleted = new TaskCompletionSource<bool>();
            Server.SetRoute("/one-style.css", context => responseCompleted.Task);

            var waitForRequestTask = Server.WaitForRequest("/one-style.css");
            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            var domContentLoadedTask = Page.WaitForNavigationAsync(new WaitForNavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }
            });

            bool bothFired = false;
            var bothFiredTask = Page.WaitForNavigationAsync(new WaitForNavigationOptions
            {
                WaitUntil = new[]
                {
                    WaitUntilNavigation.Load,
                    WaitUntilNavigation.DOMContentLoaded
                }
            }).ContinueWith(_ => bothFired = true);

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
        [Retry]
        public async Task ShouldWorkWithClickingOnAnchorLinks()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<a href='#foobar'>foobar</a>");
            var navigationTask = Page.WaitForNavigationAsync();
            await Task.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.EmptyPage + "#foobar", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with clicking on links which do not commit navigation</playwright-it>
        [Retry]
        public async Task ShouldWorkWithClickingOnLinksWhichDoNotCommitNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync($"<a href='{TestConstants.HttpsPrefix}/empty.html'>foobar</a>");
            var navigationTask = Page.WaitForNavigationAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Task.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            ));
            TestUtils.AssertSSLError(exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with history.pushState()</playwright-it>
        [Retry]
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
            await Task.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/wow.html", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with history.replaceState()</playwright-it>
        [Retry]
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
            await Task.WhenAll(
                navigationTask,
                Page.ClickAsync("a")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/replaced.html", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with DOM history.back()/history.forward()</playwright-it>
        [Retry]
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
            await Task.WhenAll(
                navigationTask,
                Page.ClickAsync("a#back")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);
            navigationTask = Page.WaitForNavigationAsync();
            await Task.WhenAll(
                navigationTask,
                Page.ClickAsync("a#forward")
            );
            Assert.Null(await navigationTask);
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work when subframe issues window.stop()</playwright-it>
        [Retry]
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
            await Task.WhenAll(
                frame.EvaluateAsync("() => window.stop()"),
                navigationTask
            );
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with url match</playwright-it>
        [Retry]
        public async Task ShouldWorkWithUrlMatch()
        {
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
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForNavigation</playwright-describe>
        ///<playwright-it>should work with url match for same document navigations</playwright-it>
        [Retry]
        public async Task ShouldWorkWithUrlMatchForSameDocumentNavigations()
        {
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
        }
    }
}
