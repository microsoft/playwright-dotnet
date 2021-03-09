using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class GoToTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GoToTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-goto.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with file URL")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithFileURL()
        {
            string fileurl = new Uri(TestUtils.GetWebServerFile(Path.Combine("frames", "two-frames.html"))).AbsoluteUri;
            await Page.GoToAsync(fileurl);
            Assert.Equal(fileurl.ToLower(), Page.Url.ToLower());
            Assert.Equal(3, Page.Frames.Length);
        }

        [PlaywrightTest("page-goto.spec.ts", "should use http for no protocol")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseHttpForNoProtocol()
        {
            await Page.GoToAsync(TestConstants.EmptyPage.Replace("http://", string.Empty));
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work cross-process")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkCrossProcess()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);

            string url = TestConstants.CrossProcessHttpPrefix + "/empty.html";
            IFrame requestFrame = null;
            Page.Request += (_, e) =>
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

        [PlaywrightTest("page-goto.spec.ts", "should capture iframe navigation request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureIframeNavigationRequest()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);

            IFrame requestFrame = null;
            Page.Request += (_, e) =>
            {
                if (e.Request.Url == TestConstants.ServerUrl + "/frames/frame.html")
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

        [PlaywrightTest("page-goto.spec.ts", "should capture cross-process iframe navigation request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureCrossProcessIframeNavigationRequest()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);

            IFrame requestFrame = null;
            Page.Request += (_, e) =>
            {
                if (e.Request.Url == TestConstants.CrossProcessHttpPrefix + "/frames/frame.html")
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

        [PlaywrightTest("page-goto.spec.ts", "should work with anchor navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAnchorNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
            await Page.GoToAsync($"{TestConstants.EmptyPage}#foo");
            Assert.Equal($"{TestConstants.EmptyPage}#foo", Page.Url);
            await Page.GoToAsync($"{TestConstants.EmptyPage}#bar");
            Assert.Equal($"{TestConstants.EmptyPage}#bar", Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with redirects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirects()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/empty.html");

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/redirect/1.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to about:blank")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToAboutBlank()
        {
            var response = await Page.GoToAsync(TestConstants.AboutBlank);
            Assert.Null(response);
        }

        [PlaywrightTest("page-goto.spec.ts", "should return response when page changes its URL after load")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnResponseWhenPageChangesItsURLAfterLoad()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/historyapi.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with subframes return 204")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithSubframesReturn204()
        {
            Server.SetRoute("/frames/frame.html", context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with subframes return 204")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenServerReturns204()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(
                () => Page.GoToAsync(TestConstants.EmptyPage));

            if (TestConstants.IsChromium)
            {
                Assert.Contains("net::ERR_ABORTED", exception.Message);
            }
            else if (TestConstants.IsFirefox)
            {
                Assert.Contains("NS_BINDING_ABORTED", exception.Message);
            }
            else
            {
                Assert.Contains("Aborted: 204 No Content", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to empty page with domcontentloaded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToEmptyPageWithDOMContentLoaded()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage, LifecycleEvent.DOMContentLoaded);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when page calls history API in beforeunload")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenPageCallsHistoryAPIInBeforeunload()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() =>
            {
                window.addEventListener('beforeunload', () => history.replaceState(null, 'initial', window.location.href), false);
            }");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenNavigatingToBadUrl()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(async () => await Page.GoToAsync("asdfasdf"));
            if (TestConstants.IsChromium || TestConstants.IsWebKit)
            {
                Assert.Contains("Cannot navigate to invalid URL", exception.Message);
            }
            else
            {
                Assert.Contains("Invalid url", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad SSL")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldFailWhenNavigatingToBadSSL()
        {
            Page.Request += (_, e) => Assert.NotNull(e.Request);
            Page.RequestFinished += (_, e) => Assert.NotNull(e.Request);
            Page.RequestFailed += (_, e) => Assert.NotNull(e.Request);

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(async () => await Page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html"));
            TestUtils.AssertSSLError(exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad SSL after redirects")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldFailWhenNavigatingToBadSSLAfterRedirects()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/empty.html");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(async () => await Page.GoToAsync(TestConstants.HttpsPrefix + "/redirect/1.html"));
            TestUtils.AssertSSLError(exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should not crash when navigating to bad SSL after a cross origin navigation")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldNotCrashWhenNavigatingToBadSSLAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            await Page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html").ContinueWith(_ => { });
        }

        [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle0 is passed as an option")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfNetworkidle0IsPassedAsAnOption()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle2 is passed as an option")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowIfNetworkidle2IsPassedAsAnOption()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle is passed as an option")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding maximum navigation timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingMaximumNavigationTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            var exception = await Assert.ThrowsAsync<TimeoutException>(async ()
                => await Page.GoToAsync(TestConstants.EmptyPage, timeout: 1));
            Assert.Contains("Timeout 1ms exceeded", exception.Message);
            Assert.Contains(TestConstants.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding maximum navigation timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingDefaultMaximumNavigationTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.DefaultNavigationTimeout = 2;
            Page.DefaultNavigationTimeout = 1;
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout 1ms exceeded", exception.Message);
            Assert.Contains(TestConstants.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding browser context navigation timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingBrowserContextNavigationTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.DefaultNavigationTimeout = 2;
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout 2ms exceeded", exception.Message);
            Assert.Contains(TestConstants.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding default maximum timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingDefaultMaximumTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.DefaultTimeout = 2;
            Page.DefaultTimeout = 1;
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout 1ms exceeded", exception.Message);
            Assert.Contains(TestConstants.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding browser context timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingBrowserContextTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.DefaultTimeout = 2;
            var exception = await Assert.ThrowsAsync<TimeoutException>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout 2ms exceeded", exception.Message);
            Assert.Contains(TestConstants.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should prioritize default navigation timeout over default timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPrioritizeDefaultNavigationTimeoutOverDefaultTimeout()
        {
            // Hang for request to the empty.html
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.DefaultTimeout = 0;
            Page.DefaultNavigationTimeout = 1;
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            Assert.Contains("Timeout 1ms exceeded", exception.Message);
            Assert.Contains(TestConstants.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should disable timeout when its set to 0")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDisableTimeoutWhenItsSetTo0()
        {
            bool loaded = false;
            void OnLoad(object sender, EventArgs e)
            {
                loaded = true;
                Page.Load -= OnLoad;
            }
            Page.Load += OnLoad;

            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html", LifecycleEvent.Load, null, 0);
            Assert.True(loaded);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when replaced by another navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenReplacedByAnotherNavigation()
        {
            Task anotherTask = null;

            // Hang for request to the empty.html
            Server.SetRoute("/empty.html", _ =>
            {
                anotherTask = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
                return Task.Delay(-1);
            });

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(async () => await Page.GoToAsync(TestConstants.EmptyPage));
            await anotherTask;

            if (TestConstants.IsChromium)
            {
                Assert.Contains("net::ERR_ABORTED", exception.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.Contains("cancelled", exception.Message);
            }
            else
            {
                Assert.Contains("NS_BINDING_ABORTED", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when navigating to valid url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNavigatingToValidUrl()
        {
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when navigating to data url")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNavigatingToDataUrl()
        {
            var response = await Page.GoToAsync("data:text/html,hello");
            Assert.Null(response);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when navigating to 404")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNavigatingTo404()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/not-found");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [PlaywrightTest("page-goto.spec.ts", "should return last response in redirect chain")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnLastResponseInRedirectChain()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/redirect/3.html");
            Server.SetRedirect("/redirect/3.html", TestConstants.EmptyPage);

            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/redirect/1.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(TestConstants.EmptyPage, response.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during navigation")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldNotLeakListenersDuringNavigation()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during bad navigation")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldNotLeakListenersDuringBadNavigation()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during navigation of 11 pages")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldNotLeakListenersDuringNavigationOf11Pages()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to dataURL and not fire dataURL requests")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e.Request);

            string dataUrl = "data:text/html,<div>yo</div>";
            var response = await Page.GoToAsync(dataUrl);
            Assert.Null(response);
            Assert.Empty(requests);
        }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to URL with hash and fire requests without hash")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToURLWithHashAndFireRequestsWithoutHash()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e.Request);

            var response = await Page.GoToAsync(TestConstants.EmptyPage + "#hash");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(TestConstants.EmptyPage, response.Url);
            Assert.Single(requests);
            Assert.Equal(TestConstants.EmptyPage, requests[0].Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with self requesting page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithSelfRequestingPage()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/self-request.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("self-request.html", response.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating and show the url at the error message")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldFailWhenNavigatingAndShowTheUrlAtTheErrorMessage()
        {
            const string url = TestConstants.HttpsPrefix + "/redirect/1.html";
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(async () => await Page.GoToAsync(url));
            Assert.Contains(url, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should be able to navigate to a page controlled by service worker")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToNavigateToAPageControlledByServiceWorker()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/fetch/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/fetch/sw.html");
        }

        [PlaywrightTest("page-goto.spec.ts", "should send referer")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendReferer()
        {
            string referer1 = null;
            string referer2 = null;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/grid.html", r => referer1 = r.Headers["Referer"]),
                Server.WaitForRequest("/digits/1.png", r => referer2 = r.Headers["Referer"]),
                Page.GoToAsync(TestConstants.ServerUrl + "/grid.html", referer: "http://google.com/")
            );

            Assert.Equal("http://google.com/", referer1);
            // Make sure subresources do not inherit referer.
            Assert.Equal(TestConstants.ServerUrl + "/grid.html", referer2);
            Assert.Equal(TestConstants.ServerUrl + "/grid.html", Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should reject referer option when setExtraHTTPHeaders provides referer")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectRefererOptionWhenSetExtraHTTPHeadersProvidesReferer()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["referer"] = "http://microsoft.com/"
            });

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(async () =>
                await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html", referer: "http://google.com/"));

            Assert.Contains("\"referer\" is already specified as extra HTTP header", exception.Message);
            Assert.Contains(TestConstants.ServerUrl + "/grid.html", exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should override referrer-policy")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldOverrideReferrerPolicy()
        {
            Server.Subscribe("/grid.html", context =>
            {
                context.Response.Headers["Referrer-Policy"] = "no-referrer";
            });

            string referer1 = null;
            string referer2 = null;

            var reqTask1 = Server.WaitForRequest("/grid.html", r => referer1 = r.Headers["Referer"]);
            var reqTask2 = Server.WaitForRequest("/digits/1.png", r => referer2 = r.Headers["Referer"]);
            await TaskUtils.WhenAll(
                reqTask1,
                reqTask2,
                Page.GoToAsync(TestConstants.ServerUrl + "/grid.html", referer: "http://microsoft.com/"));

            Assert.Equal("http://microsoft.com/", referer1);
            // Make sure subresources do not inherit referer.
            Assert.Null(referer2);
            Assert.Equal(TestConstants.ServerUrl + "/grid.html", Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when canceled by another navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenCanceledByAnotherNavigation()
        {
            Server.SetRoute("/one-style.html", _ => Task.Delay(10_000));
            var request = Server.WaitForRequest("/one-style.html");
            var failed = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", TestConstants.IsFirefox ? LifecycleEvent.Networkidle : LifecycleEvent.Load);
            await request;
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Assert.ThrowsAnyAsync<PlaywrightSharpException>(async () => await failed);
        }

        [PlaywrightTest("page-goto.spec.ts", "extraHTTPHeaders should be pushed to provisional page")]
        [Fact(Skip = "Skipped in Playwright")]
        public void ExtraHTTPHeadersShouldBePushedToProvisionalPage()
        {
        }
    }
}
