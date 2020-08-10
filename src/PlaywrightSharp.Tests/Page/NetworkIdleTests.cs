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
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>network idle</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class NetworkIdleTests : PlaywrightSharpPageBaseTest
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
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNavigateToEmptyPage()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage, LifecycleEvent.Networkidle);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 to succeed navigation</playwright-it>
        ///   <playwright-it>should wait for networkidle2 to succeed navigation</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldWaitForToSucceedNavigation()
            => NetworkIdleTestAsync(Page.MainFrame, () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", LifecycleEvent.Networkidle));

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 to succeed navigation with request from previous navigation</playwright-it>
        ///   <playwright-it>should wait for networkidle2 to succeed navigation with request from previous navigation</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForToSucceedNavigationWithRequestFromPreviousNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRoute("/foo.js", (request) => Task.CompletedTask);
            await Page.SetContentAsync("<script>fetch('foo.js')</script>");
            await NetworkIdleTestAsync(Page.MainFrame, () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", LifecycleEvent.Networkidle));
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 in waitForNavigation</playwright-it>
        ///   <playwright-it>should wait for networkidle2 in waitForNavigation</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldWaitForInWaitForNavigation()
            => NetworkIdleTestAsync(
                Page.MainFrame,
                () =>
                {
                    var task = Page.WaitForNavigationAsync(LifecycleEvent.Networkidle);
                    Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html");
                    return task;
                });

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 in setContent</playwright-it>
        ///   <playwright-it>should wait for networkidle2 in setContent</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForInSetContent()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", LifecycleEvent.Networkidle),
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
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", LifecycleEvent.Networkidle),
                true);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 when navigating iframe</playwright-it>
        ///   <playwright-it>should wait for networkidle2 when navigating iframe</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForNetworkidle0WhenNavigatingIframe()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.FirstChildFrame();
            await NetworkIdleTestAsync(
                frame,
                () => frame.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", LifecycleEvent.Networkidle));
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 in setContent from the child frame</playwright-it>
        ///   <playwright-it>should wait for networkidle2 in setContent from the child frame</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWaitForInSetContentFromTheChildFrame()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.SetContentAsync("<iframe src='networkidle.html'></iframe>", LifecycleEvent.Networkidle),
                true);
        }

        /// <playwright-file>navigation.spec.js</playwright-file>
        /// <playwright-describe>network idle</playwright-describe>
        /// <playwright-its>
        ///   <playwright-it>should wait for networkidle0 from the child frame</playwright-it>
        ///   <playwright-it>should wait for networkidle2 from the child frame</playwright-it>
        /// </playwright-its>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldWaitForFromTheChildFrame()
            => NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle-frame.html", LifecycleEvent.Networkidle));

        private async Task NetworkIdleTestAsync(IFrame frame, Func<Task> action, bool isSetContent = false)
        {
            var lastResponseFinished = new Stopwatch();
            var responses = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
            var fetches = new Dictionary<string, TaskCompletionSource<bool>>();

            async Task RequestDelegate(HttpContext context)
            {
                Debug.WriteLine($"Request {context.Request.Path}");
                var taskCompletion = new TaskCompletionSource<bool>();
                responses[context.Request.Path] = taskCompletion;
                fetches[context.Request.Path].TrySetResult(true);
                await taskCompletion.Task;
                lastResponseFinished.Restart();
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("File not found");
            }

            fetches["/fetch-request-a.js"] = new TaskCompletionSource<bool>();
            Server.SetRoute("/fetch-request-a.js", RequestDelegate);

            var initialFetchResourcesRequested = Server.WaitForRequest("/fetch-request-a.js");

            fetches["/fetch-request-d.js"] = new TaskCompletionSource<bool>();
            Server.SetRoute("/fetch-request-d.js", RequestDelegate);
            var secondFetchResourceRequested = Server.WaitForRequest("/fetch-request-d.js");

            var waitForLoadTask = isSetContent ? Task.CompletedTask : frame.WaitForNavigationAsync(LifecycleEvent.Load);

            var actionTask = action();

            await waitForLoadTask;
            Assert.False(actionTask.IsCompleted);

            await initialFetchResourcesRequested.WithTimeout();
            Assert.False(actionTask.IsCompleted);

            await fetches["/fetch-request-a.js"].Task.WithTimeout();

            // Finishing first response should leave 2 requests alive and trigger networkidle2.
            responses["/fetch-request-a.js"].TrySetResult(true);

            // Wait for the second round to be requested.
            await secondFetchResourceRequested.WithTimeout();
            Assert.False(actionTask.IsCompleted);

            await fetches["/fetch-request-d.js"].Task.WithTimeout();
            responses["/fetch-request-d.js"].TrySetResult(true);

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
        }
    }
}
