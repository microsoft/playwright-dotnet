using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>network idle</playwright-describe>
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
            => NetworkIdleTest(Page.MainFrame, waitUntil, () => Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html", waitUntil));

        public async Task NetworkIdleTest(IFrame frame, WaitUntilNavigation signal, Func<Task<IResponse>> func, bool isSetContent = false)
        {
            var lastResponseFinished = new Stopwatch();
            var responses = new Dictionary<string, TaskCompletionSource<Func<HttpResponse, Task>>>();
            var fetches = new Dictionary<string, TaskCompletionSource<bool>>();

            Func<HttpResponse, Task> finishResponse = response =>
            {
                lastResponseFinished.Restart();
                response.StatusCode = 404;
                return response.WriteAsync("File not found");
            };
            RequestDelegate requestDelegate = async context =>
            {
                var taskCompletion = new TaskCompletionSource<Func<HttpResponse, Task>>();
                responses[context.Request.Path] = taskCompletion;
                fetches[context.Request.Path].SetResult(true);
                var actionResponse = await taskCompletion.Task;
                await actionResponse(context.Response).WithTimeout();
            };
            foreach (var url in new[] {
                "/fetch-request-a.js",
                "/fetch-request-b.js",
                "/fetch-request-c.js"})
            {
                fetches[url] = new TaskCompletionSource<bool>();
                Server.SetRoute(url, requestDelegate);
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
                Server.SetRoute("/fetch-request-d.js", requestDelegate);
                secondFetchResourceRequested = Server.WaitForRequest("/fetch-request-d.js");
            }

            var pageLoaded = new TaskCompletionSource<bool>();
            if (isSetContent)
            {
                pageLoaded.TrySetResult(true);
            }
            else
            {
                void WaitPageLoad(object sender, EventArgs e)
                {
                    pageLoaded.SetResult(true);
                    Page.Load -= WaitPageLoad;
                }
                Page.Load += WaitPageLoad;
            }

            var navigationFinished = false;
            var navigationTask = func();
            _ = navigationTask.ContinueWith(t => navigationFinished = true);

            await pageLoaded.Task.WithTimeout();
            Assert.False(navigationFinished);

            await initialFetchResourcesRequested.WithTimeout();
            Assert.False(navigationFinished);

            await Task.WhenAll(
                fetches["/fetch-request-a.js"].Task,
                fetches["/fetch-request-b.js"].Task,
                fetches["/fetch-request-c.js"].Task).WithTimeout();

            // Finishing first response should leave 2 requests alive and trigger networkidle2.
            responses["/fetch-request-a.js"].TrySetResult(finishResponse);

            if (signal == WaitUntilNavigation.Networkidle0)
            {
                // Finishing two more responses should trigger the second round.
                responses["/fetch-request-b.js"].TrySetResult(finishResponse);
                responses["/fetch-request-c.js"].TrySetResult(finishResponse);

                // Wait for the second round to be requested.
                await secondFetchResourceRequested.WithTimeout();
                Assert.False(navigationFinished);

                await fetches["/fetch-request-d.js"].Task.WithTimeout();
            }
            var navigationResponse = await navigationTask;
            lastResponseFinished.Stop();
            Assert.True(lastResponseFinished.ElapsedMilliseconds < 450);

            if (!isSetContent)
            {
                Assert.Equal(HttpStatusCode.OK, navigationResponse.Status);
            }

            if (signal == WaitUntilNavigation.Networkidle2)
            {
                responses["/fetch-request-b.js"].TrySetResult(finishResponse);
                responses["/fetch-request-c.js"].TrySetResult(finishResponse);
            }
        }
    }
}