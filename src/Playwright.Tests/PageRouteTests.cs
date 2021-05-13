using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Playwright.Contracts.Constants;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageRouteTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageRouteTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-route.spec.ts", "should intercept")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIntercept()
        {
            bool intercepted = false;
            await Page.RouteAsync("**/empty.html", (route) =>
            {
                Assert.Contains("empty.html", route.Request.Url);
                Assert.Contains(route.Request.Headers, x => string.Equals(x.Key, "user-agent", StringComparison.OrdinalIgnoreCase));
                Assert.Equal(HttpMethod.Get.Method, route.Request.Method);
                Assert.Null(route.Request.PostData);
                Assert.True(route.Request.IsNavigationRequest);
                Assert.Equal(ResourceTypes.Document, route.Request.ResourceType, true);
                Assert.Same(route.Request.Frame, Page.MainFrame);
                Assert.Equal("about:blank", route.Request.Frame.Url);
                route.ResumeAsync();
                intercepted = true;
            });

            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should unroute")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnroute()
        {
            var intercepted = new List<int>();
            Action<IRoute> handler1 = (route) =>
            {
                intercepted.Add(1);
                route.ResumeAsync();
            };

            await Page.RouteAsync("**/empty.html", handler1);
            await Page.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(2);
                route.ResumeAsync();
            });

            await Page.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(3);
                route.ResumeAsync();
            });

            await Page.RouteAsync("**/*", (route) =>
            {
                intercepted.Add(4);
                route.ResumeAsync();
            });

            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { 1 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html", handler1);
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { 2 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html");
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { 4 }, intercepted.ToArray());
        }

        [PlaywrightTest("page-route.spec.ts", "should work when POST is redirected with 302")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenPostIsRedirectedWith302()
        {
            Server.SetRedirect("/rredirect", "/empty.html");
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            await Page.SetContentAsync(@"
                <form action='/rredirect' method='post'>
                    <input type=""hidden"" id=""foo"" name=""foo"" value=""FOOBAR"">
                </form>");
            await TaskUtils.WhenAll(
                Page.EvalOnSelectorAsync("form", "form => form.submit()"),
                Page.WaitForNavigationAsync()
            );
        }

        [PlaywrightTest("page-route.spec.ts", "should work when header manipulation headers with redirect")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenHeaderManipulationHeadersWithRedirect()
        {
            Server.SetRedirect("/rrredirect", "/empty.html");
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["foo"] = "bar" };
                route.ResumeAsync(headers: headers);
            });
            await Page.GotoAsync(TestConstants.ServerUrl + "/rrredirect");
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to remove headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToRemoveHeaders()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["foo"] = "bar" };
                headers.Remove("origin");
                route.ResumeAsync(headers: headers);
            });

            var originRequestHeader = Server.WaitForRequest("/empty.html", request => request.Headers["origin"]);
            await TaskUtils.WhenAll(
                originRequestHeader,
                Page.GotoAsync(TestConstants.EmptyPage)
            );
            Assert.Equal(StringValues.Empty, originRequestHeader.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should contain referer header")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldContainRefererHeader()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ResumeAsync();
            });
            await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Contains("/one-style.css", requests[1].Url);
            Assert.Contains("/one-style.html", requests[1].GetHeaderValue("referer"));
        }

        [PlaywrightTest("page-route.spec.ts", "should properly return navigation response when URL has cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProperlyReturnNavigationResponseWhenURLHasCookies()
        {
            // Setup cookie.
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = TestConstants.EmptyPage,
                    Name = "foo",
                    Value = "bar"
                }
            });

            // Setup request interception.
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            var response = await Page.ReloadAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("page-route.spec.ts", "should show custom HTTP headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldShowCustomHTTPHeaders()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["foo"] = "bar"
            });
            await Page.RouteAsync("**/*", (route) =>
            {
                Assert.Equal("bar", route.Request.GetHeaderValue("foo"));
                route.ResumeAsync();
            });
            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirect inside sync XHR")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirectInsideSyncXHR()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            Server.SetRedirect("/logo.png", "/pptr.png");
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            int status = await Page.EvaluateAsync<int>(@"async () => {
                var request = new XMLHttpRequest();
                request.open('GET', '/logo.png', false);  // `false` makes the request synchronous
                request.send(null);
                return request.status;
            }");
            Assert.Equal(200, status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with custom referer headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCustomRefererHeaders()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { ["referer"] = TestConstants.EmptyPage });
            await Page.RouteAsync("**/*", (route) =>
            {
                Assert.Equal(TestConstants.EmptyPage, route.Request.GetHeaderValue("referer"));
                route.ResumeAsync();
            });
            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbortable()
        {
            await Page.RouteAsync(new Regex("\\.css"), (route) => route.AbortAsync());

            int failedRequests = 0;
            Page.RequestFailed += (_, _) => ++failedRequests;
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(response.Ok);
            Assert.Null(response.Request.Failure);
            Assert.Equal(1, failedRequests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable with custom error codes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbortableWithCustomErrorCodes()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                route.AbortAsync(RequestAbortErrorCode.InternetDisconnected);
            });

            IRequest failedRequest = null;
            Page.RequestFailed += (_, e) => failedRequest = e;
            await Page.GotoAsync(TestConstants.EmptyPage).ContinueWith(_ => { });
            Assert.NotNull(failedRequest);
            if (TestConstants.IsWebKit)
            {
                Assert.Equal("Request intercepted", failedRequest.Failure);
            }
            else if (TestConstants.IsFirefox)
            {
                Assert.Equal("NS_ERROR_OFFLINE", failedRequest.Failure);
            }
            else
            {
                Assert.Equal("net::ERR_INTERNET_DISCONNECTED", failedRequest.Failure);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should send referer")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendReferer()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://google.com/" });
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            var requestTask = Server.WaitForRequest("/grid.html", request => request.Headers["referer"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.GotoAsync(TestConstants.ServerUrl + "/grid.html")
            );
            Assert.Equal("http://google.com/", requestTask.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should fail navigation when aborting main resource")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailNavigationWhenAbortingMainResource()
        {
            await Page.RouteAsync("**/*", (route) => route.AbortAsync());
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => Page.GotoAsync(TestConstants.EmptyPage));
            Assert.NotNull(exception);
            if (TestConstants.IsWebKit)
            {
                Assert.Contains("Request intercepted", exception.Message);
            }
            else if (TestConstants.IsFirefox)
            {
                Assert.Contains("NS_ERROR_FAILURE", exception.Message);
            }
            else
            {
                Assert.Contains("net::ERR_FAILED", exception.Message);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should not work with redirects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotWorkWithRedirects()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ResumeAsync();
                requests.Add(route.Request);
            });
            Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
            Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
            Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
            Server.SetRedirect("/non-existing-page-4.html", "/empty.html");
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/non-existing-page.html");

            Assert.Contains("non-existing-page.html", requests[0].Url);
            Assert.Single(requests);
            Assert.Equal(ResourceTypes.Document, requests[0].ResourceType, true);
            Assert.True(requests[0].IsNavigationRequest);

            var chain = new List<IRequest>();

            for (var request = response.Request; request != null; request = request.RedirectedFrom)
            {
                chain.Add(request);
                Assert.True(request.IsNavigationRequest);
            }

            Assert.Equal(5, chain.Count);
            Assert.Contains("/empty.html", chain[0].Url);
            Assert.Contains("/non-existing-page-4.html", chain[1].Url);
            Assert.Contains("/non-existing-page-3.html", chain[2].Url);
            Assert.Contains("/non-existing-page-2.html", chain[3].Url);
            Assert.Contains("/non-existing-page.html", chain[4].Url);

            for (int i = 0; i < chain.Count; ++i)
            {
                var request = chain[i];
                Assert.True(request.IsNavigationRequest);
                Assert.Equal(i > 0 ? chain[i - 1] : null, chain[i].RedirectedTo);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirects for subresources")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirectsForSubresources()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ResumeAsync();
                requests.Add(route.Request);
            });
            Server.SetRedirect("/one-style.css", "/two-style.css");
            Server.SetRedirect("/two-style.css", "/three-style.css");
            Server.SetRedirect("/three-style.css", "/four-style.css");
            Server.SetRoute("/four-style.css", context => context.Response.WriteAsync("body {box-sizing: border-box; }"));

            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("one-style.html", response.Url);

            Assert.Equal(2, requests.Count);
            Assert.Equal(ResourceTypes.Document, requests[0].ResourceType, true);
            Assert.Contains("one-style.html", requests[0].Url);

            var request = requests[1];
            foreach (string url in new[] { "/one-style.css", "/two-style.css", "/three-style.css", "/four-style.css" })
            {
                Assert.Equal(ResourceTypes.Stylesheet, request.ResourceType, true);
                Assert.Contains(url, request.Url);
                request = request.RedirectedTo;
            }

            Assert.Null(request);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with equal requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEqualRequests()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            int responseCount = 1;
            Server.SetRoute("/zzz", context => context.Response.WriteAsync((responseCount++ * 11).ToString()));

            bool spinner = false;
            // Cancel 2nd request.
            await Page.RouteAsync("**/*", (route) =>
            {
                if (spinner)
                {
                    _ = route.AbortAsync();
                }
                else
                {
                    _ = route.ResumeAsync();
                }
                spinner = !spinner;
            });

            var results = new List<string>();
            for (int i = 0; i < 3; ++i)
            {
                results.Add(await Page.EvaluateAsync<string>("fetch('/zzz').then(response => response.text()).catch (e => 'FAILED')"));
            }

            Assert.Equal(new[] { "11", "FAILED", "22" }, results);
        }

        [PlaywrightTest("page-route.spec.ts", "should navigate to dataURL and not fire dataURL requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ResumeAsync();
            });
            string dataURL = "data:text/html,<div>yo</div>";
            var response = await Page.GotoAsync(dataURL);
            Assert.Null(response);
            Assert.Empty(requests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to fetch dataURL and not fire dataURL requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);

            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ResumeAsync();
            });
            string dataURL = "data:text/html,<div>yo</div>";
            string text = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", dataURL);
            Assert.Equal("<div>yo</div>", text);
            Assert.Empty(requests);
        }

        [PlaywrightTest("page-route.spec.ts", "should navigate to URL with hash and and fire requests without hash")]
        [Fact(Skip = "Not implemented")]
        public async Task ShouldNavigateToURLWithHashAndAndFireRequestsWithoutHash()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ResumeAsync();
            });
            var response = await Page.GotoAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(TestConstants.EmptyPage, response.Url);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEncodedServer()
        {
            // The requestWillBeSent will report encoded URL, whereas interception will
            // report URL as-is. @see crbug.com/759388
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/some nonexisting page");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with badly encoded server")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBadlyEncodedServer()
        {
            Server.SetRoute("/malformed?rnd=%911", _ => Task.CompletedTask);
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/malformed?rnd=%911");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server - 2")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEncodedServer2()
        {
            // The requestWillBeSent will report URL as-is, whereas interception will
            // report encoded URL for stylesheet. @see crbug.com/759388
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ResumeAsync();
                requests.Add(route.Request);
            });
            var response = await Page.GotoAsync($"data:text/html,<link rel=\"stylesheet\" href=\"{TestConstants.EmptyPage}/fonts?helvetica|arial\"/>");
            Assert.Null(response);
            Assert.Single(requests);
            Assert.Equal(HttpStatusCode.NotFound, (await requests[0].ResponseAsync()).StatusCode);
        }

        [PlaywrightTest("page-route.spec.ts", @"should not throw ""Invalid Interception Id"" if the request was cancelled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowInvalidInterceptionIdIfTheRequestWasCancelled()
        {
            await Page.SetContentAsync("<iframe></iframe>");
            IRoute route = null;
            await Page.RouteAsync("**/*", (r) => route = r);
            _ = Page.EvalOnSelectorAsync("iframe", "(frame, url) => frame.src = url", TestConstants.EmptyPage);
            // Wait for request interception.
            await Page.WaitForEventAsync(PageEvent.Request);
            // Delete frame to cause request to be canceled.
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            await route.ResumeAsync();
        }

        [PlaywrightTest("page-route.spec.ts", "should intercept main resource during cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInterceptMainResourceDuringCrossProcessNavigation()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            bool intercepted = false;
            await Page.RouteAsync(TestConstants.CrossProcessUrl + "/empty.html", (route) =>
            {
                if (route.Request.Url.Contains(TestConstants.CrossProcessHttpPrefix + "/empty.html"))
                {
                    intercepted = true;
                }

                route.ResumeAsync();
            });
            var response = await Page.GotoAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should fulfill with redirect status")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldFulfillWithRedirectStatus()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/title.html");
            Server.SetRoute("/final", context => context.Response.WriteAsync("foo"));
            await Page.RouteAsync("**/*", (route) =>
            {
                if (route.Request.Url != TestConstants.ServerUrl + "/redirect_this")
                {
                    route.ResumeAsync();
                    return;
                }

                _ = route.FulfillAsync(
                    status: HttpStatusCode.MovedPermanently,
                    headers: new Dictionary<string, string>
                    {
                        ["location"] = "/final",
                    });
            });

            string text = await Page.EvaluateAsync<string>(@"async url => {
              const data = await fetch(url);
              return data.text();
            }", TestConstants.ServerUrl + "/redirect_this");

            Assert.Equal("foo", text);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with GET")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithGET()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                var headers = route.Request.Url.EndsWith("allow")
                    ? new Dictionary<string, string> { ["access-control-allow-origin"] = "*" }
                    : new Dictionary<string, string>();

                _ = route.FulfillAsync(
                    contentType: "application/json",
                    headers: headers,
                    status: HttpStatusCode.OK,
                    body: "[\"electric\", \"cars\"]");
            });

            string[] resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars?allow', { mode: 'cors' });
                return response.json();
            }");

            Assert.Equal(new[] { "electric", "cars" }, resp);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => Page.EvaluateAsync<string>(@"async () => {
                const response = await fetch('https://example.com/cars?reject', { mode: 'cors' });
                return response.json();
            }"));

            Assert.Contains("failed", exception.Message);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with POST")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithPOST()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                _ = route.FulfillAsync(
                    contentType: "application/json",
                    headers: new Dictionary<string, string> { ["access-control-allow-origin"] = "*" },
                    status: HttpStatusCode.OK,
                    body: "[\"electric\", \"cars\"]");
            });

            string[] resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    mode: 'cors',
                    body: JSON.stringify({ 'number': 1 }) 
                });
                return response.json();
            }");

            Assert.Equal(new[] { "electric", "cars" }, resp);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with different methods")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithDifferentMethods()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                _ = route.FulfillAsync(
                    contentType: "application/json",
                    headers: new Dictionary<string, string> { ["access-control-allow-origin"] = "*" },
                    status: HttpStatusCode.OK,
                    body: $"[\"{ route.Request.Method.ToString().ToUpper() }\", \"electric\", \"cars\"]");
            });

            string[] resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars', {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  mode: 'cors',
                  body: JSON.stringify({ 'number': 1 }) 
                });
                return response.json();
            }");

            Assert.Equal(new[] { "POST", "electric", "cars" }, resp);

            resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars', {
                  method: 'DELETE',
                  headers: { 'Content-Type': 'application/json' },
                  mode: 'cors',
                  body: JSON.stringify({ 'number': 1 }) 
                });
                return response.json();
            }");

            Assert.Equal(new[] { "DELETE", "electric", "cars" }, resp);
        }
    }
}
