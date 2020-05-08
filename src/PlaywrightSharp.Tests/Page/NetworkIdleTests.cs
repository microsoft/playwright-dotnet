using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>network idle</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class NetworkIdleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public NetworkIdleTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should navigate to empty page with networkidle0</playwright-it>
        ///   <playwright-it>should navigate to empty page with networkidle2</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public async Task ShouldNavigateToEmptyPage(WaitUntilNavigation waitUntil)
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage, waitUntil);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 to succeed navigation</playwright-it>
        ///   <playwright-it>should wait for networkidle2 to succeed navigation</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public Task ShouldWaitForToSucceedNavigation(WaitUntilNavigation waitUntil)
            => NetworkIdleTestAsync(Page.MainFrame, waitUntil, () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", waitUntil));

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 to succeed navigation with request from previous navigation</playwright-it>
        ///   <playwright-it>should wait for networkidle2 to succeed navigation with request from previous navigation</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public async Task ShouldWaitForToSucceedNavigationWithRequestFromPreviousNavigation(WaitUntilNavigation waitUntil)
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/foo.js", (request) => Task.CompletedTask);
            await Page.SetContentAsync("<script>fetch('foo.js')</script>");
            await NetworkIdleTestAsync(Page.MainFrame, waitUntil, () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", waitUntil));
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 in waitForNavigation</playwright-it>
        ///   <playwright-it>should wait for networkidle2 in waitForNavigation</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public Task ShouldWaitForInWaitForNavigation(WaitUntilNavigation waitUntil)
            => NetworkIdleTestAsync(Page.MainFrame, waitUntil, () =>
            {
                var task = Page.WaitForNavigationAsync(waitUntil);
                Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html");
                return task;
            });

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 in setContent</playwright-it>
        ///   <playwright-it>should wait for networkidle2 in setContent</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public async Task ShouldWaitForInSetContent(WaitUntilNavigation waitUntil)
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await NetworkIdleTestAsync(
                Page.MainFrame,
                waitUntil,
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", waitUntil),
                true);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-it>should wait for networkidle0 in setContent with request from previous navigation</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWaitForNetworkidle0InSetContentWithRequestFromPreviousNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/foo.js", (request) => Task.CompletedTask);
            await Page.SetContentAsync("<script>fetch('foo.js')</script>");
            await NetworkIdleTestAsync(
                Page.MainFrame,
                WaitUntilNavigation.Networkidle0,
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", WaitUntilNavigation.Networkidle0),
                true);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-it>should wait for networkidle2 in setContent with request from previous navigation</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWaitForNetworkidle2InSetContentWithRequestFromPreviousNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/foo.js", (request) => Task.CompletedTask);
            await Page.SetContentAsync("<script>fetch('foo.js')</script>");
            await NetworkIdleTestAsync(
                Page.MainFrame,
                WaitUntilNavigation.Networkidle2,
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", WaitUntilNavigation.Networkidle2),
                true);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 when navigating iframe</playwright-it>
        ///   <playwright-it>should wait for networkidle2 when navigating iframe</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public async Task ShouldWaitForNetworkidle0WhenNavigatingIframe(WaitUntilNavigation waitUntil)
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.FirstChildFrame();
            await NetworkIdleTestAsync(
                frame,
                waitUntil,
                () => frame.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", waitUntil));
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 in setContent from the child frame</playwright-it>
        ///   <playwright-it>should wait for networkidle2 in setContent from the child frame</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public async Task ShouldWaitForInSetContentFromTheChildFrame(WaitUntilNavigation waitUntil)
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await NetworkIdleTestAsync(
                Page.MainFrame,
                waitUntil, () => Page.SetContentAsync("<iframe src='networkidle.html'></iframe>", waitUntil),
                true);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 from the child frame</playwright-it>
        ///   <playwright-it>should wait for networkidle2 from the child frame</playwright-it>
        /// </playwright-its>
        [Theory]
        [InlineData(WaitUntilNavigation.Networkidle0)]
        [InlineData(WaitUntilNavigation.Networkidle2)]
        public Task ShouldWaitForFromTheChildFrame(WaitUntilNavigation waitUntil)
            => NetworkIdleTestAsync(
                Page.MainFrame,
                waitUntil,
                () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle-frame.html", waitUntil));

        private async Task NetworkIdleTestAsync(IFrame frame, WaitUntilNavigation signal, Func<Task> action, bool isSetContent = false)
        {
            var lastResponseFinished = new Stopwatch();
            var responses = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
            var fetches = new Dictionary<string, TaskCompletionSource<bool>>();

            async Task RequestDelegate(HttpContext context)
            {
                System.Diagnostics.Debug.WriteLine($"Request {context.Request.Path}");
                var taskCompletion = new TaskCompletionSource<bool>();
                responses[context.Request.Path] = taskCompletion;
                fetches[context.Request.Path].TrySetResult(true);
                await taskCompletion.Task;
                lastResponseFinished.Restart();
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("File not found");
            }

            foreach (string url in new[] {
                "/fetch-request-a.js",
                "/fetch-request-b.js",
                "/fetch-request-c.js"})
            {
                fetches[url] = new TaskCompletionSource<bool>();
                Server.SetRoute(url, RequestDelegate);
            }

            var initialFetchResourcesRequested = Task.WhenAll(
                Server.WaitForRequest("/fetch-request-a.js"),
                Server.WaitForRequest("/fetch-request-b.js"),
                Server.WaitForRequest("/fetch-request-c.js")
            );

            var secondFetchResourceRequested = Task.CompletedTask;
            if (signal == WaitUntilNavigation.Networkidle0)
            {
                fetches["/fetch-request-d.js"] = new TaskCompletionSource<bool>();
                Server.SetRoute("/fetch-request-d.js", RequestDelegate);
                secondFetchResourceRequested = Server.WaitForRequest("/fetch-request-d.js");
            }

            var waitForLoadTask = isSetContent ? Task.CompletedTask : frame.WaitForNavigationAsync(WaitUntilNavigation.Load);

            var actionTask = action();

            await waitForLoadTask;
            Assert.False(actionTask.IsCompleted);

            await initialFetchResourcesRequested.WithTimeout();
            Assert.False(actionTask.IsCompleted);

            await Task.WhenAll(
                fetches["/fetch-request-a.js"].Task,
                fetches["/fetch-request-b.js"].Task,
                fetches["/fetch-request-c.js"].Task).WithTimeout();

            // Finishing first response should leave 2 requests alive and trigger networkidle2.
            responses["/fetch-request-a.js"].TrySetResult(true);

            if (signal == WaitUntilNavigation.Networkidle0)
            {
                // Finishing two more responses should trigger the second round.
                responses["/fetch-request-b.js"].TrySetResult(true);
                responses["/fetch-request-c.js"].TrySetResult(true);

                // Wait for the second round to be requested.
                await secondFetchResourceRequested.WithTimeout();
                Assert.False(actionTask.IsCompleted);

                await fetches["/fetch-request-d.js"].Task.WithTimeout();
                responses["/fetch-request-d.js"].TrySetResult(true);
            }
            IResponse navigationResponse = null;
            if (!isSetContent)
            {
                navigationResponse = await (Task<IResponse>)actionTask;
            }
            else
            {
                await actionTask;
            }

            lastResponseFinished.Stop();
            Assert.True(lastResponseFinished.ElapsedMilliseconds > 450);

            if (!isSetContent)
            {
                Assert.Equal(HttpStatusCode.OK, navigationResponse.Status);
            }

            if (signal == WaitUntilNavigation.Networkidle2)
            {
                responses["/fetch-request-b.js"].TrySetResult(true);
                responses["/fetch-request-c.js"].TrySetResult(true);
            }
        }
    }
}
