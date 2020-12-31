using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Popup
{

    ///<playwright-file>popup.spec.js</playwright-file>
    ///<playwright-describe>Link navigation</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LinkNavigationTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public LinkNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Link navigation</playwright-describe>
        ///<playwright-it>should inherit user agent from browser context</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInheritUserAgentFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { UserAgent = "hey" });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var requestTcs = new TaskCompletionSource<string>();
            _ = Server.WaitForRequest("/popup/popup.html", request => requestTcs.TrySetResult(request.Headers["user-agent"]));

            await page.SetContentAsync("<a target=_blank rel=noopener href=\"/popup/popup.html\">link</a>");
            var popupTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

            await popupTask.Result.Page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            string userAgent = await popupTask.Result.Page.EvaluateAsync<string>("() => window.initialUserAgent");
            await requestTcs.Task;

            Assert.Equal("hey", userAgent);
            Assert.Equal("hey", requestTcs.Task.Result);
        }

        ///<playwright-file>popup.spec.js</playwright-file>
        ///<playwright-describe>Link navigation</playwright-describe>
        ///<playwright-it>should respect routes from browser context</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectRoutesFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await page.SetContentAsync("<a target=_blank rel=noopener href=\"empty.html\">link</a>");
            bool intercepted = false;

            await context.RouteAsync("**/empty.html", (route, _) =>
            {
                route.ContinueAsync();
                intercepted = true;
            });

            var popupTask = context.WaitForEventAsync(ContextEvent.Page);
            await TaskUtils.WhenAll(popupTask, page.ClickAsync("a"));

            Assert.True(intercepted);
        }
    }
}
