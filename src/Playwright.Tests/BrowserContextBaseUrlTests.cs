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
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextBaseUrlTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-base-url.spec.ts", "should construct a new URL when a baseURL in browser.newContext is passed to page.goto")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldConstructANewURLInBrowserNewContextWhenGoto()
        {
            await using var context = await Browser.NewContextAsync(new() { BaseURL = Server.Prefix });
            var page = await context.NewPageAsync();
            var response = await page.GotoAsync("/empty.html");
            Assert.AreEqual(Server.EmptyPage, response.Url);
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should construct a new URL when a baseURL in browser.newPage is passed to page.goto")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldConstructANewURLInBrowserNewPageWhenGoto()
        {
            var page = await Browser.NewPageAsync(new() { BaseURL = Server.Prefix });
            var response = await page.GotoAsync("/empty.html");
            Assert.AreEqual(Server.EmptyPage, response.Url);
            await page.CloseAsync();
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should construct the URLs correctly when a baseURL without a trailing slash in browser.newPage is passed to page.goto")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldConstructTheURLsCorrectlyWihoutTrailingSlash()
        {
            var serverUri = new Uri(Server.Prefix);
            var page = await Browser.NewPageAsync(new() { BaseURL = new Uri(serverUri, "/url-construction").ToString() });

            Assert.AreEqual(new Uri(serverUri, "/mypage.html"), (await page.GotoAsync("mypage.html")).Url);
            Assert.AreEqual(new Uri(serverUri, "/mypage.html"), (await page.GotoAsync("./mypage.html")).Url);
            Assert.AreEqual(new Uri(serverUri, "/mypage.html"), (await page.GotoAsync("/mypage.html")).Url);

            await page.CloseAsync();
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should construct the URLs correctly when a baseURL with a trailing slash in browser.newPage is passed to page.goto")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldConstructTheURLsCorrectlyWithATrailingSlash()
        {
            var serverUri = new Uri(Server.Prefix);
            var page = await Browser.NewPageAsync(new() { BaseURL = new Uri(serverUri, "/url-construction/").ToString() });

            Assert.AreEqual(new Uri(serverUri, "/url-construction/mypage.html"), (await page.GotoAsync("mypage.html")).Url);
            Assert.AreEqual(new Uri(serverUri, "/url-construction/mypage.html"), (await page.GotoAsync("./mypage.html")).Url);
            Assert.AreEqual(new Uri(serverUri, "/mypage.html"), (await page.GotoAsync("/mypage.html")).Url);
            Assert.AreEqual(new Uri(serverUri, "/url-construction/"), (await page.GotoAsync(".")).Url);
            Assert.AreEqual(new Uri(serverUri, "/"), (await page.GotoAsync("/")).Url);

            await page.CloseAsync();
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should not construct a new URL when valid URLs are passed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotConstructANewURLWhenValidURLsArePassed()
        {
            var page = await Browser.NewPageAsync(new() { BaseURL = "http://microsoft.com" });

            Assert.AreEqual(Server.EmptyPage, (await page.GotoAsync(Server.EmptyPage)).Url);

            await page.GotoAsync("data:text/html,Hello World");
            Assert.AreEqual("data:text/html,Hello World", await page.EvaluateAsync<string>("window.location.href"));

            await page.GotoAsync("about:blank");
            Assert.AreEqual("about:blank", await page.EvaluateAsync<string>("window.location.href"));

            await page.CloseAsync();
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should be able to match a URL relative to its given URL with urlMatcher")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToMatchURLRelativeToItsGivenURL()
        {
            var serverUri = new Uri(Server.Prefix);
            var page = await Browser.NewPageAsync(new() { BaseURL = new Uri(serverUri, "/foobar/").ToString() });

            await page.GotoAsync("/kek/index.html");
            await page.WaitForURLAsync("/kek/index.html");
            Assert.AreEqual(Server.Prefix + "/kek/index.html", page.Url);

            await page.RouteAsync("./kek/index.html", route => route.FulfillAsync(new()
            {
                Body = "base-url-matched-route"
            }));

            var requestTask = page.WaitForRequestAsync("./kek/index.html");
            var responseTask = page.WaitForResponseAsync("./kek/index.html");
            Task.WaitAll(
                requestTask,
                responseTask,
                page.GotoAsync("./kek/index.html"));

            Assert.AreEqual(Server.Prefix + "/foobar/kek/index.html", requestTask.Result.Url);
            Assert.AreEqual(Server.Prefix + "/foobar/kek/index.html", responseTask.Result.Url);
            Assert.AreEqual("base-url-matched-route", Encoding.UTF8.GetString(await responseTask.Result.BodyAsync()));

            await page.CloseAsync();
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should construct a new URL when a baseURL in browserType.launchPersistentContext is passed to page.goto")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldConstructANewURLWhenABaseURLIsInPersistentContext()
        {
            using var dir = new TempDirectory();
            await using var context = await BrowserType.LaunchPersistentContextAsync(dir.Path, new() { BaseURL = Server.Prefix });

            var page = await context.NewPageAsync();
            Assert.AreEqual(Server.EmptyPage, (await page.GotoAsync("/empty.html")).Url);
        }

        [PlaywrightTest("browsercontext-base-url.spec.ts", "should not construct a new URL with baseURL when a glob was used")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotConstructANewUrlWithBaseURLWhenAGlobWasUsed()
        {
            var page = await Browser.NewPageAsync(new() { BaseURL = Server.Prefix + "/foobar/" });
            await page.GotoAsync("./kek/index.html");
            await page.WaitForURLAsync("**/foobar/kek/index.html");
            await page.CloseAsync();
        }

    }
}
