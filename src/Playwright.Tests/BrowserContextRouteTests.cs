using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextRouteTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-route.spec.ts", "should intercept")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIntercept()
        {
            bool intercepted = false;

            await using var context = await Browser.NewContextAsync();
            IPage page = null;

            await context.RouteAsync("**/empty.html", (route) =>
            {
                intercepted = true;

                StringAssert.Contains("empty.html", route.Request.Url);
                Assert.False(string.IsNullOrEmpty(route.Request.Headers["user-agent"]));
                Assert.AreEqual(HttpMethod.Get.Method, route.Request.Method);
                Assert.Null(route.Request.PostData);
                Assert.True(route.Request.IsNavigationRequest);
                Assert.AreEqual("document", route.Request.ResourceType);
                Assert.AreEqual(page.MainFrame, route.Request.Frame);
                Assert.AreEqual("about:blank", page.MainFrame.Url);

                route.ContinueAsync();
            });

            page = await context.NewPageAsync();
            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should unroute")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnroute()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var intercepted = new List<int>();

            Action<IRoute> handler1 = (route) =>
            {
                intercepted.Add(1);
                route.ContinueAsync();
            };

            await context.RouteAsync("**/empty.html", handler1);
            await context.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(2);
                route.ContinueAsync();
            });

            await context.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(3);
                route.ContinueAsync();
            });

            await context.RouteAsync("**/*", (route) =>
            {
                intercepted.Add(4);
                route.ContinueAsync();
            });

            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(new List<int>() { 1 }, intercepted);

            intercepted.Clear();
            await context.UnrouteAsync("**/empty.html", handler1);
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(new List<int>() { 2 }, intercepted);

            intercepted.Clear();
            await context.UnrouteAsync("**/empty.html");
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(new List<int>() { 4 }, intercepted);
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should yield to page.route")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldYieldToPageRoute()
        {
            await using var context = await Browser.NewContextAsync();
            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "context" });
            });

            var page = await context.NewPageAsync();
            await page.RouteAsync("**/empty.html", (route) =>
            {
                route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "page" });
            });

            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("page", await response.TextAsync());
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should fall back to context.route")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFallBackToContextRoute()
        {
            await using var context = await Browser.NewContextAsync();
            await context.RouteAsync("**/empty.html", (route) =>
            {
                route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "context" });
            });

            var page = await context.NewPageAsync();
            await page.RouteAsync("**/non-empty.html", (route) =>
            {
                route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "page" });
            });

            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("context", await response.TextAsync());
        }
    }
}
