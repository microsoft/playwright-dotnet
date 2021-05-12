using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright.Contracts.Constants;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextRouteTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextRouteTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should intercept")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIntercept()
        {
            bool intercepted = false;

            await using var context = await Browser.NewContextAsync();
            IPage page = null;

            await context.RouteAsync("**/empty.html", (route) =>
            {
                intercepted = true;

                Assert.Contains("empty.html", route.Request.Url);
                Assert.False(string.IsNullOrEmpty(route.Request.GetHeaderValue("user-agent")));
                Assert.Equal(HttpMethod.Get.Method, route.Request.Method);
                Assert.Null(route.Request.PostData);
                Assert.True(route.Request.IsNavigationRequest);
                Assert.Equal(ResourceTypes.Document, route.Request.ResourceType, false);
                Assert.Same(page.MainFrame, route.Request.Frame);
                Assert.Equal("about:blank", page.MainFrame.Url);

                route.ResumeAsync();
            });

            page = await context.NewPageAsync();
            var response = await page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should unroute")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnroute()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var intercepted = new List<int>();

            Action<IRoute> handler1 = (route) =>
            {
                intercepted.Add(1);
                route.ResumeAsync();
            };

            await context.RouteAsync("**/empty.html", handler1);
            await context.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(2);
                route.ResumeAsync();
            });

            await context.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(3);
                route.ResumeAsync();
            });

            await context.RouteAsync("**/*", (route) =>
            {
                intercepted.Add(4);
                route.ResumeAsync();
            });

            await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(new List<int>() { 1 }, intercepted);

            intercepted.Clear();
            await context.UnrouteAsync("**/empty.html", handler1);
            await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(new List<int>() { 2 }, intercepted);

            intercepted.Clear();
            await context.UnrouteAsync("**/empty.html");
            await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(new List<int>() { 4 }, intercepted);
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should yield to page.route")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldYieldToPageRoute()
        {
            await using var context = await Browser.NewContextAsync();
            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.FulfillAsync(HttpStatusCode.OK, "context");
            });

            var page = await context.NewPageAsync();
            await page.RouteAsync("**/empty.html", (route) =>
            {
                route.FulfillAsync(HttpStatusCode.OK, "page");
            });

            var response = await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal("page", await response.TextAsync());
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should fall back to context.route")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFallBackToContextRoute()
        {
            await using var context = await Browser.NewContextAsync();
            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.FulfillAsync(HttpStatusCode.OK, "context");
            });

            var page = await context.NewPageAsync();
            await page.RouteAsync("**/non-empty.html", (route) =>
            {
                route.FulfillAsync(HttpStatusCode.OK, "page");
            });

            var response = await page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal("context", await response.TextAsync());
        }
    }
}
