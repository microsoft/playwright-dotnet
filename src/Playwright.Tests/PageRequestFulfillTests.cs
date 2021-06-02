using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class RequestFulfillTests : PageTestEx
    {
        public RequestFulfillTests()
        {
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = (int)HttpStatusCode.Created,
                    Headers = new Dictionary<string, string>
                    {
                        ["foo"] = "bar"
                    },
                    ContentType = "text/html",
                    Body = "Yo, page!",
                });
            });
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.Created, response.Status);
            Assert.AreEqual("bar", response.Headers["foo"]);
            Assert.AreEqual("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        /// <summary>
        /// In Playwright this method is called ShouldWorkWithStatusCode422.
        /// I found that status 422 is not available in all .NET runtime versions (see https://github.com/dotnet/core/blob/4c4642d548074b3fbfd425541a968aadd75fea99/release-notes/2.1/Preview/api-diff/preview2/2.1-preview2_System.Net.md)
        /// As the goal here is testing HTTP codes that are not in Chromium (see https://cs.chromium.org/chromium/src/net/http/http_status_code_list.h?sq=package:chromium) we will use code 426: Upgrade Required
        /// </summary>
        [PlaywrightTest("page-request-fulfill.spec.ts", "should work with status code 422")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithStatusCode422()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions { Status = (int)HttpStatusCode.UpgradeRequired, Body = "Yo, page!" });
            });
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.UpgradeRequired, response.Status);
            Assert.AreEqual("Upgrade Required", response.StatusText);
            Assert.AreEqual("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should allow mocking binary responses")]
        [Test, Ignore("We need screenshots for this")]
        public async Task ShouldAllowMockingBinaryResponses()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
                route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "image/png",
                    BodyBytes = imageBuffer,
                });
            });
            await Page.EvaluateAsync(@"PREFIX => {
                const img = document.createElement('img');
                img.src = PREFIX + '/does-not-exist.png';
                document.body.appendChild(img);
                return new Promise(fulfill => img.onload = fulfill);
            }", Server.Prefix);
            var img = await Page.QuerySelectorAsync("img");
            Assert.True(ScreenshotHelper.PixelMatch("mock-binary-response.png", await img.ScreenshotAsync()));
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should allow mocking svg with charset")]
        [Test, Ignore("We need screenshots for this")]
        public void ShouldAllowMockingSvgWithCharset()
        {
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should work with file path")]
        [Test, Ignore("We need screenshots for this")]
        public async Task ShouldWorkWithFilePath()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "shouldBeIgnored",
                    Path = TestUtils.GetWebServerFile("pptr.png"),
                });
            });

            await Page.EvaluateAsync(@"PREFIX => {
                const img = document.createElement('img');
                img.src = PREFIX + '/does-not-exist.png';
                document.body.appendChild(img);
                return new Promise(fulfill => img.onload = fulfill);
            }", Server.Prefix);
            var img = await Page.QuerySelectorAsync("img");
            Assert.True(ScreenshotHelper.PixelMatch("mock-binary-response.png", await img.ScreenshotAsync()));
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should stringify intercepted request response headers")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldStringifyInterceptedRequestResponseHeaders()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = (int)HttpStatusCode.OK,
                    Headers = new Dictionary<string, string>
                    {
                        ["foo"] = "true"
                    },
                    Body = "Yo, page!",
                });
            });

            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual("true", response.Headers["foo"]);
            Assert.AreEqual("Yo, page!", await Page.EvaluateAsync<string>("() => document.body.textContent"));
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should not modify the headers sent to the server")]
        [Test, Ignore("Flacky with the ASP.NET server")]
        public async Task ShouldNotModifyTheHeadersSentToTheServer()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var interceptedRequests = new List<Dictionary<string, string>>();

            await Page.GotoAsync(Server.Prefix + "/unused");

            Server.SetRoute("/something", ctx =>
            {
                var hh = new Dictionary<string, string>();
                foreach (var h in ctx.Request.Headers)
                {
                    hh[h.Key] = h.Value;
                }
                interceptedRequests.Add(hh);
                ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
                return ctx.Response.WriteAsync("done");
            });

            string text = await Page.EvaluateAsync<string>(@"async url => {
                const data = await fetch(url);
                return data.text();
            }", Server.CrossProcessPrefix + "/something");

            Assert.AreEqual("done", text);

            IRequest playwrightRequest = null;

            await Page.RouteAsync(Server.CrossProcessPrefix + "/something", (route) =>
            {
                playwrightRequest = route.Request;
                route.ContinueAsync(new RouteContinueOptions { Headers = route.Request.Headers.ToDictionary(x => x.Key, x => x.Value) });
            });

            string textAfterRoute = await Page.EvaluateAsync<string>(@"async url => {
                const data = await fetch(url);
                return data.text();
            }", Server.CrossProcessPrefix + "/something");

            Assert.AreEqual("done", textAfterRoute);

            Assert.AreEqual(2, interceptedRequests.Count);
            Assert.AreEqual(interceptedRequests[1].OrderBy(kv => kv.Key), interceptedRequests[0].OrderBy(kv => kv.Key));
        }

        [PlaywrightTest("page-request-fulfill.spec.ts", "should include the origin header")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeTheOriginHeader()
        {
            await Page.GotoAsync(Server.EmptyPage);
            IRequest interceptedRequest = null;

            await Page.RouteAsync(Server.CrossProcessPrefix + "/something", (route) =>
            {
                interceptedRequest = route.Request;
                route.FulfillAsync(new RouteFulfillOptions
                {
                    Headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*" },
                    ContentType = "text/plain",
                    Body = "done",
                });
            });

            string text = await Page.EvaluateAsync<string>(@"async url => {
                const data = await fetch(url);
                return data.text();
            }", Server.CrossProcessPrefix + "/something");

            Assert.AreEqual("done", text);
            Assert.AreEqual(Server.Prefix, interceptedRequest.Headers["origin"]);
        }
    }
}
