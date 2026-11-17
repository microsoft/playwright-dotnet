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
public class PageCloseTests
{
    [UpstreamTest("PA-039 should close page with active dialog")]
    public async Task ShouldClosePageWithActiveDialog(IPage page)
    {
        await page.EvaluateAsync("\"trigger builtins.setTimeout\"");
        await page.SetContentAsync("<button onclick=\"builtins.setTimeout(() => alert(1))\">alert</button>");
        IgnoreErrors(page.ClickAsync("button"));
        await Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h);
        await page.CloseAsync();
    }

    [UpstreamTest("PA-040 expect should not print timed out error message when page closes")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: Target page, context or browser has been closed 'hey'\\nBut was: 'null' \\nCall log: ...\", upstream expects \"expect(locator).toHaveText(expected)\"")]
    public async Task ExpectShouldNotPrintTimedOutErrorMessageWhenPageCloses(IPage page)
    {
        await page.SetContentAsync("<div id=node>Text content</div>");
        var error = await PromiseAll(
            Catch(() => Expect(page.Locator("div")).ToHaveTextAsync("hey", new() { Timeout = 100000 })),
            page.CloseAsync());
        Assert.That(StripAnsi(error.Message), Does.Contain("expect(locator).toHaveText(expected)"));
        Assert.That(StripAnsi(error.Message), Does.Not.Contain("Timed out"));
    }

    [UpstreamTest("PA-041 addLocatorHandler should throw when page closes")]
    public async Task AddLocatorHandlerShouldThrowWhenPageCloses(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            await page.CloseAsync(new() { Reason = "custom reason" });
        });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('mouseover', 1);
  }");
        var error = await Catch(() => page.Locator("#target").ClickAsync());
        Assert.That(error.Message, Does.Contain("custom reason"));
    }

    [UpstreamTest("PA-042 should reject all promises when page is closed")]
    public async Task ShouldRejectAllPromisesWhenPageIsClosed(IPage page, Server server)
    {
        server.SetRoute("/hang", (req, res) => { });
        var requestPromise = server.WaitForRequest("/hang");
        await page.SetContentAsync($"<iframe src=\"{server.Prefix}/hang\"></iframe>", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await requestPromise;
        var frameErrorPromise = Catch(() => page.Frames[1].EvaluateAsync("() => new Promise(() => {})"));
        Exception error = null;
        error = await PromiseAll(
            Catch(() => page.EvaluateAsync("() => new Promise(r => {})")),
            page.CloseAsync());
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
        Assert.That((await frameErrorPromise).Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("PA-043 should set the page close state")]
    public async Task ShouldSetThePageCloseState(IPage page)
    {
        Assert.That(page.IsClosed, Is.EqualTo(false));
        await page.CloseAsync();
        Assert.That(page.IsClosed, Is.EqualTo(true));
    }

    [UpstreamTest("PA-044 should pass page to close event")]
    public async Task ShouldPassPageToCloseEvent(IPage page)
    {
        var closedPage = await PromiseAll(
            Once<IPage>(h => page.Close += h, h => page.Close -= h),
            page.CloseAsync());
        Assert.That(closedPage, Is.SameAs(page));
    }

    [UpstreamTest("PA-045 should terminate network waiters")]
    public async Task ShouldTerminateNetworkWaiters(IPage page, Server server)
    {
        // .NET: the results of PromiseAll are a tuple.
        var (first, second) = await PromiseAll(
            Catch(() => page.WaitForRequestAsync(server.EmptyPage)),
            Catch(() => page.WaitForResponseAsync(server.EmptyPage)),
            page.CloseAsync());
        var results = new[] { first, second };
        for (var i = 0; i < 2; i++)
        {
            var message = results[i].Message;
            Assert.That(message, Does.Contain(TargetClosedErrorMessage));
            Assert.That(message, Does.Not.Contain("Timeout"));
        }
    }

    [UpstreamTest("PA-046 should be callable twice")]
    public async Task ShouldBeCallableTwice(IPage page)
    {
        await PromiseAll(
            page.CloseAsync(),
            page.CloseAsync());
        await page.CloseAsync();
    }

    [UpstreamTest("PA-047 should return null if parent page has been closed")]
    public async Task ShouldReturnNullIfParentPageHasBeenClosed(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open('about:blank')"));
        await page.CloseAsync();
        var opener = await popup.OpenerAsync();
        Assert.That(opener, Is.Null);
    }

    [UpstreamTest("PA-048 should fail with error upon disconnect")]
    public async Task ShouldFailWithErrorUponDisconnect(IPage page)
    {
        Exception error = null;
        var waitForPromise = Catch(() => page.WaitForDownloadAsync());
        await page.CloseAsync();
        error = await waitForPromise;
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("PA-049 page.close should work with window.close")]
    public async Task PageCloseShouldWorkWithWindowClose(IPage page)
    {
        var closedPromise = Once<IPage>(h => page.Close += h, h => page.Close -= h);
        await page.CloseAsync();
        await closedPromise;
    }

    [UpstreamTest("PA-050 should not throw UnhandledPromiseRejection when page closes")]
    public async Task ShouldNotThrowUnhandledPromiseRejectionWhenPageCloses(IPage page, string browserName, bool isWindows)
    {
        Info.Fixme(browserName == "firefox" && isWindows, "makes the next test to always timeout");

        await Catch(() => PromiseAll(
            page.CloseAsync(),
            page.Mouse.ClickAsync(1, 2)));
    }

    [UpstreamTest("PA-051 interrupt request.response() and request.allHeaders() on page.close")]
    public async Task InterruptRequestResponseAndRequestAllHeadersOnPageClose(IPage page, Server server, string browserName, string channel)
    {
        server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
        });
        var reqPromise = page.WaitForRequestAsync("**/one-style.css");
        await page.GotoAsync(server.Prefix + "/one-style.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var req = await reqPromise;
        var respPromise = Catch(() => req.ResponseAsync());
        // .NET: the promise gives the headers or the error: the error is caught where the promise is awaited.
        var headersPromise = req.AllHeadersAsync();
        await page.CloseAsync();
        Assert.That((await respPromise).Message, Does.Contain(TargetClosedErrorMessage));
        // All headers are the same as "provisional" headers in Firefox.
        if (browserName == "firefox")
        {
            Assert.That((await headersPromise)["user-agent"], Is.Not.Empty);
        }
        else
        {
            Assert.That((await Catch(() => headersPromise)).Message, Does.Contain(TargetClosedErrorMessage));
        }
    }

    [UpstreamTest("PA-052 should not treat navigations as new popups")]
    public async Task ShouldNotTreatNavigationsAsNewPopups(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            page.WaitForPopupAsync(),
            page.ClickAsync("a"));
        var badSecondPopup = false;
        page.Popup += (_, _) => badSecondPopup = true;
        await popup.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        await page.CloseAsync();
        Assert.That(badSecondPopup, Is.EqualTo(false));
    }

    [UpstreamTest("PA-053 should not result in unhandled rejection")]
    public async Task ShouldNotResultInUnhandledRejection(IPage page)
    {
        var closedPromise = Once<IPage>(h => page.Close += h, h => page.Close -= h);
        await page.ExposeFunctionAsync("foo", async () =>
        {
            await page.CloseAsync();
        });
        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => window.foo(), 0);
    return undefined;
  }");
        await closedPromise;
        // Make a round-trip to be sure we did not throw immediately after closing.
        Assert.That(await Catch(() => page.EvaluateAsync("1 + 1")), Is.InstanceOf<Exception>());
    }

    [UpstreamTest("PA-054 should reject response.finished if page closes")]
    public async Task ShouldRejectResponseFinishedIfPageCloses(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/get", (req, res) =>
        {
            // In Firefox, |fetch| will be hanging until it receives |Content-Type| header
            // from server.
            res.SetHeader("Content-Type", "text/plain; charset=utf-8");
            res.Write("hello ");
        });
        // send request and wait for server response
        var (pageResponse, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync("() => fetch('./get', { method: 'GET' })"));

        var finishPromise = Catch(() => pageResponse.FinishedAsync());
        await page.CloseAsync();
        var error = await finishPromise;
        Assert.That(error.Message, Does.Contain("closed"));
    }

    [UpstreamTest("PA-055 should not throw when continuing while page is closing")]
    public async Task ShouldNotThrowWhenContinuingWhilePageIsClosing(IPage page, Server server)
    {
        // .NET: let done; is a completed task: await undefined does nothing.
        var done = Task.CompletedTask;
        await page.RouteAsync("**/*", route =>
        {
            // .NET: void route.continue() is the first item of the Promise.all() of upstream.
            IgnoreErrors(route.ContinueAsync());
            done = PromiseAll(
                Task.CompletedTask,
                page.CloseAsync());
        });
        await Catch(() => page.GotoAsync(server.EmptyPage));
        await done;
    }

    [UpstreamTest("PA-056 should not throw when continuing after page is closed")]
    public async Task ShouldNotThrowWhenContinuingAfterPageIsClosed(IPage page, Server server)
    {
        // .NET: let done; is a completed task: await undefined does nothing.
        var done = Task.CompletedTask;
        await page.RouteAsync("**/*", async route =>
        {
            await page.CloseAsync();
            done = route.ContinueAsync();
        });
        var error = await Catch(() => page.GotoAsync(server.EmptyPage));
        await done;
        Assert.That(error, Is.InstanceOf<Exception>());
    }
}
