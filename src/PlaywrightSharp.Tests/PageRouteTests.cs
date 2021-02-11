using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
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
            await Page.RouteAsync("**/empty.html", (route, request) =>
            {
                Assert.Contains("empty.html", request.Url);
                Assert.True(request.Headers.ContainsKey("user-agent"));
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Null(request.PostData);
                Assert.True(request.IsNavigationRequest);
                Assert.Equal(ResourceType.Document, request.ResourceType);
                Assert.Same(request.Frame, Page.MainFrame);
                Assert.Equal("about:blank", request.Frame.Url);
                route.ContinueAsync();
                intercepted = true;
            });

            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should unroute")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnroute()
        {
            var intercepted = new List<int>();
            Action<Route, IRequest> handler1 = (route, _) =>
            {
                intercepted.Add(1);
                route.ContinueAsync();
            };

            await Page.RouteAsync("**/empty.html", handler1);
            await Page.RouteAsync("**/empty.html", (route, request) =>
            {
                intercepted.Add(2);
                route.ContinueAsync();
            });

            await Page.RouteAsync("**/empty.html", (route, request) =>
            {
                intercepted.Add(3);
                route.ContinueAsync();
            });

            await Page.RouteAsync("**/*", (route, request) =>
            {
                intercepted.Add(4);
                route.ContinueAsync();
            });

            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { 1 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html", handler1);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { 2 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(new[] { 4 }, intercepted.ToArray());
        }

        [PlaywrightTest("page-route.spec.ts", "should work when POST is redirected with 302")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenPostIsRedirectedWith302()
        {
            Server.SetRedirect("/rredirect", "/empty.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/*", (route, _) => route.ContinueAsync());
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
            await Page.RouteAsync("**/*", (route, request) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers) { ["foo"] = "bar" };
                route.ContinueAsync(headers: headers);
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to remove headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToRemoveHeaders()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers) { ["foo"] = "bar" };
                headers.Remove("origin");
                route.ContinueAsync(headers: headers);
            });

            var originRequestHeader = Server.WaitForRequest("/empty.html", request => request.Headers["origin"]);
            await TaskUtils.WhenAll(
                originRequestHeader,
                Page.GoToAsync(TestConstants.EmptyPage)
            );
            Assert.Equal(StringValues.Empty, originRequestHeader.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should contain referer header")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldContainRefererHeader()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route, request) =>
            {
                requests.Add(request);
                route.ContinueAsync();
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Contains("/one-style.css", requests[1].Url);
            Assert.Contains("/one-style.html", requests[1].Headers["referer"]);
        }

        [PlaywrightTest("page-route.spec.ts", "should properly return navigation response when URL has cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProperlyReturnNavigationResponseWhenURLHasCookies()
        {
            // Setup cookie.
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "foo",
                Value = "bar"
            });

            // Setup request interception.
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            var response = await Page.ReloadAsync();
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should show custom HTTP headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldShowCustomHTTPHeaders()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["foo"] = "bar"
            });
            await Page.RouteAsync("**/*", (route, request) =>
            {
                Assert.Equal("bar", request.Headers["foo"]);
                route.ContinueAsync();
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirect inside sync XHR")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirectInsideSyncXHR()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRedirect("/logo.png", "/pptr.png");
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
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
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = TestConstants.EmptyPage });
            await Page.RouteAsync("**/*", (route, request) =>
            {
                Assert.Equal(TestConstants.EmptyPage, request.Headers["referer"]);
                route.ContinueAsync();
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbortable()
        {
            await Page.RouteAsync(new Regex("\\.css"), (route, request) => route.AbortAsync());

            int failedRequests = 0;
            Page.RequestFailed += (sender, e) => ++failedRequests;
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(response.Ok);
            Assert.Null(response.Request.Failure);
            Assert.Equal(1, failedRequests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable with custom error codes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbortableWithCustomErrorCodes()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.AbortAsync(RequestAbortErrorCode.InternetDisconnected);
            });

            IRequest failedRequest = null;
            Page.RequestFailed += (sender, e) => failedRequest = e.Request;
            await Page.GoToAsync(TestConstants.EmptyPage).ContinueWith(task => { });
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
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://google.com/" });
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            var requestTask = Server.WaitForRequest("/grid.html", request => request.Headers["referer"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.GoToAsync(TestConstants.ServerUrl + "/grid.html")
            );
            Assert.Equal("http://google.com/", requestTask.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should fail navigation when aborting main resource")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailNavigationWhenAbortingMainResource()
        {
            await Page.RouteAsync("**/*", (route, request) => route.AbortAsync());
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.GoToAsync(TestConstants.EmptyPage));
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
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.ContinueAsync();
                requests.Add(request);
            });
            Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
            Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
            Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
            Server.SetRedirect("/non-existing-page-4.html", "/empty.html");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/non-existing-page.html");

            Assert.Contains("non-existing-page.html", requests[0].Url);
            Assert.Single(requests);
            Assert.Equal(ResourceType.Document, requests[0].ResourceType);
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
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.ContinueAsync();
                requests.Add(request);
            });
            Server.SetRedirect("/one-style.css", "/two-style.css");
            Server.SetRedirect("/two-style.css", "/three-style.css");
            Server.SetRedirect("/three-style.css", "/four-style.css");
            Server.SetRoute("/four-style.css", context => context.Response.WriteAsync("body {box-sizing: border-box; }"));

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("one-style.html", response.Url);

            Assert.Equal(2, requests.Count);
            Assert.Equal(ResourceType.Document, requests[0].ResourceType);
            Assert.Contains("one-style.html", requests[0].Url);

            var request = requests[1];
            foreach (string url in new[] { "/one-style.css", "/two-style.css", "/three-style.css", "/four-style.css" })
            {
                Assert.Equal(ResourceType.StyleSheet, request.ResourceType);
                Assert.Contains(url, request.Url);
                request = request.RedirectedTo;
            }

            Assert.Null(request);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with equal requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEqualRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            int responseCount = 1;
            Server.SetRoute("/zzz", context => context.Response.WriteAsync((responseCount++ * 11).ToString()));

            bool spinner = false;
            // Cancel 2nd request.
            await Page.RouteAsync("**/*", (route, request) =>
            {
                if (spinner)
                {
                    _ = route.AbortAsync();
                }
                else
                {
                    _ = route.ContinueAsync();
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
            await Page.RouteAsync("**/*", (route, request) =>
            {
                requests.Add(request);
                route.ContinueAsync();
            });
            string dataURL = "data:text/html,<div>yo</div>";
            var response = await Page.GoToAsync(dataURL);
            Assert.Null(response);
            Assert.Empty(requests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to fetch dataURL and not fire dataURL requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route, request) =>
            {
                requests.Add(request);
                route.ContinueAsync();
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
            await Page.RouteAsync("**/*", (route, request) =>
            {
                requests.Add(request);
                route.ContinueAsync();
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(HttpStatusCode.OK, response.Status);
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
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/some nonexisting page");
            Assert.Equal(HttpStatusCode.NotFound, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with badly encoded server")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBadlyEncodedServer()
        {
            Server.SetRoute("/malformed?rnd=%911", context => Task.CompletedTask);
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/malformed?rnd=%911");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server - 2")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEncodedServer2()
        {
            // The requestWillBeSent will report URL as-is, whereas interception will
            // report encoded URL for stylesheet. @see crbug.com/759388
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.ContinueAsync();
                requests.Add(request);
            });
            var response = await Page.GoToAsync($"data:text/html,<link rel=\"stylesheet\" href=\"{TestConstants.EmptyPage}/fonts?helvetica|arial\"/>");
            Assert.Null(response);
            Assert.Single(requests);
            Assert.Equal(HttpStatusCode.NotFound, (await requests[0].GetResponseAsync()).Status);
        }

        [PlaywrightTest("page-route.spec.ts", @"should not throw ""Invalid Interception Id"" if the request was cancelled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowInvalidInterceptionIdIfTheRequestWasCancelled()
        {
            await Page.SetContentAsync("<iframe></iframe>");
            Route route = null;
            await Page.RouteAsync("**/*", (r, _) => route = r);
            _ = Page.EvalOnSelectorAsync("iframe", "(frame, url) => frame.src = url", TestConstants.EmptyPage);
            // Wait for request interception.
            await Page.WaitForEventAsync(PageEvent.Request);
            // Delete frame to cause request to be canceled.
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            await route.ContinueAsync();
        }

        [PlaywrightTest("page-route.spec.ts", "should intercept main resource during cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInterceptMainResourceDuringCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            bool intercepted = false;
            await Page.RouteAsync(TestConstants.CrossProcessUrl + "/empty.html", (route, request) =>
            {
                if (request.Url.Contains(TestConstants.CrossProcessHttpPrefix + "/empty.html"))
                {
                    intercepted = true;
                }

                route.ContinueAsync();
            });
            var response = await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should fulfill with redirect status")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldFulfillWithRedirectStatus()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/title.html");
            Server.SetRoute("/final", context => context.Response.WriteAsync("foo"));
            await Page.RouteAsync("**/*", (route, request) =>
            {
                if (request.Url != TestConstants.ServerUrl + "/redirect_this")
                {
                    route.ContinueAsync();
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
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route, request) =>
            {
                var headers = request.Url.EndsWith("allow")
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

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvaluateAsync<string>(@"async () => {
                const response = await fetch('https://example.com/cars?reject', { mode: 'cors' });
                return response.json();
            }"));

            Assert.Contains("failed", exception.Message);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with POST")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithPOST()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route, request) =>
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
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route, request) =>
            {
                _ = route.FulfillAsync(
                    contentType: "application/json",
                    headers: new Dictionary<string, string> { ["access-control-allow-origin"] = "*" },
                    status: HttpStatusCode.OK,
                    body: $"[\"{ request.Method.ToString().ToUpper() }\", \"electric\", \"cars\"]");
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
