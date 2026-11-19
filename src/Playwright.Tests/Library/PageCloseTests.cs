/**
 * Copyright 2017 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/page-close.spec.ts.</summary>
public class PageCloseTests : UpstreamPageTest
{
    [UpstreamTest("PA-039 should close page with active dialog")]
    public async Task ShouldClosePageWithActiveDialog()
    {
        await Page.EvaluateAsync("\"trigger builtins.setTimeout\"");
        await Page.SetContentAsync("<button onclick=\"builtins.setTimeout(() => alert(1))\">alert</button>");
        IgnoreErrors(Page.ClickAsync("button"));
        await Once<IDialog>(h => Page.Dialog += h, h => Page.Dialog -= h);
        await Page.CloseAsync();
    }

    [UpstreamTest("PA-040 expect should not print timed out error message when page closes")]
    public async Task ExpectShouldNotPrintTimedOutErrorMessageWhenPageCloses()
    {
        await Page.SetContentAsync("<div id=node>Text content</div>");
        var error = await PromiseAll(
            Catch(() => Expect(Page.Locator("div")).ToHaveTextAsync("hey", new() { Timeout = 100000 })),
            Page.CloseAsync());
        // .NET: the message names the .NET call, upstream has "expect(locator).toHaveText(expected)".
        Assert.That(StripAnsi(error.Message), Does.Contain("Expect(locator).ToHaveTextAsync(expected)"));
        Assert.That(StripAnsi(error.Message), Does.Not.Contain("Timed out"));
    }

    [UpstreamTest("PA-041 addLocatorHandler should throw when page closes")]
    public async Task AddLocatorHandlerShouldThrowWhenPageCloses()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            await Page.CloseAsync(new() { Reason = "custom reason" });
        });

        await Page.Locator("#aside").HoverAsync();
        await Page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('mouseover', 1);
  }");
        var error = await Catch(() => Page.Locator("#target").ClickAsync());
        Assert.That(error.Message, Does.Contain("custom reason"));
    }

    [UpstreamTest("PA-042 should reject all promises when page is closed")]
    public async Task ShouldRejectAllPromisesWhenPageIsClosed()
    {
        Server.SetRoute("/hang", (req, res) => { });
        var requestPromise = Server.WaitForRequest("/hang");
        await Page.SetContentAsync($"<iframe src=\"{Server.Prefix}/hang\"></iframe>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await requestPromise;
        var frameErrorPromise = Catch(() => Page.Frames[1].EvaluateAsync("() => new Promise(() => {})"));
        Exception error = null;
        error = await PromiseAll(
            Catch(() => Page.EvaluateAsync("() => new Promise(r => {})")),
            Page.CloseAsync());
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
        Assert.That((await frameErrorPromise).Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("PA-043 should set the page close state")]
    public async Task ShouldSetThePageCloseState()
    {
        Assert.That(Page.IsClosed, Is.EqualTo(false));
        await Page.CloseAsync();
        Assert.That(Page.IsClosed, Is.EqualTo(true));
    }

    [UpstreamTest("PA-044 should pass page to close event")]
    public async Task ShouldPassPageToCloseEvent()
    {
        var closedPage = await PromiseAll(
            Once<IPage>(h => Page.Close += h, h => Page.Close -= h),
            Page.CloseAsync());
        Assert.That(closedPage, Is.SameAs(Page));
    }

    [UpstreamTest("PA-045 should terminate network waiters")]
    public async Task ShouldTerminateNetworkWaiters()
    {
        // .NET: the results of PromiseAll are a tuple.
        var (first, second) = await PromiseAll(
            Catch(() => Page.WaitForRequestAsync(Server.EmptyPage)),
            Catch(() => Page.WaitForResponseAsync(Server.EmptyPage)),
            Page.CloseAsync());
        var results = new[] { first, second };
        for (var i = 0; i < 2; i++)
        {
            var message = results[i].Message;
            Assert.That(message, Does.Contain(TargetClosedErrorMessage));
            Assert.That(message, Does.Not.Contain("Timeout"));
        }
    }

    [UpstreamTest("PA-046 should be callable twice")]
    public async Task ShouldBeCallableTwice()
    {
        await PromiseAll(
            Page.CloseAsync(),
            Page.CloseAsync());
        await Page.CloseAsync();
    }

    [UpstreamTest("PA-047 should return null if parent page has been closed")]
    public async Task ShouldReturnNullIfParentPageHasBeenClosed()
    {
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('about:blank')"));
        await Page.CloseAsync();
        var opener = await popup.OpenerAsync();
        Assert.That(opener, Is.Null);
    }

    [UpstreamTest("PA-048 should fail with error upon disconnect")]
    public async Task ShouldFailWithErrorUponDisconnect()
    {
        Exception error = null;
        var waitForPromise = Catch(() => Page.WaitForDownloadAsync());
        await Page.CloseAsync();
        error = await waitForPromise;
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("PA-049 page.close should work with window.close")]
    public async Task PageCloseShouldWorkWithWindowClose()
    {
        var closedPromise = Once<IPage>(h => Page.Close += h, h => Page.Close -= h);
        await Page.CloseAsync();
        await closedPromise;
    }

    [UpstreamTest("PA-050 should not throw UnhandledPromiseRejection when page closes")]
    public async Task ShouldNotThrowUnhandledPromiseRejectionWhenPageCloses()
    {
        Info.Fixme(BrowserName == "firefox" && IsWindows, "makes the next test to always timeout");

        await Catch(() => PromiseAll(
            Page.CloseAsync(),
            Page.Mouse.ClickAsync(1, 2)));
    }

    [UpstreamTest("PA-051 interrupt request.response() and request.allHeaders() on page.close")]
    public async Task InterruptRequestResponseAndRequestAllHeadersOnPageClose()
    {
        Server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
        });
        var reqPromise = Page.WaitForRequestAsync("**/one-style.css");
        await Page.GotoAsync(Server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var req = await reqPromise;
        var respPromise = Catch(() => req.ResponseAsync());
        // .NET: the promise gives the headers or the error: the error is caught where the promise is awaited.
        var headersPromise = req.AllHeadersAsync();
        await Page.CloseAsync();
        Assert.That((await respPromise).Message, Does.Contain(TargetClosedErrorMessage));
        // All headers are the same as "provisional" headers in Firefox.
        if (BrowserName == "firefox")
        {
            Assert.That((await headersPromise)["user-agent"], Is.Not.Empty);
        }
        else
        {
            Assert.That((await Catch(() => headersPromise)).Message, Does.Contain(TargetClosedErrorMessage));
        }
    }

    [UpstreamTest("PA-052 should not treat navigations as new popups")]
    public async Task ShouldNotTreatNavigationsAsNewPopups()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("a"));
        var badSecondPopup = false;
        Page.Popup += (_, _) => badSecondPopup = true;
        await popup.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        await Page.CloseAsync();
        Assert.That(badSecondPopup, Is.EqualTo(false));
    }

    [UpstreamTest("PA-053 should not result in unhandled rejection")]
    public async Task ShouldNotResultInUnhandledRejection()
    {
        var closedPromise = Once<IPage>(h => Page.Close += h, h => Page.Close -= h);
        await Page.ExposeFunctionAsync("foo", async () =>
        {
            await Page.CloseAsync();
        });
        await Page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => window.foo(), 0);
    return undefined;
  }");
        await closedPromise;
        // Make a round-trip to be sure we did not throw immediately after closing.
        Assert.That(await Catch(() => Page.EvaluateAsync("1 + 1")), Is.InstanceOf<Exception>());
    }

    [UpstreamTest("PA-054 should reject response.finished if page closes")]
    public async Task ShouldRejectResponseFinishedIfPageCloses()
    {
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute("/get", (req, res) =>
        {
            // In Firefox, |fetch| will be hanging until it receives |Content-Type| header
            // from server.
            res.SetHeader("Content-Type", "text/plain; charset=utf-8");
            res.Write("hello ");
        });
        // send request and wait for server response
        var (pageResponse, _) = await PromiseAll(
            Page.WaitForResponseAsync(_ => true),
            Page.EvaluateAsync("() => fetch('./get', { method: 'GET' })"));

        var finishPromise = Catch(() => pageResponse.FinishedAsync());
        await Page.CloseAsync();
        var error = await finishPromise;
        Assert.That(error.Message, Does.Contain("closed"));
    }

    [UpstreamTest("PA-055 should not throw when continuing while page is closing")]
    public async Task ShouldNotThrowWhenContinuingWhilePageIsClosing()
    {
        // .NET: let done; is a completed task: await undefined does nothing.
        var done = Task.CompletedTask;
        await Page.RouteAsync("**/*", route =>
        {
            // .NET: void route.continue() is the first item of the Promise.all() of upstream.
            IgnoreErrors(route.ContinueAsync());
            done = PromiseAll(
                Task.CompletedTask,
                Page.CloseAsync());
        });
        await Catch(() => Page.GotoAsync(Server.EmptyPage));
        await done;
    }

    [UpstreamTest("PA-056 should not throw when continuing after page is closed")]
    public async Task ShouldNotThrowWhenContinuingAfterPageIsClosed()
    {
        // .NET: let done; is a completed task: await undefined does nothing.
        var done = Task.CompletedTask;
        await Page.RouteAsync("**/*", async route =>
        {
            await Page.CloseAsync();
            done = route.ContinueAsync();
        });
        var error = await Catch(() => Page.GotoAsync(Server.EmptyPage));
        await done;
        Assert.That(error, Is.InstanceOf<Exception>());
    }
}
