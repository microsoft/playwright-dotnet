﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.goto</playwright-describe>
    public class GoToTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GoToTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work cross-process</playwright-it>
        [Fact]
        public async Task ShouldWorkCrossProcess()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);

            var url = TestConstants.CrossProcessHttpPrefix + "/empty.html";
            IFrame requestFrame = null;
            Page.Request += (sender, e) =>
            {
                if (e.Request.Url == url)
                {
                    requestFrame = e.Request.Frame;
                }
            };

            var response = await Page.GoToAsync(url);
            Assert.Equal(url, Page.Url);
            Assert.Same(Page.MainFrame, response.Frame);
            Assert.Same(Page.MainFrame, requestFrame);
            Assert.Equal(url, response.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should capture iframe navigation request</playwright-it>
        [Fact]
        public async Task ShouldCaptureIframeNavigationRequest()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);

            IFrame requestFrame = null;
            Page.Request += (sender, e) =>
            {
                if (e.Request.Url == TestConstants.ServerUrl + "/frame/frame.html")
                {
                    requestFrame = e.Request.Frame;
                }
            };

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            Assert.Equal(TestConstants.ServerUrl + "/frames/one-frame.html", Page.Url);
            Assert.Same(Page.MainFrame, response.Frame);
            Assert.Equal(TestConstants.ServerUrl + "/frames/one-frame.html", response.Url);

            Assert.Equal(2, Page.Frames.Length);
            Assert.Same(Page.FirstChildFrame(), requestFrame);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should capture cross-process iframe navigation request</playwright-it>
        [Fact]
        public async Task ShouldCaptureCrossProcessIframeNavigationRequest()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);

            IFrame requestFrame = null;
            Page.Request += (sender, e) =>
            {
                if (e.Request.Url == TestConstants.CrossProcessHttpPrefix + "/frame/frame.html")
                {
                    requestFrame = e.Request.Frame;
                }
            };

            var response = await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/frames/one-frame.html");
            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/frames/one-frame.html", Page.Url);
            Assert.Same(Page.MainFrame, response.Frame);
            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/frames/one-frame.html", response.Url);

            Assert.Equal(2, Page.Frames.Length);
            Assert.Same(Page.FirstChildFrame(), requestFrame);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work with anchor navigation</playwright-it>
        [Fact]
        public async Task ShouldWorkWithAnchorNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
            await Page.GoToAsync($"{TestConstants.EmptyPage}#foo");
            Assert.Equal($"{TestConstants.EmptyPage}#foo", Page.Url);
            await Page.GoToAsync($"{TestConstants.EmptyPage}#bar");
            Assert.Equal($"{TestConstants.EmptyPage}#bar", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work with redirects</playwright-it>
        [Fact]
        public async Task ShouldWorkWithRedirects()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/empty.html");

            await Page.GoToAsync(TestConstants.ServerUrl + "/redirect/1.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should navigate to about:blank</playwright-it>
        [Fact]
        public async Task ShouldNavigateToAboutBlank()
        {
            var response = await Page.GoToAsync(TestConstants.AboutBlank);
            Assert.Null(response);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should return response when page changes its URL after load</playwright-it>
        [Fact]
        public async Task ShouldReturnResponseWhenPageChangesItsURLAfterLoad()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/historyapi.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work with subframes return 204</playwright-it>
        [Fact]
        public async Task ShouldWorkWithSubframesReturn204()
        {
            Server.SetRoute("/frames/frame.html", context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work with subframes return 204</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldFailWhenServerReturns204()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(
                () => Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("net::ERR_ABORTED", exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should navigate to empty page with domcontentloaded</playwright-it>
        [Fact]
        public async Task ShouldNavigateToEmptyPageWithDOMContentLoaded()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage, waitUntil: new[]
            {
                WaitUntilNavigation.DOMContentLoaded
            });
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should work when page calls history API in beforeunload</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenPageCallsHistoryAPIInBeforeunload()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() =>
            {
                window.addEventListener('beforeunload', () => history.replaceState(null, 'initial', window.location.href), false);
            }");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should fail when navigating to bad url</playwright-it>
        [Fact]
        public async Task ShouldFailWhenNavigatingToBadUrl()
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync("asdfasdf"));
            Assert.Contains("Cannot navigate to invalid URL", exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should fail when navigating to bad SSL</playwright-it>
        [Fact]
        public async Task ShouldFailWhenNavigatingToBadSSL()
        {
            Page.Request += (sender, e) => Assert.NotNull(e.Request);
            Page.RequestFinished += (sender, e) => Assert.NotNull(e.Request);
            Page.RequestFailed += (sender, e) => Assert.NotNull(e.Request);

            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html"));
            AssertSSLError(exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should fail when navigating to bad SSL after redirects</playwright-it>
        [Fact]
        public async Task ShouldFailWhenNavigatingToBadSSLAfterRedirects()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/empty.html");
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync(TestConstants.HttpsPrefix + "/redirect/1.html"));
            AssertSSLError(exception.Message);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should not crash when navigating to bad SSL after a cross origin navigation</playwright-it>
        [Fact]
        public async Task ShouldNotCrashWhenNavigatingToBadSSLAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            await Page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should throw if networkidle is passed as an option</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfNetworkidleIsPassedAsAnOption() { }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.goto</playwright-describe>
        ///<playwright-it>should throw if networkidle is passed as an option</playwright-it>
        [Fact]
        public async Task ShouldFailWhenMainResourcesFailedToLoad()
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync("http://localhost:44123/non-existing-url"));

            if (TestConstants.IsChromium)
            {
                Assert.Contains("net::ERR_CONNECTION_REFUSED", exception.Message);
            }
            else if (TestConstants.IsWebKit && TestConstants.IsWindows)
            {
                Assert.Contains("Couldn't connect to server", exception.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.Contains("Could not connect", exception.Message);
            }
            else
            {
                Assert.Contains("NS_ERROR_CONNECTION_REFUSED", exception.Message);
            }
        }

        [Fact]
        public async Task ShouldFailWhenExceedingMaximumNavigationTimeout()
        {
            Server.SetRoute("/empty.html", context => Task.Delay(-1));

            var exception = await Assert.ThrowsAnyAsync<Exception>(async ()
                => await Page.GoToAsync(TestConstants.EmptyPage, new NavigationOptions { Timeout = 1 }));
            Assert.Contains("Timeout of 1 ms exceeded", exception.Message);
        }

        [Fact]
        public async Task ShouldFailWhenExceedingDefaultMaximumNavigationTimeout()
        {
            Server.SetRoute("/empty.html", context => Task.Delay(-1));

            Page.DefaultNavigationTimeout = 1;
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout of 1 ms exceeded", exception.Message);
        }

        [Fact]
        public async Task ShouldFailWhenExceedingDefaultMaximumTimeout()
        {
            // Hang for request to the empty.html
            Server.SetRoute("/empty.html", context => Task.Delay(-1));
            Page.DefaultTimeout = 1;
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout of 1 ms exceeded", exception.Message);
        }

        [Fact]
        public async Task ShouldPrioritizeDefaultNavigationTimeoutOverDefaultTimeout()
        {
            // Hang for request to the empty.html
            Server.SetRoute("/empty.html", context => Task.Delay(-1));
            Page.DefaultTimeout = 0;
            Page.DefaultNavigationTimeout = 1;
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout of 1 ms exceeded", exception.Message);
        }

        [Fact]
        public async Task ShouldDisableTimeoutWhenItsSetTo0()
        {
            var loaded = false;
            void OnLoad(object sender, EventArgs e)
            {
                loaded = true;
                Page.Load -= OnLoad;
            }
            Page.Load += OnLoad;

            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html", new NavigationOptions { Timeout = 0, WaitUntil = new[] { WaitUntilNavigation.Load } });
            Assert.True(loaded);
        }

        [Fact]
        public async Task ShouldWorkWhenNavigatingToValidUrl()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        [Fact]
        public async Task ShouldWorkWhenNavigatingToDataUrl()
        {
            var response = await Page.GoToAsync("data:text/html,hello");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        [Fact]
        public async Task ShouldWorkWhenNavigatingTo404()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/not-found");
            Assert.Equal(HttpStatusCode.NotFound, response.Status);
        }

        [Fact]
        public async Task ShouldReturnLastResponseInRedirectChain()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/redirect/3.html");
            Server.SetRedirect("/redirect/3.html", TestConstants.EmptyPage);

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/redirect/1.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal(TestConstants.EmptyPage, response.Url);
        }

        [Fact]
        public async Task ShouldWaitForNetworkIdleToSucceedNavigation()
        {
            var responses = new List<TaskCompletionSource<Func<HttpResponse, Task>>>();
            var fetches = new Dictionary<string, TaskCompletionSource<bool>>();
            foreach (var url in new[] {
                "/fetch-request-a.js",
                "/fetch-request-b.js",
                "/fetch-request-c.js",
                "/fetch-request-d.js" })
            {
                fetches[url] = new TaskCompletionSource<bool>();
                Server.SetRoute(url, async context =>
                {
                    var taskCompletion = new TaskCompletionSource<Func<HttpResponse, Task>>();
                    responses.Add(taskCompletion);
                    fetches[context.Request.Path].SetResult(true);
                    var actionResponse = await taskCompletion.Task;
                    await actionResponse(context.Response).WithTimeout();
                });
            }

            var initialFetchResourcesRequested = Task.WhenAll(
                Server.WaitForRequest("/fetch-request-a.js"),
                Server.WaitForRequest("/fetch-request-b.js"),
                Server.WaitForRequest("/fetch-request-c.js")
            );
            var secondFetchResourceRequested = Server.WaitForRequest("/fetch-request-d.js");

            var pageLoaded = new TaskCompletionSource<bool>();
            void WaitPageLoad(object sender, EventArgs e)
            {
                pageLoaded.SetResult(true);
                Page.Load -= WaitPageLoad;
            }
            Page.Load += WaitPageLoad;

            var navigationFinished = false;
            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/networkidle.html",
                new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } })
                .ContinueWith(res =>
                {
                    navigationFinished = true;
                    return res.Result;
                });

            await pageLoaded.Task.WithTimeout();

            Assert.False(navigationFinished);

            await initialFetchResourcesRequested.WithTimeout();

            Assert.False(navigationFinished);

            await Task.WhenAll(
                fetches["/fetch-request-a.js"].Task,
                fetches["/fetch-request-b.js"].Task,
                fetches["/fetch-request-c.js"].Task).WithTimeout();

            foreach (var actionResponse in responses)
            {
                actionResponse.SetResult(response =>
                {
                    response.StatusCode = 404;
                    return response.WriteAsync("File not found");
                });
            }

            responses.Clear();

            await secondFetchResourceRequested.WithTimeout();

            Assert.False(navigationFinished);

            await fetches["/fetch-request-d.js"].Task.WithTimeout();

            foreach (var actionResponse in responses)
            {
                actionResponse.SetResult(response =>
                {
                    response.StatusCode = 404;
                    return response.WriteAsync("File not found");
                });
            }

            var navigationResponse = await navigationTask;
            Assert.Equal(HttpStatusCode.OK, navigationResponse.Status);
        }

        [Fact]
        public async Task ShouldNavigateToDataURLAndFireDataURLRequests()
        {
            var requests = new List<Request>();
            Page.Request += (sender, e) =>
            {
                if (!TestUtils.IsFavicon(e.Request))
                {
                    requests.Add(e.Request);
                }
            };
            var dataUrl = "data:text/html,<div>yo</div>";
            var response = await Page.GoToAsync(dataUrl);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Single(requests);
            Assert.Equal(dataUrl, requests[0].Url);
        }

        [Fact]
        public async Task ShouldNavigateToURLWithHashAndFireRequestsWithoutHash()
        {
            var requests = new List<Request>();
            Page.Request += (sender, e) =>
            {
                if (!TestUtils.IsFavicon(e.Request))
                {
                    requests.Add(e.Request);
                }
            };
            var response = await Page.GoToAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal(TestConstants.EmptyPage, response.Url);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
        }

        [Fact]
        public async Task ShouldWorkWithSelfRequestingPage()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/self-request.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("self-request.html", response.Url);
        }

        [Fact]
        public async Task ShouldFailWhenNavigatingAndShowTheUrlAtTheErrorMessage()
        {
            var url = TestConstants.HttpsPrefix + "/redirect/1.html";
            var exception = await Assert.ThrowsAnyAsync<NavigationException>(async () => await Page.GoToAsync(url));
            Assert.Contains(url, exception.Message);
            Assert.Contains(url, exception.Url);
        }

        [Fact]
        public async Task ResponseOkShouldBeTrueForFile()
        {
            var fileToNavigate = Path.Combine(Directory.GetCurrentDirectory(), Path.Combine("Assets", "file-to-upload.txt"));
            var url = new Uri(fileToNavigate).AbsoluteUri;

            var response = await Page.GoToAsync(url);
            Assert.True(response.Ok);
        }

        [Fact]
        public async Task ShouldSendReferer()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            string referer1 = null;
            string referer2 = null;

            await Task.WhenAll(
                Server.WaitForRequest("/grid.html", r => referer1 = r.Headers["Referer"]),
                Server.WaitForRequest("/digits/1.png", r => referer2 = r.Headers["Referer"]),
                Page.GoToAsync(TestConstants.ServerUrl + "/grid.html", new NavigationOptions
                {
                    Referer = "http://google.com/"
                })
            );

            Assert.Equal("http://google.com/", referer1);
            // Make sure subresources do not inherit referer.
            Assert.Equal(TestConstants.ServerUrl + "/grid.html", referer2);
        }

        private void AssertSSLError(string errorMessage)
        {
            if (TestConstants.IsChromium)
            {
                Assert.Contains("net::ERR_CERT_AUTHORITY_INVALID", errorMessage);
            }
            else if (TestConstants.IsWebKit)
            {
                if (TestConstants.IsMacOSX)
                    Assert.Contains("The certificate for this server is invalid", errorMessage);
                else if (TestConstants.IsWindows)
                    Assert.Contains("SSL peer certificate or SSH remote key was not OK", errorMessage);
                else
                    Assert.Contains("Unacceptable TLS certificate", errorMessage);
            }
            else
            {
                Assert.Contains("SSL_ERROR_UNKNOWN", errorMessage);
            }
        }
    }
}