using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Page.setRequestInterception</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetRequestInterceptionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetRequestInterceptionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should intercept</playwright-it>
        [Fact]
        public async Task ShouldIntercept()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                Assert.Contains("empty.html", e.Request.Url);
                Assert.True(e.Request.Headers.ContainsKey("User-Agent"));
                Assert.Equal(HttpMethod.Get, e.Request.Method);
                Assert.Null(e.Request.PostData);
                Assert.True(e.Request.IsNavigationRequest);
                Assert.Equal(ResourceType.Document, e.Request.ResourceType);
                Assert.Same(e.Request.Frame, Page.MainFrame);
                Assert.Equal("about:blank", e.Request.Frame.Url);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work when POST is redirected with 302</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenPostIsRedirectedWith302()
        {
            Server.SetRedirect("/rredirect", "/empty.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            await Page.SetContentAsync(@"
                <form action='/rredirect' method='post'>
                    <input type=""hidden"" id=""foo"" name=""foo"" value=""FOOBAR"">
                </form>");
            await Task.WhenAll(
                Page.QuerySelectorEvaluateAsync("form", "form => form.submit()"),
                Page.WaitForNavigationAsync()
            );
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work when header manipulation headers with redirect</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenHeaderManipulationHeadersWithRedirect()
        {
            Server.SetRedirect("/rrredirect", "/empty.html");
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                var headers = new Dictionary<string, string>(e.Request.Headers) { ["foo"] = "bar" };
                e.Request.ContinueAsync(new Payload { Headers = headers });
            };
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
        }


        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be able to remove headers</playwright-it>
        [Fact]
        public async Task ShouldBeAbleToRemoveHeaders()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                var headers = new Dictionary<string, string>(e.Request.Headers) { ["foo"] = "bar" };
                headers.Remove("origin");
                e.Request.ContinueAsync(new Payload { Headers = headers });
            };

            var originRequestHeader = Server.WaitForRequest("/empty.html", request => request.Headers["origin"]);
            await Task.WhenAll(
                originRequestHeader,
                Page.GoToAsync(TestConstants.EmptyPage)
            );
            Assert.Equal(StringValues.Empty, originRequestHeader.Result);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should contain referer header</playwright-it>
        [Fact]
        public async Task ShouldContainRefererHeader()
        {
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += (sender, e) =>
            {
                requests.Add(e.Request);
                e.Request.ContinueAsync();
            };
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Contains("/one-style.css", requests[1].Url);
            Assert.Contains("/one-style.html", requests[1].Headers["Referer"]);
        }


        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should properly return navigation response when URL has cookies</playwright-it>
        [Fact]
        public async Task ShouldProperlyReturnNavigationResponseWhenURLHasCookies()
        {
            // Setup cookie.
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "foo",
                Value = "bar"
            });

            // Setup request interception.
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) => e.Request.ContinueAsync();
            var response = await Page.ReloadAsync();
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should stop intercepting</playwright-it>
        [Fact]
        public async Task ShouldStopIntercepting()
        {
            await Page.SetRequestInterceptionAsync(true);
            async void ContinueRequestOnce(object sender, RequestEventArgs e)
            {
                Page.Request -= ContinueRequestOnce;
                await e.Request.ContinueAsync();
            }
            Page.Request += ContinueRequestOnce;
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(false);
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should show custom HTTP headers</playwright-it>
        [Fact]
        public async Task ShouldShowCustomHTTPHeaders()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["foo"] = "bar"
            });
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                Assert.Equal("bar", e.Request.Headers["foo"]);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with redirect inside sync XHR</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirectInsideSyncXHR()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Server.SetRedirect("/logo.png", "/pptr.png");
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            int status = await Page.EvaluateAsync<int>(@"async () => {
                var request = new XMLHttpRequest();
                request.open('GET', '/logo.png', false);  // `false` makes the request synchronous
                request.send(null);
                return request.status;
            }");
            Assert.Equal(200, status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with custom referer headers</playwright-it>
        [Fact]
        public async Task ShouldWorkWithCustomRefererHeaders()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { ["Referer"] = TestConstants.EmptyPage });
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                Assert.Equal(TestConstants.EmptyPage, e.Request.Headers["Referer"]);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.True(response.Ok);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be abortable</playwright-it>
        [Fact]
        public async Task ShouldBeAbortable()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                if (e.Request.Url.EndsWith(".css"))
                {
                    await e.Request.AbortAsync();
                }
                else
                {
                    await e.Request.ContinueAsync();
                }
            };
            int failedRequests = 0;
            Page.RequestFailed += (sender, e) => ++failedRequests;
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.True(response.Ok);
            Assert.Null(response.Request.Failure);
            Assert.Equal(1, failedRequests);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be abortable with custom error codes</playwright-it>
        [Fact]
        public async Task ShouldBeAbortableWithCustomErrorCodes()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) =>
            {
                e.Request.AbortAsync(RequestAbortErrorCode.InternetDisconnected);
            };
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

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should send referer</playwright-it>
        [Fact]
        public async Task ShouldSendReferer()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { ["referer"] = "http://google.com/" });
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var requestTask = Server.WaitForRequest("/grid.html", request => request.Headers["referer"]);
            await Task.WhenAll(
                requestTask,
                Page.GoToAsync(TestConstants.ServerUrl + "/grid.html")
            );
            Assert.Equal("http://google.com/", requestTask.Result);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should fail navigation when aborting main resource</playwright-it>
        [Fact]
        public async Task ShouldFailNavigationWhenAbortingMainResource()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.AbortAsync();
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

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with redirects</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirects()
        {
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += async (sender, e) =>
            {
                await e.Request.ContinueAsync();
                requests.Add(e.Request);
            };
            Server.SetRedirect("/non-existing-page.html", "/non-existing-page-2.html");
            Server.SetRedirect("/non-existing-page-2.html", "/non-existing-page-3.html");
            Server.SetRedirect("/non-existing-page-3.html", "/non-existing-page-4.html");
            Server.SetRedirect("/non-existing-page-4.html", "/empty.html");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/non-existing-page.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("empty.html", response.Url);
            Assert.Equal(5, requests.Count);
            Assert.Equal(ResourceType.Document, requests[2].ResourceType);
            // Check redirect chain
            var redirectChain = response.Request.RedirectChain;
            Assert.Equal(4, redirectChain.Length);
            Assert.Contains("/non-existing-page.html", redirectChain[0].Url);
            Assert.Contains("/non-existing-page-3.html", redirectChain[2].Url);
            for (int i = 0; i < redirectChain.Length; ++i)
            {
                var request = redirectChain[i];
                Assert.True(request.IsNavigationRequest);
                Assert.Equal(request.RedirectChain[i], request);
            }
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with redirects for subresources</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirectsForSubresources()
        {
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += async (sender, e) =>
            {
                await e.Request.ContinueAsync();
                requests.Add(e.Request);
            };
            Server.SetRedirect("/one-style.css", "/two-style.css");
            Server.SetRedirect("/two-style.css", "/three-style.css");
            Server.SetRedirect("/three-style.css", "/four-style.css");
            Server.SetRoute("/four-style.css", context => context.Response.WriteAsync("body {box-sizing: border-box; }"));

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("one-style.html", response.Url);
            Assert.Equal(5, requests.Count);
            Assert.Equal(ResourceType.Document, requests[0].ResourceType);
            Assert.Equal(ResourceType.StyleSheet, requests[1].ResourceType);
            // Check redirect chain
            var redirectChain = requests[1].RedirectChain;
            Assert.Equal(3, redirectChain.Length);
            Assert.Contains("/one-style.css", redirectChain[0].Url);
            Assert.Contains("/three-style.css", redirectChain[2].Url);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with equal requests</playwright-it>
        [Fact]
        public async Task ShouldWorkWithEqualRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            int responseCount = 1;
            Server.SetRoute("/zzz", context => context.Response.WriteAsync((responseCount++ * 11).ToString()));
            await Page.SetRequestInterceptionAsync(true);

            bool spinner = false;
            // Cancel 2nd request.
            Page.Request += async (sender, e) =>
            {
                if (spinner)
                {
                    _ = e.Request.AbortAsync();
                }
                else
                {
                    _ = e.Request.ContinueAsync();
                }
                spinner = !spinner;
            };
            var results = await Page.EvaluateAsync<string[]>(@"() => Promise.all([
                fetch('/zzz').then(response => response.text()).catch (e => 'FAILED'),
                fetch('/zzz').then(response => response.text()).catch (e => 'FAILED'),
                fetch('/zzz').then(response => response.text()).catch (e => 'FAILED'),
            ])");
            Assert.Equal(new[] { "11", "FAILED", "22" }, results);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should navigate to dataURL and not fire dataURL requests</playwright-it>
        [Fact]
        public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
        {
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += async (sender, e) =>
            {
                requests.Add(e.Request);
                await e.Request.ContinueAsync();
            };
            string dataURL = "data:text/html,<div>yo</div>";
            var response = await Page.GoToAsync(dataURL);
            Assert.Null(response);
            Assert.Empty(requests);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should be able to fetch dataURL and not fire dataURL requests</playwright-it>
        [Fact]
        public async Task ShouldBeAbleToFetchDataURLAndNotFireDataURLRequests()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += async (sender, e) =>
            {
                requests.Add(e.Request);
                await e.Request.ContinueAsync();
            };
            string dataURL = "data:text/html,<div>yo</div>";
            string text = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", dataURL);
            Assert.Equal("<div>yo</div>", text);
            Assert.Empty(requests);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should navigate to URL with hash and and fire requests without hash</playwright-it>
        [Fact(Skip = "Not implemented")]
        public async Task ShouldNavigateToURLWithHashAndAndFireRequestsWithoutHash()
        {
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += async (sender, e) =>
            {
                requests.Add(e.Request);
                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal(TestConstants.EmptyPage, response.Url);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with encoded server</playwright-it>
        [Fact]
        public async Task ShouldWorkWithEncodedServer()
        {
            // The requestWillBeSent will report encoded URL, whereas interception will
            // report URL as-is. @see crbug.com/759388
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/some nonexisting page");
            Assert.Equal(HttpStatusCode.NotFound, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with badly encoded server</playwright-it>
        [Fact]
        public async Task ShouldWorkWithBadlyEncodedServer()
        {
            await Page.SetRequestInterceptionAsync(true);
            Server.SetRoute("/malformed?rnd=%911", context => Task.CompletedTask);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/malformed?rnd=%911");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should work with encoded server - 2</playwright-it>
        [Fact]
        public async Task ShouldWorkWithEncodedServer2()
        {
            // The requestWillBeSent will report URL as-is, whereas interception will
            // report encoded URL for stylesheet. @see crbug.com/759388
            await Page.SetRequestInterceptionAsync(true);
            var requests = new List<IRequest>();
            Page.Request += async (sender, e) =>
            {
                await e.Request.ContinueAsync();
                requests.Add(e.Request);
            };
            var response = await Page.GoToAsync($"data:text/html,<link rel=\"stylesheet\" href=\"{TestConstants.EmptyPage}/fonts?helvetica|arial\"/>");
            Assert.Null(response);
            Assert.Single(requests);
            Assert.Equal(HttpStatusCode.NotFound, requests[0].Response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should not throw "Invalid Interception Id" if the request was cancelled</playwright-it>
        [Fact]
        public async Task ShouldNotThrowInvalidInterceptionIdIfTheRequestWasCancelled()
        {
            await Page.SetContentAsync("<iframe></iframe>");
            await Page.SetRequestInterceptionAsync(true);
            IRequest request = null;
            Page.Request += (sender, e) => request = e.Request;
            _ = Page.QuerySelectorEvaluateAsync("iframe", "(frame, url) => frame.src = url", TestConstants.EmptyPage);
            // Wait for request interception.
            await Page.WaitForEvent<RequestEventArgs>(PageEvent.Request);
            // Delete frame to cause request to be canceled.
            _ = Page.QuerySelectorEvaluateAsync("iframe", "frame => frame.remove()");
            await request.ContinueAsync();
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should throw if interception is not enabled</playwright-it>
        [Fact]
        public async Task ShouldThrowIfInterceptionIsNotEnabled()
        {
            var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            Exception exception = null;
            page.Request += async (sender, e) =>
            {
                try { await e.Request.ContinueAsync(); }
                catch (Exception ex)
                {
                    exception = ex;
                }
            };
            await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Contains("Request Interception is not enabled", exception.Message);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should intercept main resource during cross-process navigation</playwright-it>
        [Fact]
        public async Task ShouldInterceptMainResourceDuringCrossProcessNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(true);
            bool intercepted = false;
            Page.Request += async (sender, e) =>
            {
                if (e.Request.Url.Contains(TestConstants.CrossProcessHttpPrefix + "/empty.html"))
                {
                    intercepted = true;
                }

                await e.Request.ContinueAsync();
            };
            var response = await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            Assert.True(response.Ok);
            Assert.True(intercepted);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should not throw when continued after navigation</playwright-it>
        [Fact]
        public async Task ShouldNotThrowWhenContinuedAfterNavigation()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                if (e.Request.Url != TestConstants.ServerUrl + "/one-style.css")
                {
                    await e.Request.ContinueAsync();
                }
            };
            // For some reason, Firefox issues load event with one outstanding request.
            var failed = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", new GoToOptions
            {
                WaitUntil = TestConstants.IsFirefox ? new[] { WaitUntilNavigation.Networkidle0 } : new[] { WaitUntilNavigation.Load }
            });
            var request = await Page.WaitForRequestAsync(TestConstants.ServerUrl + "/one-style.css");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => failed);
            Assert.Equal("Navigation to " + TestConstants.ServerUrl + "/one-style.html was canceled by another one", exception.Message);
            await request.ContinueAsync();
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.setRequestInterception</playwright-describe>
        ///<playwright-it>should not throw when continued after cross-process navigation</playwright-it>
        [Fact]
        public async Task ShouldNotThrowWhenContinuedAfterCrossProcessNavigation()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                if (e.Request.Url != TestConstants.ServerUrl + "/one-style.css")
                {
                    await e.Request.ContinueAsync();
                }
            };
            // For some reason, Firefox issues load event with one outstanding request.
            var failed = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", new GoToOptions
            {
                WaitUntil = TestConstants.IsFirefox ? new[] { WaitUntilNavigation.Networkidle0 } : new[] { WaitUntilNavigation.Load }
            });
            var request = await Page.WaitForRequestAsync(TestConstants.ServerUrl + "/one-style.css");
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => failed);
            Assert.Equal("Navigation to " + TestConstants.ServerUrl + "/one-style.html was canceled by another one", exception.Message);
            await request.ContinueAsync();
        }
    }
}
