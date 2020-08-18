using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForLoadState</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class WaitForLoadStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForLoadStateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames[1];

            TaskCompletionSource<bool> requestTask = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> routeReachedTask = new TaskCompletionSource<bool>();
            await Page.RouteAsync(TestConstants.ServerUrl + "/one-style.css", async (route, __) =>
            {
                routeReachedTask.TrySetResult(true);
                await requestTask.Task;
                await route.ContinueAsync();
            });

            await frame.GoToAsync(TestConstants.ServerUrl + "/one-style.html", LifecycleEvent.DOMContentLoaded);

            await routeReachedTask.Task;
            var loadTask = frame.WaitForLoadStateAsync();
            await Page.EvaluateAsync("1");
            Assert.False(loadTask.IsCompleted);
            requestTask.TrySetResult(true);
            await loadTask;
        }
    }
}
