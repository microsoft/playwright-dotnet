/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
        public async Task ShouldUnroute()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var intercepted = new List<int>();


            await context.RouteAsync("**/*", route =>
            {
                intercepted.Add(1);
                route.ContinueAsync();
            });

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

            Action<IRoute> handler4 = (route) =>
            {
                intercepted.Add(4);
                route.ContinueAsync();
            };
            await context.RouteAsync("**/empty.html", handler4);

            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(new List<int>() { 4 }, intercepted);

            intercepted.Clear();
            await context.UnrouteAsync("**/empty.html", handler4);
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(new List<int>() { 3 }, intercepted);

            intercepted.Clear();
            await context.UnrouteAsync("**/empty.html");
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(new List<int>() { 1 }, intercepted);
        }

        [PlaywrightTest("browsercontext-route.spec.ts", "should yield to page.route")]
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
