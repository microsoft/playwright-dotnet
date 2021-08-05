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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    [TestClass]
    public class PageRouteTests : PageTestEx
    {
        [PlaywrightTest("page-route.spec.ts", "should intercept")]
        public async Task ShouldIntercept()
        {
            bool intercepted = false;
            await Page.RouteAsync("**/empty.html", (route) =>
            {
                StringAssert.Contains(route.Request.Url, "empty.html");
                Assert.IsFalse(string.IsNullOrEmpty(route.Request.Headers["user-agent"]));
                Assert.AreEqual(HttpMethod.Get.Method, route.Request.Method);
                Assert.IsNull(route.Request.PostData);
                Assert.IsTrue(route.Request.IsNavigationRequest);
                Assert.AreEqual("document", route.Request.ResourceType);
                Assert.AreEqual(route.Request.Frame, Page.MainFrame);
                Assert.AreEqual("about:blank", route.Request.Frame.Url);
                route.ContinueAsync();
                intercepted = true;
            });

            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.IsTrue(response.Ok);
            Assert.IsTrue(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should unroute")]
        public async Task ShouldUnroute()
        {
            var intercepted = new List<int>();

            await Page.RouteAsync("**/*", (route) =>
            {
                intercepted.Add(1);
                route.ContinueAsync();
            });

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


            Action<IRoute> handler4 = (route) =>
            {
                intercepted.Add(4);
                route.ContinueAsync();
            };

            await Page.RouteAsync("**/empty.html", handler4);
            await Page.GotoAsync(Server.EmptyPage);
            CollectionAssert.AreEqual(new[] { 4 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html", handler4);
            await Page.GotoAsync(Server.EmptyPage);
            CollectionAssert.AreEqual(new[] { 3 }, intercepted.ToArray());

            intercepted.Clear();
            await Page.UnrouteAsync("**/empty.html");
            await Page.GotoAsync(Server.EmptyPage);
            CollectionAssert.AreEqual(new[] { 1 }, intercepted.ToArray());
        }

        [PlaywrightTest("page-route.spec.ts", "should work when POST is redirected with 302")]
        public async Task ShouldWorkWhenPostIsRedirectedWith302()
        {
            Server.SetRedirect("/rredirect", "/empty.html");
            await Page.GotoAsync(Server.EmptyPage);
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
        public async Task ShouldWorkWhenHeaderManipulationHeadersWithRedirect()
        {
            Server.SetRedirect("/rrredirect", "/empty.html");
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["foo"] = "bar" };
                route.ContinueAsync(new() { Headers = headers });
            });
            await Page.GotoAsync(Server.Prefix + "/rrredirect");
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to remove headers")]
        public async Task ShouldBeAbleToRemoveHeaders()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["foo"] = "bar" };
                headers.Remove("origin");
                route.ContinueAsync(new() { Headers = headers });
            });

            var originRequestHeader = Server.WaitForRequest("/empty.html", request => request.Headers["origin"]);
            await TaskUtils.WhenAll(
                originRequestHeader,
                Page.GotoAsync(Server.EmptyPage)
            );
            Assert.AreEqual(StringValues.Empty, originRequestHeader.Result);
        }

        [PlaywrightTest("page-route.spec.ts", "should contain referer header")]
        public async Task ShouldContainRefererHeader()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            await Page.GotoAsync(Server.Prefix + "/one-style.html");
            StringAssert.Contains(requests[1].Url, "/one-style.css");
            StringAssert.Contains(requests[1].Headers["referer"], "/one-style.html");
        }

        [PlaywrightTest("page-route.spec.ts", "should properly return navigation response when URL has cookies")]
        public async Task ShouldProperlyReturnNavigationResponseWhenURLHasCookies()
        {
            // Setup cookie.
            await Page.GotoAsync(Server.EmptyPage);
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
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
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.IsTrue(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirect inside sync XHR")]
        public async Task ShouldWorkWithRedirectInsideSyncXHR()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Server.SetRedirect("/logo.png", "/pptr.png");
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
        public async Task ShouldWorkWithCustomRefererHeaders()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = Server.EmptyPage });
            await Page.RouteAsync("**/*", (route) =>
            {
                Assert.AreEqual(Server.EmptyPage, route.Request.Headers["referer"]);
                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.IsTrue(response.Ok);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable")]
        public async Task ShouldBeAbortable()
        {
            await Page.RouteAsync(new Regex("\\.css"), (route) => route.AbortAsync());

            int failedRequests = 0;
            Page.RequestFailed += (_, _) => ++failedRequests;
            var response = await Page.GotoAsync(Server.Prefix + "/one-style.html");
            Assert.IsTrue(response.Ok);
            Assert.IsNull(response.Request.Failure);
            Assert.AreEqual(1, failedRequests);
        }

        [PlaywrightTest("page-route.spec.ts", "should be abortable with custom error codes")]
        public async Task ShouldBeAbortableWithCustomErrorCodes()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                route.AbortAsync(RequestAbortErrorCode.InternetDisconnected);
            });

            IRequest failedRequest = null;
            Page.RequestFailed += (_, e) => failedRequest = e;
            await Page.GotoAsync(Server.EmptyPage).ContinueWith(_ => { });
            Assert.IsNotNull(failedRequest);
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
        public async Task ShouldSendReferer()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://google.com/" });
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var requestTask = Server.WaitForRequest("/grid.html", request => request.Headers["referer"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.GotoAsync(Server.Prefix + "/grid.html")
            );
            Assert.AreEqual("http://google.com/", requestTask.Result[0]);
        }

        [PlaywrightTest("page-route.spec.ts", "should fail navigation when aborting main resource")]
        public async Task ShouldFailNavigationWhenAbortingMainResource()
        {
            await Page.RouteAsync("**/*", (route) => route.AbortAsync());
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(Server.EmptyPage));
            Assert.IsNotNull(exception);
            if (TestConstants.IsWebKit)
            {
                StringAssert.Contains(exception.Message, "Request intercepted");
            }
            else if (TestConstants.IsFirefox)
            {
                StringAssert.Contains(exception.Message, "NS_ERROR_FAILURE");
            }
            else
            {
                StringAssert.Contains(exception.Message, "net::ERR_FAILED");
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should not work with redirects")]
        public async Task ShouldNotWorkWithRedirects()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ContinueAsync();
                requests.Add(route.Request);
            });
            Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
            Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
            Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
            Server.SetRedirect("/non-existing-page-4.html", "/empty.html");
            var response = await Page.GotoAsync(Server.Prefix + "/non-existing-page.html");

            StringAssert.Contains(requests[0].Url, "non-existing-page.html");
            Assert.That.Collection(requests).HasExactly(1);
            Assert.AreEqual("document", requests[0].ResourceType);
            Assert.IsTrue(requests[0].IsNavigationRequest);

            var chain = new List<IRequest>();

            for (var request = response.Request; request != null; request = request.RedirectedFrom)
            {
                chain.Add(request);
                Assert.IsTrue(request.IsNavigationRequest);
            }

            Assert.AreEqual(5, chain.Count);
            StringAssert.Contains(chain[0].Url, "/empty.html");
            StringAssert.Contains(chain[1].Url, "/non-existing-page-4.html");
            StringAssert.Contains(chain[2].Url, "/non-existing-page-3.html");
            StringAssert.Contains(chain[3].Url, "/non-existing-page-2.html");
            StringAssert.Contains(chain[4].Url, "/non-existing-page.html");

            for (int i = 0; i < chain.Count; ++i)
            {
                var request = chain[i];
                Assert.IsTrue(request.IsNavigationRequest);
                Assert.AreEqual(i > 0 ? chain[i - 1] : null, chain[i].RedirectedTo);
            }
        }

        [PlaywrightTest("page-route.spec.ts", "should work with redirects for subresources")]
        public async Task ShouldWorkWithRedirectsForSubresources()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ContinueAsync();
                requests.Add(route.Request);
            });
            Server.SetRedirect("/one-style.css", "/two-style.css");
            Server.SetRedirect("/two-style.css", "/three-style.css");
            Server.SetRedirect("/three-style.css", "/four-style.css");
            Server.SetRoute("/four-style.css", context => context.Response.WriteAsync("body {box-sizing: border-box; }"));

            var response = await Page.GotoAsync(Server.Prefix + "/one-style.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            StringAssert.Contains(response.Url, "one-style.html");

            Assert.AreEqual(2, requests.Count);
            Assert.AreEqual("document", requests[0].ResourceType);
            StringAssert.Contains(requests[0].Url, "one-style.html");

            var request = requests[1];
            foreach (string url in new[] { "/one-style.css", "/two-style.css", "/three-style.css", "/four-style.css" })
            {
                Assert.AreEqual("stylesheet", request.ResourceType);
                StringAssert.Contains(request.Url, url);
                request = request.RedirectedTo;
            }

            Assert.IsNull(request);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with equal requests")]
        public async Task ShouldWorkWithEqualRequests()
        {
            await Page.GotoAsync(Server.EmptyPage);
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
                    _ = route.ContinueAsync();
                }
                spinner = !spinner;
            });

            var results = new List<string>();
            for (int i = 0; i < 3; ++i)
            {
                results.Add(await Page.EvaluateAsync<string>("fetch('/zzz').then(response => response.text()).catch (e => 'FAILED')"));
            }

            CollectionAssert.AreEqual(new[] { "11", "FAILED", "22" }, results);
        }

        [PlaywrightTest("page-route.spec.ts", "should navigate to dataURL and not fire dataURL requests")]
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
            Assert.IsNull(response);
            Assert.That.Collection(requests).IsEmpty();
        }

        [PlaywrightTest("page-route.spec.ts", "should be able to fetch dataURL and not fire dataURL requests")]
        public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
        {
            await Page.GotoAsync(Server.EmptyPage);

            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            string dataURL = "data:text/html,<div>yo</div>";
            string text = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", dataURL);
            Assert.AreEqual("<div>yo</div>", text);
            Assert.That.Collection(requests).IsEmpty();
        }

        [PlaywrightTest("page-route.spec.ts", "should navigate to URL with hash and and fire requests without hash")]
        [Ignore("Not implemented")]
        public async Task ShouldNavigateToURLWithHashAndAndFireRequestsWithoutHash()
        {
            var requests = new List<IRequest>();
            await Page.RouteAsync("**/*", (route) =>
            {
                requests.Add(route.Request);
                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(Server.EmptyPage + "#hash");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual(Server.EmptyPage, response.Url);
            Assert.That.Collection(requests).HasExactly(1);
            Assert.AreEqual(Server.EmptyPage, requests[0].Url);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server")]
        public async Task ShouldWorkWithEncodedServer()
        {
            // The requestWillBeSent will report encoded URL, whereas interception will
            // report URL as-is. @see crbug.com/759388
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var response = await Page.GotoAsync(Server.Prefix + "/some nonexisting page");
            Assert.AreEqual((int)HttpStatusCode.NotFound, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with badly encoded server")]
        public async Task ShouldWorkWithBadlyEncodedServer()
        {
            Server.SetRoute("/malformed?rnd=%911", _ => Task.CompletedTask);
            await Page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var response = await Page.GotoAsync(Server.Prefix + "/malformed?rnd=%911");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-route.spec.ts", "should work with encoded server - 2")]
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
            var response = await Page.GotoAsync($"data:text/html,<link rel=\"stylesheet\" href=\"{Server.EmptyPage}/fonts?helvetica|arial\"/>");
            Assert.IsNull(response);
            Assert.That.Collection(requests).HasExactly(1);
            Assert.AreEqual((int)HttpStatusCode.NotFound, (await requests[0].ResponseAsync()).Status);
        }

        [PlaywrightTest("page-route.spec.ts", @"should not throw ""Invalid Interception Id"" if the request was cancelled")]
        public async Task ShouldNotThrowInvalidInterceptionIdIfTheRequestWasCancelled()
        {
            await Page.SetContentAsync("<iframe></iframe>");
            IRoute route = null;
            await Page.RouteAsync("**/*", (r) => route = r);
            _ = Page.EvalOnSelectorAsync("iframe", "(frame, url) => frame.src = url", Server.EmptyPage);
            // Wait for request interception.
            await Page.WaitForRequestAsync("**/*");
            // Delete frame to cause request to be canceled.
            await Page.EvalOnSelectorAsync("iframe", "frame => frame.remove()");
            await route.ContinueAsync();
        }

        [PlaywrightTest("page-route.spec.ts", "should intercept main resource during cross-process navigation")]
        public async Task ShouldInterceptMainResourceDuringCrossProcessNavigation()
        {
            await Page.GotoAsync(Server.EmptyPage);
            bool intercepted = false;
            await Page.RouteAsync(Server.CrossProcessPrefix + "/empty.html", (route) =>
            {
                if (route.Request.Url.Contains(Server.CrossProcessPrefix + "/empty.html"))
                {
                    intercepted = true;
                }

                route.ContinueAsync();
            });
            var response = await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
            Assert.IsTrue(response.Ok);
            Assert.IsTrue(intercepted);
        }

        [PlaywrightTest("page-route.spec.ts", "should fulfill with redirect status")]
        [Skip(TestTargets.Webkit)]
        public async Task ShouldFulfillWithRedirectStatus()
        {
            await Page.GotoAsync(Server.Prefix + "/title.html");
            Server.SetRoute("/final", context => context.Response.WriteAsync("foo"));
            await Page.RouteAsync("**/*", (route) =>
            {
                if (route.Request.Url != Server.Prefix + "/redirect_this")
                {
                    route.ContinueAsync();
                    return;
                }

                _ = route.FulfillAsync(new()
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
            }", Server.Prefix + "/redirect_this");

            Assert.AreEqual("foo", text);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with GET")]
        public async Task ShouldSupportCorsWithGET()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                var headers = route.Request.Url.EndsWith("allow")
                    ? new() { ["access-control-allow-origin"] = "*" }
                    : new Dictionary<string, string>();

                _ = route.FulfillAsync(new()
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

            CollectionAssert.AreEqual(new[] { "electric", "cars" }, resp);

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync<string>(@"async () => {
                const response = await fetch('https://example.com/cars?reject', { mode: 'cors' });
                return response.json();
            }"));

            StringAssert.Contains(exception.Message, "failed");
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with POST")]
        public async Task ShouldSupportCorsWithPOST()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                _ = route.FulfillAsync(new()
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

            CollectionAssert.AreEqual(new[] { "electric", "cars" }, resp);
        }

        [PlaywrightTest("page-route.spec.ts", "should support cors with different methods")]
        public async Task ShouldSupportCorsWithDifferentMethods()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/cars*", (route) =>
            {
                _ = route.FulfillAsync(new()
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

            CollectionAssert.AreEqual(new[] { "POST", "electric", "cars" }, resp);

            resp = await Page.EvaluateAsync<string[]>(@"async () => {
                const response = await fetch('https://example.com/cars', {
                  method: 'DELETE',
                  headers: { 'Content-Type': 'application/json' },
                  mode: 'cors',
                  body: JSON.stringify({ 'number': 1 }) 
                });
                return response.json();
            }");

            CollectionAssert.AreEqual(new[] { "DELETE", "electric", "cars" }, resp);
        }
    }
}
