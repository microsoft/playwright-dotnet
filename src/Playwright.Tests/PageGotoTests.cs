using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageGotoTests : PageTestEx
    {
        [PlaywrightTest("page-goto.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with file URL")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithFileURL()
        {
            string fileurl = new Uri(TestUtils.GetWebServerFile(Path.Combine("frames", "two-frames.html"))).AbsoluteUri;
            await Page.GotoAsync(fileurl);
            Assert.AreEqual(fileurl.ToLower(), Page.Url.ToLower());
            Assert.AreEqual(3, Page.Frames.Count);
        }

        [PlaywrightTest("page-goto.spec.ts", "should use http for no protocol")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUseHttpForNoProtocol()
        {
            await Page.GotoAsync(Server.EmptyPage.Replace("http://", string.Empty));
            Assert.AreEqual(Server.EmptyPage, Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work cross-process")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkCrossProcess()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);

            string url = Server.CrossProcessPrefix + "/empty.html";
            IFrame requestFrame = null;
            Page.Request += (_, e) =>
            {
                if (e.Url == url)
                {
                    requestFrame = e.Frame;
                }
            };

            var response = await Page.GotoAsync(url);
            Assert.AreEqual(url, Page.Url);
            Assert.AreEqual(Page.MainFrame, response.Frame);
            Assert.AreEqual(Page.MainFrame, requestFrame);
            Assert.AreEqual(url, response.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should capture iframe navigation request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureIframeNavigationRequest()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);

            IFrame requestFrame = null;
            Page.Request += (_, e) =>
            {
                if (e.Url == Server.Prefix + "/frames/frame.html")
                {
                    requestFrame = e.Frame;
                }
            };

            var response = await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            Assert.AreEqual(Server.Prefix + "/frames/one-frame.html", Page.Url);
            Assert.AreEqual(Page.MainFrame, response.Frame);
            Assert.AreEqual(Server.Prefix + "/frames/one-frame.html", response.Url);

            Assert.AreEqual(2, Page.Frames.Count);
            Assert.AreEqual(Page.FirstChildFrame(), requestFrame);
        }

        [PlaywrightTest("page-goto.spec.ts", "should capture cross-process iframe navigation request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureCrossProcessIframeNavigationRequest()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);

            IFrame requestFrame = null;
            Page.Request += (_, e) =>
            {
                if (e.Url == Server.CrossProcessPrefix + "/frames/frame.html")
                {
                    requestFrame = e.Frame;
                }
            };

            var response = await Page.GotoAsync(Server.CrossProcessPrefix + "/frames/one-frame.html");
            Assert.AreEqual(Server.CrossProcessPrefix + "/frames/one-frame.html", Page.Url);
            Assert.AreEqual(Page.MainFrame, response.Frame);
            Assert.AreEqual(Server.CrossProcessPrefix + "/frames/one-frame.html", response.Url);

            Assert.AreEqual(2, Page.Frames.Count);
            Assert.AreEqual(Page.FirstChildFrame(), requestFrame);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with anchor navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAnchorNavigation()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual(Server.EmptyPage, Page.Url);
            await Page.GotoAsync($"{Server.EmptyPage}#foo");
            Assert.AreEqual($"{Server.EmptyPage}#foo", Page.Url);
            await Page.GotoAsync($"{Server.EmptyPage}#bar");
            Assert.AreEqual($"{Server.EmptyPage}#bar", Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with redirects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirects()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/empty.html");

            var response = await Page.GotoAsync(Server.Prefix + "/redirect/1.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            await Page.GotoAsync(Server.EmptyPage);
        }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to about:blank")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToAboutBlank()
        {
            var response = await Page.GotoAsync(TestConstants.AboutBlank);
            Assert.Null(response);
        }

        [PlaywrightTest("page-goto.spec.ts", "should return response when page changes its URL after load")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnResponseWhenPageChangesItsURLAfterLoad()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/historyapi.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with subframes return 204")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public Task ShouldWorkWithSubframesReturn204()
        {
            Server.SetRoute("/frames/frame.html", context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            return Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with subframes return 204")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenServerReturns204()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(
                () => Page.GotoAsync(Server.EmptyPage));

            if (TestConstants.IsChromium)
            {
                StringAssert.Contains("net::ERR_ABORTED", exception.Message);
            }
            else if (TestConstants.IsFirefox)
            {
                StringAssert.Contains("NS_BINDING_ABORTED", exception.Message);
            }
            else
            {
                StringAssert.Contains("Aborted: 204 No Content", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to empty page with domcontentloaded")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToEmptyPageWithDOMContentLoaded()
        {
            var response = await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when page calls history API in beforeunload")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenPageCallsHistoryAPIInBeforeunload()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync(@"() =>
            {
                window.addEventListener('beforeunload', () => history.replaceState(null, 'initial', window.location.href), false);
            }");
            var response = await Page.GotoAsync(Server.Prefix + "/grid.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenNavigatingToBadUrl()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync("asdfasdf"));
            if (TestConstants.IsChromium || TestConstants.IsWebKit)
            {
                StringAssert.Contains("Cannot navigate to invalid URL", exception.Message);
            }
            else
            {
                StringAssert.Contains("Invalid url", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad SSL")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldFailWhenNavigatingToBadSSL()
        {
            Page.Request += (_, e) => Assert.NotNull(e);
            Page.RequestFinished += (_, e) => Assert.NotNull(e);
            Page.RequestFailed += (_, e) => Assert.NotNull(e);

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(HttpsServer.Prefix + "/empty.html"));
            TestUtils.AssertSSLError(exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad SSL after redirects")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldFailWhenNavigatingToBadSSLAfterRedirects()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/empty.html");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(HttpsServer.Prefix + "/redirect/1.html"));
            TestUtils.AssertSSLError(exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should not crash when navigating to bad SSL after a cross origin navigation")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldNotCrashWhenNavigatingToBadSSLAfterACrossOriginNavigation()
        {
            await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
            await Page.GotoAsync(HttpsServer.Prefix + "/empty.html").ContinueWith(_ => { });
        }

        [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle0 is passed as an option")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowIfNetworkIdle0IsPassedAsAnOption()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle2 is passed as an option")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowIfNetworkIdle2IsPassedAsAnOption()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle is passed as an option")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenMainResourcesFailedToLoad()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync("http://localhost:44123/non-existing-url"));

            if (TestConstants.IsChromium)
            {
                StringAssert.Contains("net::ERR_CONNECTION_REFUSED", exception.Message);
            }
            else if (TestConstants.IsWebKit && TestConstants.IsWindows)
            {
                StringAssert.Contains("Couldn't connect to server", exception.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                StringAssert.Contains("Could not connect", exception.Message);
            }
            else
            {
                StringAssert.Contains("NS_ERROR_CONNECTION_REFUSED", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding maximum navigation timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingMaximumNavigationTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
                => Page.GotoAsync(Server.EmptyPage, new() { Timeout = 1 }));
            StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
            StringAssert.Contains(Server.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding maximum navigation timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingDefaultMaximumNavigationTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.SetDefaultNavigationTimeout(2);
            Page.SetDefaultNavigationTimeout(1);
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
            StringAssert.Contains(Server.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding browser context navigation timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingBrowserContextNavigationTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.SetDefaultNavigationTimeout(2);
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains("Timeout 2ms exceeded", exception.Message);
            StringAssert.Contains(Server.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding default maximum timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingDefaultMaximumTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.SetDefaultTimeout(2);
            Page.SetDefaultTimeout(1);
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
            StringAssert.Contains(Server.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding browser context timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenExceedingBrowserContextTimeout()
        {
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.Context.SetDefaultTimeout(2);
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains("Timeout 2ms exceeded", exception.Message);
            StringAssert.Contains(Server.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should prioritize default navigation timeout over default timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldPrioritizeDefaultNavigationTimeoutOverDefaultTimeout()
        {
            // Hang for request to the empty.html
            Server.SetRoute("/empty.html", _ => Task.Delay(-1));
            Page.SetDefaultTimeout(0);
            Page.SetDefaultNavigationTimeout(1);
            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
            StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
            StringAssert.Contains(Server.EmptyPage, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should disable timeout when its set to 0")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDisableTimeoutWhenItsSetTo0()
        {
            bool loaded = false;
            void OnLoad(object sender, IPage e)
            {
                loaded = true;
                Page.Load -= OnLoad;
            }
            Page.Load += OnLoad;

            await Page.GotoAsync(Server.Prefix + "/grid.html", new() { WaitUntil = WaitUntilState.Load, Timeout = 0 });
            Assert.True(loaded);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when replaced by another navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenReplacedByAnotherNavigation()
        {
            Task anotherTask = null;

            // Hang for request to the empty.html
            Server.SetRoute("/empty.html", _ =>
            {
                anotherTask = Page.GotoAsync(Server.Prefix + "/one-style.html");
                return Task.Delay(-1);
            });

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(Server.EmptyPage));
            await anotherTask;

            if (TestConstants.IsChromium)
            {
                StringAssert.Contains("net::ERR_ABORTED", exception.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                StringAssert.Contains("cancelled", exception.Message);
            }
            else
            {
                StringAssert.Contains("NS_BINDING_ABORTED", exception.Message);
            }
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when navigating to valid url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNavigatingToValidUrl()
        {
            var response = await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when navigating to data url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNavigatingToDataUrl()
        {
            var response = await Page.GotoAsync("data:text/html,hello");
            Assert.Null(response);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work when navigating to 404")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhenNavigatingTo404()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/not-found");
            Assert.AreEqual((int)HttpStatusCode.NotFound, response.Status);
        }

        [PlaywrightTest("page-goto.spec.ts", "should return last response in redirect chain")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnLastResponseInRedirectChain()
        {
            Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
            Server.SetRedirect("/redirect/2.html", "/redirect/3.html");
            Server.SetRedirect("/redirect/3.html", Server.EmptyPage);

            var response = await Page.GotoAsync(Server.Prefix + "/redirect/1.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual(Server.EmptyPage, response.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during navigation")]
        [Test, Ignore("We don't need this test")]
        public void ShouldNotLeakListenersDuringNavigation()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during bad navigation")]
        [Test, Ignore("We don't need this test")]
        public void ShouldNotLeakListenersDuringBadNavigation()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during navigation of 11 pages")]
        [Test, Ignore("We don't need this test")]
        public void ShouldNotLeakListenersDuringNavigationOf11Pages()
        { }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to dataURL and not fire dataURL requests")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToDataURLAndNotFireDataURLRequests()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);

            string dataUrl = "data:text/html,<div>yo</div>";
            var response = await Page.GotoAsync(dataUrl);
            Assert.Null(response);
            Assert.IsEmpty(requests);
        }

        [PlaywrightTest("page-goto.spec.ts", "should navigate to URL with hash and fire requests without hash")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNavigateToURLWithHashAndFireRequestsWithoutHash()
        {
            var requests = new List<IRequest>();
            Page.Request += (_, e) => requests.Add(e);

            var response = await Page.GotoAsync(Server.EmptyPage + "#hash");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual(Server.EmptyPage, response.Url);
            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.AreEqual(Server.EmptyPage, requests[0].Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should work with self requesting page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithSelfRequestingPage()
        {
            var response = await Page.GotoAsync(Server.Prefix + "/self-request.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            StringAssert.Contains("self-request.html", response.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when navigating and show the url at the error message")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldFailWhenNavigatingAndShowTheUrlAtTheErrorMessage()
        {
            string url = HttpsServer.Prefix + "/redirect/1.html";
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(url));
            StringAssert.Contains(url, exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should be able to navigate to a page controlled by service worker")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToNavigateToAPageControlledByServiceWorker()
        {
            await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetch/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");
            await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetch/sw.html");
        }

        [PlaywrightTest("page-goto.spec.ts", "should send referer")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendReferer()
        {
            string referer1 = null;
            string referer2 = null;

            await TaskUtils.WhenAll(
                Server.WaitForRequest("/grid.html", r => referer1 = r.Headers["Referer"]),
                Server.WaitForRequest("/digits/1.png", r => referer2 = r.Headers["Referer"]),
                Page.GotoAsync(Server.Prefix + "/grid.html", new() { Referer = "http://google.com/" })
            );

            Assert.AreEqual("http://google.com/", referer1);
            // Make sure subresources do not inherit referer.
            Assert.AreEqual(Server.Prefix + "/grid.html", referer2);
            Assert.AreEqual(Server.Prefix + "/grid.html", Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should reject referer option when setExtraHTTPHeaders provides referer")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectRefererOptionWhenSetExtraHTTPHeadersProvidesReferer()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["referer"] = "http://microsoft.com/"
            });

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
                Page.GotoAsync(Server.Prefix + "/grid.html", new() { Referer = "http://google.com/" }));

            StringAssert.Contains("\"referer\" is already specified as extra HTTP header", exception.Message);
            StringAssert.Contains(Server.Prefix + "/grid.html", exception.Message);
        }

        [PlaywrightTest("page-goto.spec.ts", "should override referrer-policy")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
                Page.GotoAsync(Server.Prefix + "/grid.html", new() { Referer = "http://microsoft.com/" }));

            Assert.AreEqual("http://microsoft.com/", referer1);
            // Make sure subresources do not inherit referer.
            Assert.Null(referer2);
            Assert.AreEqual(Server.Prefix + "/grid.html", Page.Url);
        }

        [PlaywrightTest("page-goto.spec.ts", "should fail when canceled by another navigation")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenCanceledByAnotherNavigation()
        {
            Server.SetRoute("/one-style.html", _ => Task.Delay(10_000));
            var request = Server.WaitForRequest("/one-style.html");
            var failed = Page.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = TestConstants.IsFirefox ? WaitUntilState.NetworkIdle : WaitUntilState.Load });
            await request;
            await Page.GotoAsync(Server.EmptyPage);

            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => failed);
        }

        [PlaywrightTest("page-goto.spec.ts", "extraHTTPHeaders should be pushed to provisional page")]
        [Test, Ignore("Skipped in Playwright")]
        public void ExtraHTTPHeadersShouldBePushedToProvisionalPage()
        {
        }
    }
}
