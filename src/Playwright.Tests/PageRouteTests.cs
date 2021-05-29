using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageRouteTests : PageTestEx
    {
        [PlaywrightTest("page-route.spec.ts", "should intercept")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIntercept()
        {
            bool intercepted = false;
            await Page.RouteAsync("**/empty.html", (route) =>
            {
                StringAssert.Contains("empty.html", route.Request.Url);
                Assert.False(string.IsNullOrEmpty(route.Request.Headers["user-agent"]));
                Assert.AreEqual(HttpMethod.Get.Method, route.Request.Method);
                Assert.Null(route.Request.PostData);
                Assert.True(route.Request.IsNavigationRequest);
                Assert.AreEqual("document", route.Request.ResourceType);
                Assert.AreEqual(route.Request.Frame, Page.MainFrame);
                Assert.AreEqual("about:blank", route.Request.Frame.Url);
                route.ContinueAsync();
                intercepted = true;
            });

            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should unroute")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUnroute()
        {
            var intercepted = new List<int>();
            Action<IRoute> handler1 = (route) =>
            {
                intercepted.Add(1);
                route.ContinueAsync();
            };

            await Page.RouteAsync("**/empty.html", handler1);
            await Page.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(2);
                route.ContinueAsync();
            });

            await Page.RouteAsync("**/empty.html", (route) =>
            {
                intercepted.Add(3);
                route.ContinueAsync();
            });

            await Page.RouteAsync("**/*", (route) =>
            {
                intercepted.Add(4);
                route.ContinueAsync();
            });

            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual(new[] { 1 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html", handler1);
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual(new[] { 2 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html");
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual(new[] { 4 }, intercepted.ToArray());
        }

        [PlaywrightTest("page-route.spec.ts", "should work when POST is redirected with 302")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenPostIsRedirectedWith302()
        {
            HttpServer.Server.SetRedirect("/rredirect", "/empty.html");
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenHeaderManipulationHeadersWithRedirect()
        {
            HttpServer.Server.SetRedirect("/rrredirect", "/empty.html");
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["foo"] = "bar" };
                route.ContinueAsync(new RouteContinueOptions { Headers = headers });
            });
            await Page.GotoAsync(TestConstants.ServerUrl + "/rrredirect");
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to remove headers")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToRemoveHeaders()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["foo"] = "bar" };
                headers.Remove("origin");
                route.ContinueAsync(new RouteContinueOptions { Headers = headers });
            });

            var originRequestHeader = HttpServer.Server.WaitForRequest("/empty.html", request => request.Headers["origin"]);
            await TaskUtils.WhenAll(
                originRequestHeader,
                Page.GotoAsync(TestConstants.EmptyPage)
            );
            Assert.AreEqual(StringValues.Empty, originRequestHeader.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should contain referer header")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldContainRefererHeader()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            StringAssert.Contains("/one-style.css", requests[1].Url);
            StringAssert.Contains("/one-style.html", requests[1].Headers["referer"]);
        }

        [PlaywrightTest("page-route.spec.ts", "should properly return navigation response when URL has cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var response = await Page.ReloadAsync();
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should show custom HTTP headers")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldShowCustomHTTPHeaders()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["foo"] = "bar"
            });
            await Page.RouteAsync("**/*", (route) =>
            {
                Assert.AreEqual("bar", route.Request.Headers["foo"]);
                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirect inside sync XHR")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirectInsideSyncXHR()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            HttpServer.Server.SetRedirect("/logo.png", "/pptr.png");
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            int status = await Page.EvaluateAsync<int>(@"async () => {
                var request = new XMLHttpRequest();
                request.open('GET', '/logo.png', false);  // `false` makes the request synchronous
                request.send(null);
                return request.status;
            }");
            Assert.AreEqual(200, status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with custom referer headers")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCustomRefererHeaders()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = TestConstants.EmptyPage });
            await Page.RouteAsync("**/*", (route) =>
            {
                Assert.AreEqual(TestConstants.EmptyPage, route.Request.Headers["referer"]);
                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbortable()
        {
            await Page.RouteAsync(new Regex("\\.css"), (route) => route.AbortAsync());

            int failedRequests = 0;
            Page.RequestFailed += (_, _) => ++failedRequests;
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(response.Ok);
            Assert.Null(response.Request.Failure);
            Assert.AreEqual(1, failedRequests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable with custom error codes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
                Assert.AreEqual("Request intercepted", failedRequest.Failure);
            }
            else if (TestConstants.IsFirefox)
            {
                Assert.AreEqual("NS_ERROR_OFFLINE", failedRequest.Failure);
            }
            else
            {
                Assert.AreEqual("net::ERR_INTERNET_DISCONNECTED", failedRequest.Failure);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should send referer")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendReferer()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://google.com/" });
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var requestTask = HttpServer.Server.WaitForRequest("/grid.html", request => request.Headers["referer"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.GotoAsync(TestConstants.ServerUrl + "/grid.html")
            );
            Assert.AreEqual("http://google.com/", requestTask.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should fail navigation when aborting main resource")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailNavigationWhenAbortingMainResource()
        {
            await Page.RouteAsync("**/*", (route) => route.AbortAsync());
            var exception = await AssertThrowsAsync<PlaywrightException>(() => Page.GotoAsync(TestConstants.EmptyPage));
            Assert.NotNull(exception);
            if (TestConstants.IsWebKit)
            {
                StringAssert.Contains("Request intercepted", exception.Message);
            }
            else if (TestConstants.IsFirefox)
            {
                StringAssert.Contains("NS_ERROR_FAILURE", exception.Message);
            }
            else
            {
                StringAssert.Contains("net::ERR_FAILED", exception.Message);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should not work with redirects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotWorkWithRedirects()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ContinueAsync();
                requests.Add(route.Request);
            });
            HttpServer.Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
            HttpServer.Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
            HttpServer.Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
            HttpServer.Server.SetRedirect("/non-existing-page-4.html", "/empty.html");
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/non-existing-page.html");

            StringAssert.Contains("non-existing-page.html", requests[0].Url);
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.AreEqual("document", requests[0].ResourceType);
            Assert.True(requests[0].IsNavigationRequest);

            var chain = new List<IRequest>();

            for (var request = response.Request; request != null; request = request.RedirectedFrom)
            {
                chain.Add(request);
                Assert.True(request.IsNavigationRequest);
            }

            Assert.AreEqual(5, chain.Count);
            StringAssert.Contains("/empty.html", chain[0].Url);
            StringAssert.Contains("/non-existing-page-4.html", chain[1].Url);
            StringAssert.Contains("/non-existing-page-3.html", chain[2].Url);
            StringAssert.Contains("/non-existing-page-2.html", chain[3].Url);
            StringAssert.Contains("/non-existing-page.html", chain[4].Url);

            for (int i = 0; i < chain.Count; ++i)
            {
                var request = chain[i];
                Assert.True(request.IsNavigationRequest);
                Assert.AreEqual(i > 0 ? chain[i - 1] : null, chain[i].RedirectedTo);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirects for subresources")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirectsForSubresources()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ContinueAsync();
                requests.Add(route.Request);
            });
            HttpServer.Server.SetRedirect("/one-style.css", "/two-style.css");
            HttpServer.Server.SetRedirect("/two-style.css", "/three-style.css");
            HttpServer.Server.SetRedirect("/three-style.css", "/four-style.css");
            HttpServer.Server.SetRoute("/four-style.css", context => context.Response.WriteAsync("body {box-sizing: border-box; }"));

            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            StringAssert.Contains("one-style.html", response.Url);

            Assert.AreEqual(2, requests.Count);
            Assert.AreEqual("document", requests[0].ResourceType);
            StringAssert.Contains("one-style.html", requests[0].Url);

            var request = requests[1];
            foreach (string url in new[] { "/one-style.css", "/two-style.css", "/three-style.css", "/four-style.css" })
            {
                Assert.AreEqual("stylesheet", request.ResourceType);
                StringAssert.Contains(url, request.Url);
                request = request.RedirectedTo;
            }

            Assert.Null(request);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with equal requests")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEqualRequests()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            int responseCount = 1;
            HttpServer.Server.SetRoute("/zzz", context => context.Response.WriteAsync((responseCount++ * 11).ToString()));

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
                    _ = route.ContinueAsync();
                }
                spinner = !spinner;
            });

            var results = new List<string>();
            for (int i = 0; i < 3; ++i)
            {
                results.Add(await Page.EvaluateAsync<string>("fetch('/zzz').then(response => response.text()).catch (e => 'FAILED')"));
            }

            Assert.AreEqual(new[] { "11", "FAILED", "22" }, results);
        }

        [PlaywrightTest("page-route.spec.ts", "should navigate to dataURL and not fire dataURL requests")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            string dataURL = "data:text/html,<div>yo</div>";
            var response = await Page.GotoAsync(dataURL);
            Assert.Null(response);
            Assert.IsEmpty(requests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to fetch dataURL and not fire dataURL requests")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);

            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            string dataURL = "data:text/html,<div>yo</div>";
            string text = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", dataURL);
            Assert.AreEqual("<div>yo</div>", text);
            Assert.IsEmpty(requests);
        }

        [PlaywrightTest("page-route.spec.ts", "should navigate to URL with hash and and fire requests without hash")]
        [Test, Ignore("Not implemented")]
        public async Task ShouldNavigateToURLWithHashAndAndFireRequestsWithoutHash()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(TestConstants.EmptyPage + "#hash");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual(TestConstants.EmptyPage, response.Url);
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.AreEqual(TestConstants.EmptyPage, requests[0].Url);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEncodedServer()
        {
            // The requestWillBeSent will report encoded URL, whereas interception will
            // report URL as-is. @see crbug.com/759388
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/some nonexisting page");
            Assert.AreEqual((int)HttpStatusCode.NotFound, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with badly encoded server")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithBadlyEncodedServer()
        {
            HttpServer.Server.SetRoute("/malformed?rnd=%911", _ => Task.CompletedTask);
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var response = await Page.GotoAsync(TestConstants.ServerUrl + "/malformed?rnd=%911");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server - 2")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithEncodedServer2()
        {
            // The requestWillBeSent will report URL as-is, whereas interception will
            // report encoded URL for stylesheet. @see crbug.com/759388
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ContinueAsync();
                requests.Add(route.Request);
            });
            var response = await Page.GotoAsync($"data:text/html,<link rel=\"stylesheet\" href=\"{TestConstants.EmptyPage}/fonts?helvetica|arial\"/>");
            Assert.Null(response);
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.AreEqual((int)HttpStatusCode.NotFound, (await requests[0].ResponseAsync()).Status);
        }

        [PlaywrightTest("page-route.spec.ts", @"should not throw ""Invalid Interception Id"" if the request was cancelled")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotThrowInvalidInterceptionIdIfTheRequestWasCancelled()
        {
            await Page.SetContentAsync("<iframe></iframe>");
            IRoute route = null;
            await Page.RouteAsync("**/*", (r) => route = r);
            _ = Page.EvalOnSelectorAsync("iframe", "(frame, url) => frame.src = url", TestConstants.EmptyPage);
            // Wait for request interception.
            await Page.WaitForRequestAsync("**/*");
            // Delete frame to cause request to be canceled.
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            await route.ContinueAsync();
        }

        [PlaywrightTest("page-route.spec.ts", "should intercept main resource during cross-process navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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

                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should fulfill with redirect status")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldFulfillWithRedirectStatus()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/title.html");
            HttpServer.Server.SetRoute("/final", context => context.Response.WriteAsync("foo"));
            await Page.RouteAsync("**/*", (route) =>
            {
                if (route.Request.Url != TestConstants.ServerUrl + "/redirect_this")
                {
                    route.ContinueAsync();
                    return;
                }

                _ = route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = (int)HttpStatusCode.MovedPermanently,
                    Headers = new Dictionary<string, string>
                    {
                        ["location"] = "/final",
                    }
                });
            });

            string text = await Page.EvaluateAsync<string>(@"async url => {
              const data = await fetch(url);
              return data.text();
            }", TestConstants.ServerUrl + "/redirect_this");

            Assert.AreEqual("foo", text);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with GET")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithGET()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                var headers = route.Request.Url.EndsWith("allow")
                    ? new Dictionary<string, string> { ["access-control-allow-origin"] = "*" }
                    : new Dictionary<string, string>();

                _ = route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/json",
                    Headers = headers,
                    Status = (int)HttpStatusCode.OK,
                    Body = "[\"electric\", \"cars\"]"
                });
            });

            string[] resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars?allow', { mode: 'cors' });
                return response.json();
            }");

            Assert.AreEqual(new[] { "electric", "cars" }, resp);

            var exception = await AssertThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync<string>(@"async () => {
                const response = await fetch('https://example.com/cars?reject', { mode: 'cors' });
                return response.json();
            }"));

            StringAssert.Contains("failed", exception.Message);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with POST")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithPOST()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                _ = route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/json",
                    Headers = new Dictionary<string, string> { ["access-control-allow-origin"] = "*" },
                    Status = (int)HttpStatusCode.OK,
                    Body = "[\"electric\", \"cars\"]"
                });
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

            Assert.AreEqual(new[] { "electric", "cars" }, resp);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with different methods")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportCorsWithDifferentMethods()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                _ = route.FulfillAsync(new RouteFulfillOptions
                {
                    ContentType = "application/json",
                    Headers = new Dictionary<string, string> { ["access-control-allow-origin"] = "*" },
                    Status = (int)HttpStatusCode.OK,
                    Body = $"[\"{ route.Request.Method.ToString().ToUpper() }\", \"electric\", \"cars\"]"
                });
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

            Assert.AreEqual(new[] { "POST", "electric", "cars" }, resp);

            resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars', {
                  method: 'DELETE',
                  headers: { 'Content-Type': 'application/json' },
                  mode: 'cors',
                  body: JSON.stringify({ 'number': 1 }) 
                });
                return response.json();
            }");

            Assert.AreEqual(new[] { "DELETE", "electric", "cars" }, resp);
        }
    }
}
