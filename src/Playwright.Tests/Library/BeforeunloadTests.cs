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

/// <summary>Port of tests/library/beforeunload.spec.ts.</summary>
public class BeforeunloadTests : UpstreamBrowserTest
{
    [UpstreamTest("BU-001 should close browser with beforeunload page")]
    public async Task ShouldCloseBrowserWithBeforeunloadPage()
    {
        var browser = await BrowserType.LaunchAsync(LaunchOptions());
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");
        await browser.CloseAsync();
    }

    [UpstreamTest("BU-002 should close browsercontext with beforeunload page")]
    public async Task ShouldCloseBrowsercontextWithBeforeunloadPage()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");
        await context.CloseAsync();
    }

    [UpstreamTest("BU-003 should be able to navigate away from page with beforeunload")]
    public async Task ShouldBeAbleToNavigateAwayFromPageWithBeforeunload()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");
        await page.GotoAsync(Server.EmptyPage);
    }

    [UpstreamTest("BU-004 should close page with beforeunload listener")]
    public async Task ShouldClosePageWithBeforeunloadListener()
    {
        var context = await ContextFixture();
        var newPage = await context.NewPageAsync();
        await newPage.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await newPage.ClickAsync("body");
        await newPage.CloseAsync();
    }

    [UpstreamTest("BU-005 should run beforeunload if asked for @smoke")]
    public async Task ShouldRunBeforeunloadIfAskedFor()
    {
        var context = await ContextFixture();
        var newPage = await context.NewPageAsync();
        await newPage.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await newPage.ClickAsync("body");
        var dialog = await PromiseAll(
            Once<IDialog>(h => newPage.Dialog += h, h => newPage.Dialog -= h),
            newPage.CloseAsync(new() { RunBeforeUnload = true }));
        Assert.That(dialog.Type, Is.EqualTo("beforeunload"));
        Assert.That(dialog.DefaultValue, Is.EqualTo(""));
        if (BrowserName == "chromium")
        {
            Assert.That(dialog.Message, Is.EqualTo(""));
        }
        else if (BrowserName == "webkit")
        {
            Assert.That(dialog.Message, Is.EqualTo("Leave?"));
        }
        else
        {
            Assert.That(dialog.Message, Does.Contain("This page is asking you to confirm that you want to leave"));
        }
        await PromiseAll(
            dialog.AcceptAsync(),
            Once<IPage>(h => newPage.Close += h, h => newPage.Close -= h));
    }

    [UpstreamTest("BU-006 should access page after beforeunload")]
    public async Task ShouldAccessPageAfterBeforeunload()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");
        var dialog = await PromiseAll(
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h),
            page.CloseAsync(new() { RunBeforeUnload = true }));
        await dialog.DismissAsync();
        await page.EvaluateAsync("() => document.title");
    }

    [UpstreamTest("BU-007 should not stall on evaluate when dismissing beforeunload")]
    public async Task ShouldNotStallOnEvaluateWhenDismissingBeforeunload()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers fire.
        await page.ClickAsync("body");

        async Task Then()
        {
            var dialog = await Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h);
            await dialog.DismissAsync();
        }
        await PromiseAll(
            Then(),
            page.EvaluateAsync(@"() => {
      window.location.reload();
    }"));
    }

    [UpstreamTest("BU-008 should not stall on click when dismissing beforeunload")]
    public async Task ShouldNotStallOnClickWhenDismissingBeforeunload()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/frames/one-frame.html\">click me</a>");

        await page.EvaluateAsync(@"() => {
    window.onbeforeunload = () => false;
  }");
        page.Dialog += async (_, dialog) =>
        {
            await dialog.DismissAsync();
        };

        await page.GetByRole(AriaRole.Link).ClickAsync(new() { NoWaitAfter = true });
        await page.EvaluateAsync(@"() => {
    window.onbeforeunload = null;
  }");

        // This line should not timeout.
        await page.GetByRole(AriaRole.Link).ClickAsync(new() { Timeout = 5000 });
        await Expect(page).ToHaveURLAsync(Server.Prefix + "/frames/one-frame.html");
    }

    [UpstreamTest("BU-009 should support dismissing the dialog multiple times")]
    [LocalFixme("harness: on the macOS bot the test times out, the second page.close({ runBeforeUnload: true }) shows no dialog (only in firefox on darwin)")]
    public async Task ShouldSupportDismissingTheDialogMultipleTimes()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");

        // We have to interact with a page so that 'beforeunload' handlers
        // fire (in Firefox).
        await page.ClickAsync("body");

        var dialog = await PromiseAll(
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h),
            page.CloseAsync(new() { RunBeforeUnload = true }));

        await dialog.DismissAsync();

        var dialog2 = await PromiseAll(
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h),
            page.CloseAsync(new() { RunBeforeUnload = true }));

        await dialog2.DismissAsync();
    }

    [UpstreamTest("BU-010 should support closing the page after a previous dismiss")]
    public async Task ShouldSupportClosingThePageAfterAPreviousDismiss()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");

        // We have to interact with a page so that 'beforeunload' handlers
        // fire (in Firefox).
        await page.ClickAsync("body");

        var dialog = await PromiseAll(
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h),
            page.CloseAsync(new() { RunBeforeUnload = true }));

        await dialog.DismissAsync();

        await page.CloseAsync();
        Assert.That(page.IsClosed, Is.EqualTo(true));
    }

    [UpstreamTest("BU-011 should support closing the page via a subsequent onbeforeunload dialog")]
    public async Task ShouldSupportClosingThePageViaASubsequentOnbeforeunloadDialog()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/beforeunload.html");

        // We have to interact with a page so that 'beforeunload' handlers
        // fire (in Firefox).
        await page.ClickAsync("body");

        var dialog = await PromiseAll(
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h),
            page.CloseAsync(new() { RunBeforeUnload = true }));

        await dialog.AcceptAsync();

        await Once<IPage>(h => page.Close += h, h => page.Close -= h);
        Assert.That(page.IsClosed, Is.EqualTo(true));
    }

    [UpstreamTest("BU-012 does not get stalled by beforeUnload")]
    public async Task DoesNotGetStalledByBeforeUnload()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.HelloWorld);

        await page.EvaluateAsync(@"() => {
    window.addEventListener('beforeunload', event => {
      event.preventDefault();
    });
  }");
        page.Dialog += (_, dialog) => dialog.DismissAsync();

        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");

        await page.RouteAsync("**/api", route => route.FulfillAsync(new() { Status = 200, Body = "ok" }));
        await page.EvaluateAsync("async () => fetch(new URL('/api', window.location.href))");

        await page.CloseAsync(new() { RunBeforeUnload = true });

        await page.EvaluateAsync("async () => fetch(new URL('/api', window.location.href))");
    }
}
