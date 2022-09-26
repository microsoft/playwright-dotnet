/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class PageGotoTests : PageTestEx
{
    [PlaywrightTest("page-goto.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(Server.EmptyPage, Page.Url);
    }

    [PlaywrightTest("page-goto.spec.ts", "should work with file URL")]
    public async Task ShouldWorkWithFileURL()
    {
        string fileurl = new Uri(TestUtils.GetAsset(Path.Combine("frames", "two-frames.html"))).AbsoluteUri;
        await Page.GotoAsync(fileurl);
        Assert.AreEqual(fileurl.ToLowerInvariant(), Page.Url.ToLowerInvariant());
        Assert.AreEqual(3, Page.Frames.Count);
    }

    [PlaywrightTest("page-goto.spec.ts", "should use http for no protocol")]
    public async Task ShouldUseHttpForNoProtocol()
    {
        await Page.GotoAsync(Server.EmptyPage.Replace("http://", string.Empty));
        Assert.AreEqual(Server.EmptyPage, Page.Url);
    }

    [PlaywrightTest("page-goto.spec.ts", "should work cross-process")]
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
    public async Task ShouldWorkWithRedirects()
    {
        Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        Server.SetRedirect("/redirect/2.html", "/empty.html");

        var response = await Page.GotoAsync(Server.Prefix + "/redirect/1.html");
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        await Page.GotoAsync(Server.EmptyPage);
    }

    [PlaywrightTest("page-goto.spec.ts", "should navigate to about:blank")]
    public async Task ShouldNavigateToAboutBlank()
    {
        var response = await Page.GotoAsync("about:blank");
        Assert.Null(response);
    }

    [PlaywrightTest("page-goto.spec.ts", "should return response when page changes its URL after load")]
    public async Task ShouldReturnResponseWhenPageChangesItsURLAfterLoad()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/historyapi.html");
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("page-goto.spec.ts", "should work with subframes return 204")]
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
    public async Task ShouldNavigateToEmptyPageWithDOMContentLoaded()
    {
        var response = await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("page-goto.spec.ts", "should work when page calls history API in beforeunload")]
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
    public async Task ShouldFailWhenNavigatingToBadSSL()
    {
        Page.Request += (_, e) => Assert.NotNull(e);
        Page.RequestFinished += (_, e) => Assert.NotNull(e);
        Page.RequestFailed += (_, e) => Assert.NotNull(e);

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(HttpsServer.Prefix + "/empty.html"));
        TestUtils.AssertSSLError(exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should fail when navigating to bad SSL after redirects")]
    public async Task ShouldFailWhenNavigatingToBadSSLAfterRedirects()
    {
        Server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        Server.SetRedirect("/redirect/2.html", "/empty.html");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(HttpsServer.Prefix + "/redirect/1.html"));
        TestUtils.AssertSSLError(exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should not crash when navigating to bad SSL after a cross origin navigation")]
    public async Task ShouldNotCrashWhenNavigatingToBadSSLAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(HttpsServer.Prefix + "/empty.html"));
        TestUtils.AssertSSLError(exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle0 is passed as an option")]
    [Ignore("We don't need this test")]
    public void ShouldThrowIfNetworkIdle0IsPassedAsAnOption()
    { }

    [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle2 is passed as an option")]
    [Ignore("We don't need this test")]
    public void ShouldThrowIfNetworkIdle2IsPassedAsAnOption()
    { }

    [PlaywrightTest("page-goto.spec.ts", "should throw if networkidle is passed as an option")]
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
    public async Task ShouldFailWhenExceedingMaximumNavigationTimeout()
    {
        Server.SetRoute("/empty.html", _ => Task.Delay(-1));
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.GotoAsync(Server.EmptyPage, new() { Timeout = 1 }));
        StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
        StringAssert.Contains(Server.EmptyPage, exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding maximum navigation timeout")]
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
    public async Task ShouldFailWhenExceedingBrowserContextNavigationTimeout()
    {
        Server.SetRoute("/empty.html", _ => Task.Delay(-1));
        Page.Context.SetDefaultNavigationTimeout(2);
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
        StringAssert.Contains("Timeout 2ms exceeded", exception.Message);
        StringAssert.Contains(Server.EmptyPage, exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should fail when exceeding default maximum timeout")]
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
    public async Task ShouldFailWhenExceedingBrowserContextTimeout()
    {
        Server.SetRoute("/empty.html", _ => Task.Delay(-1));
        Page.Context.SetDefaultTimeout(2);
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.GotoAsync(Server.EmptyPage));
        StringAssert.Contains("Timeout 2ms exceeded", exception.Message);
        StringAssert.Contains(Server.EmptyPage, exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should prioritize default navigation timeout over default timeout")]
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
            StringAssert.Contains("Navigation interrupted by another one", exception.Message);
        }
        else
        {
            StringAssert.Contains("NS_BINDING_ABORTED", exception.Message);
        }
    }

    [PlaywrightTest("page-goto.spec.ts", "should work when navigating to valid url")]
    public async Task ShouldWorkWhenNavigatingToValidUrl()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("page-goto.spec.ts", "should work when navigating to data url")]
    public async Task ShouldWorkWhenNavigatingToDataUrl()
    {
        var response = await Page.GotoAsync("data:text/html,hello");
        Assert.Null(response);
    }

    [PlaywrightTest("page-goto.spec.ts", "should work when navigating to 404")]
    public async Task ShouldWorkWhenNavigatingTo404()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/not-found");
        Assert.AreEqual((int)HttpStatusCode.NotFound, response.Status);
    }

    [PlaywrightTest("page-goto.spec.ts", "should return last response in redirect chain")]
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
    [Ignore("We don't need this test")]
    public void ShouldNotLeakListenersDuringNavigation()
    { }

    [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during bad navigation")]
    [Ignore("We don't need this test")]
    public void ShouldNotLeakListenersDuringBadNavigation()
    { }

    [PlaywrightTest("page-goto.spec.ts", "should not leak listeners during navigation of 11 pages")]
    [Ignore("We don't need this test")]
    public void ShouldNotLeakListenersDuringNavigationOf11Pages()
    { }

    [PlaywrightTest("page-goto.spec.ts", "should navigate to dataURL and not fire dataURL requests")]
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
    public async Task ShouldWorkWithSelfRequestingPage()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/self-request.html");
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        StringAssert.Contains("self-request.html", response.Url);
    }

    [PlaywrightTest("page-goto.spec.ts", "should fail when navigating and show the url at the error message")]
    public async Task ShouldFailWhenNavigatingAndShowTheUrlAtTheErrorMessage()
    {
        string url = HttpsServer.Prefix + "/redirect/1.html";
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.GotoAsync(url));
        StringAssert.Contains(url, exception.Message);
    }

    [PlaywrightTest("page-goto.spec.ts", "should be able to navigate to a page controlled by service worker")]
    public async Task ShouldBeAbleToNavigateToAPageControlledByServiceWorker()
    {
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetch/sw.html");
        await Page.EvaluateAsync("() => window.activationPromise");
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetch/sw.html");
    }

    [PlaywrightTest("page-goto.spec.ts", "should send referer")]
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
    public async Task ShouldOverrideReferrerPolicy()
    {
        Server.SetRoute("/grid.html", async context =>
        {
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await Server.ServeFile(context);
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
    public async Task ShouldFailWhenCanceledByAnotherNavigation()
    {
        Server.SetRoute("/one-style.html", _ => Task.Delay(10_000));
        var request = Server.WaitForRequest("/one-style.html");
        var failed = Page.GotoAsync(Server.Prefix + "/one-style.html");
        await request;
        await Page.GotoAsync(Server.EmptyPage);

        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => failed);
    }

    [PlaywrightTest("page-goto.spec.ts", "should return when navigation is committed if commit is specified'")]
    public async Task ShouldReturnWhenNavigationIsComittedIfCommitIsSpecified()
    {
        Server.SetRoute("/empty.html", async context =>
        {
            context.Response.StatusCode = 200;
            context.Response.Headers.Add("content-type", "text/html");
            context.Response.Headers.Add("content-length", (8192 + 7).ToString(CultureInfo.InvariantCulture));
            // Write enought bytes of the body to trigge response received event.
            var str = "<title>" + new string('a', 8192);
            await context.Response.WriteAsync(str);
            await context.Response.Body.FlushAsync();
        });

        var response = await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.Commit });
        Assert.AreEqual(200, response.Status);
    }

}
