using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>page-network-idle.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageNetworkIdleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageNetworkIdleTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should navigate to empty page with networkidle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToEmptyPageWithNetworkIdle()
        {
            var response = await Page.GotoAsync(TestConstants.EmptyPage, WaitUntilState.NetworkIdle);
            Assert.Equal((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle to succeed navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldWaitForNetworkIdleToSucceedNavigation()
            => NetworkIdleTestAsync(Page.MainFrame, () => Page.GotoAsync(TestConstants.ServerUrl + "/networkidle.html", WaitUntilState.NetworkIdle));

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle to succeed navigation with request from previous navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForToSucceedNavigationWithRequestFromPreviousNavigation()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            Server.SetRoute("/foo.js", _ => Task.CompletedTask);
            await Page.SetContentAsync("<script>fetch('foo.js')</script>");
            await NetworkIdleTestAsync(Page.MainFrame, () => Page.GotoAsync(TestConstants.ServerUrl + "/networkidle.html", WaitUntilState.NetworkIdle));
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in waitForNavigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldWaitForInWaitForNavigation()
            => NetworkIdleTestAsync(
                Page.MainFrame,
                () =>
                {
                    var task = Page.WaitForNavigationAsync(waitUntil: WaitUntilState.NetworkIdle);
                    Page.GotoAsync(TestConstants.ServerUrl + "/networkidle.html");
                    return task;
                });

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in setContent")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForInSetContent()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", waitUntil: WaitUntilState.NetworkIdle),
                true);
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in setContent with request from previous navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForNetworkIdleInSetContentWithRequestFromPreviousNavigation()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            Server.SetRoute("/foo.js", _ => Task.CompletedTask);
            await Page.SetContentAsync("<script>fetch('foo.js')</script>");
            await NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.SetContentAsync("<script src='networkidle.js'></script>", waitUntil: WaitUntilState.NetworkIdle),
                true);
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle when navigating iframe")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForNetworkIdleWhenNavigatingIframe()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.FirstChildFrame();
            await NetworkIdleTestAsync(
                frame,
                () => frame.GotoAsync(TestConstants.ServerUrl + "/networkidle.html", WaitUntilState.NetworkIdle));
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle in setContent from the child frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForInSetContentFromTheChildFrame()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.SetContentAsync("<iframe src='networkidle.html'></iframe>", waitUntil: WaitUntilState.NetworkIdle),
                true);
        }

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle from the child frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldWaitForFromTheChildFrame()
            => NetworkIdleTestAsync(
                Page.MainFrame,
                () => Page.GotoAsync(TestConstants.ServerUrl + "/networkidle-frame.html", WaitUntilState.NetworkIdle));

        [PlaywrightTest("page-network-idle.spec.ts", "should wait for networkidle from the popup")]
        [Fact]
        public async Task ShouldWaitForNetworkIdleFromThePopup()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync(@"
                <button id=box1 onclick=""window.open('./popup/popup.html')"">Button1</button>
                <button id=box2 onclick=""window.open('./popup/popup.html')"">Button2</button>
                <button id=box3 onclick=""window.open('./popup/popup.html')"">Button3</button>
                <button id=box4 onclick=""window.open('./popup/popup.html')"">Button4</button>
                <button id=box5 onclick=""window.open('./popup/popup.html')"">Button5</button>
            ");

            for (int i = 1; i < 6; i++)
            {
                var popupTask = Page.WaitForEventAsync(PageEvent.Popup);
                await Task.WhenAll(
                    Page.WaitForEventAsync(PageEvent.Popup),
                    Page.ClickAsync("#box" + i)).WithTimeout(TestConstants.DefaultTestTimeout);

                await popupTask.Result.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        private async Task NetworkIdleTestAsync(IFrame frame, Func<Task> action = default, bool isSetContent = false)
        {
            var lastResponseFinished = new Stopwatch();
            var responses = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
            var fetches = new Dictionary<string, TaskCompletionSource<bool>>();

            async Task RequestDelegate(HttpContext context)
            {
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

            var firstFetchResourceRequested = Server.WaitForRequest("/fetch-request-a.js");

            fetches["/fetch-request-b.js"] = new TaskCompletionSource<bool>();
            Server.SetRoute("/fetch-request-b.js", RequestDelegate);
            var secondFetchResourceRequested = Server.WaitForRequest("/fetch-request-b.js");

            var waitForLoadTask = isSetContent ? Task.CompletedTask : frame.WaitForNavigationAsync(waitUntil: WaitUntilState.Load);

            var actionTask = action();

            await waitForLoadTask;
            Assert.False(actionTask.IsCompleted);

            await firstFetchResourceRequested.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.False(actionTask.IsCompleted);

            await fetches["/fetch-request-a.js"].Task.WithTimeout(TestConstants.DefaultTaskTimeout);
            await frame.Page.EvaluateAsync("() => window['fetchSecond']()");

            // Finishing first response should leave 2 requests alive and trigger networkidle2.
            responses["/fetch-request-a.js"].TrySetResult(true);

            // Wait for the second round to be requested.
            await secondFetchResourceRequested.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.False(actionTask.IsCompleted);

            await fetches["/fetch-request-b.js"].Task.WithTimeout(TestConstants.DefaultTaskTimeout);
            responses["/fetch-request-b.js"].TrySetResult(true);

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
            if (!isSetContent)
            {
                Assert.Equal((int)HttpStatusCode.OK, navigationResponse.Status);
            }
        }
    }
}
