/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-event-popup.spec.ts.</summary>
public class PageEventPopupTests
{
    [UpstreamTest("PO-001 should work @smoke")]
    public async Task ShouldWork(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window['__popup'] = window.open('about:blank')"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(true));
    }

    [UpstreamTest("PO-002 should work with window features")]
    public async Task ShouldWorkWithWindowFeatures(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window['__popup'] = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0')"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(true));
    }

    [UpstreamTest("PO-003 should emit for immediately closed popups")]
    public async Task ShouldEmitForImmediatelyClosedPopups(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync(@"() => {
      const win = window.open('about:blank');
      win.close();
    }"));
        Assert.That(popup, Is.Not.Null);
    }

    [UpstreamTest("PO-004 should emit for immediately closed popups 2")]
    public async Task ShouldEmitForImmediatelyClosedPopups2(IPage page, Server server, string browserName, string video)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync(@"() => {
      const win = window.open(window.location.href);
      win.close();
    }"));
        Assert.That(popup, Is.Not.Null);
    }

    [UpstreamTest("PO-005 should be able to capture alert")]
    public async Task ShouldBeAbleToCaptureAlert(IPage page)
    {
        var evaluatePromise = page.EvaluateAsync(@"() => {
    const win = window.open('');
    win.alert('hello');
  }");
        var (popup, dialog) = await PromiseAll(
            page.WaitForPopupAsync(),
            Once<IDialog>(h => page.Context.Dialog += h, h => page.Context.Dialog -= h));
        Assert.That(dialog.Message, Is.EqualTo("hello"));
        Assert.That(dialog.Page, Is.SameAs(popup));
        await dialog.DismissAsync();
        await evaluatePromise;
    }

    [UpstreamTest("PO-006 should work with empty url")]
    public async Task ShouldWorkWithEmptyUrl(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window['__popup'] = window.open('')"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(true));
    }

    [UpstreamTest("PO-007 should work with noopener and no url")]
    public async Task ShouldWorkWithNoopenerAndNoUrl(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window['__popup'] = window.open(undefined, null, 'noopener')"));
        // Chromium reports `about:blank#blocked` here.
        Assert.That(popup.Url.Split('#')[0], Is.EqualTo("about:blank"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
    }

    [UpstreamTest("PO-008 should work with noopener and about:blank")]
    public async Task ShouldWorkWithNoopenerAndAboutBlank(IPage page)
    {
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window['__popup'] = window.open('about:blank', null, 'noopener')"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
    }

    [UpstreamTest("PO-009 should work with noopener and url")]
    public async Task ShouldWorkWithNoopenerAndUrl(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window['__popup'] = window.open(url, null, 'noopener')", server.EmptyPage));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
    }

    [UpstreamTest("PO-010 should work with clicking target=_blank")]
    public async Task ShouldWorkWithClickingTargetBlank(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            page.WaitForPopupAsync(),
            page.ClickAsync("a"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(true));
        Assert.That(popup.MainFrame.Page, Is.SameAs(popup));
    }

    [UpstreamTest("PO-011 should work with fake-clicking target=_blank and rel=noopener")]
    public async Task ShouldWorkWithFakeClickingTargetBlankAndRelNoopener(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvalOnSelectorAsync("a", "a => a.click()"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
    }

    [UpstreamTest("PO-012 should work with clicking target=_blank and rel=noopener")]
    public async Task ShouldWorkWithClickingTargetBlankAndRelNoopener(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popup = await PromiseAll(
            page.WaitForPopupAsync(),
            page.ClickAsync("a"));
        Assert.That(await page.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
        Assert.That(await popup.EvaluateAsync<bool>("() => !!window.opener"), Is.EqualTo(false));
    }

    [UpstreamTest("PO-013 should report popup opened from iframes")]
    public async Task ShouldReportPopupOpenedFromIframes(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.Prefix + "/frames/two-frames.html");
        var frame = page.Frames[1];
        Assert.That(frame, Is.Not.Null);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            frame.EvaluateAsync("() => window.open('')"));
        Assert.That(popup, Is.Not.Null);
    }

    [UpstreamTest("PO-030 should finish loading after opening a popup while a cross-site iframe is loading")]
    public async Task ShouldFinishLoadingAfterOpeningAPopupWhileACrossSiteIframeIsLoading(IPage page, Server server, bool isHeadlessShell, bool isElectron, bool isAndroid)
    {
        Info.Skip(isElectron || isAndroid, "No defined site-isolation policy");
        Info.Fixme(isHeadlessShell, "Without site isolation, which is the default in headless shell, the opener never fires the load event");

        var frameMayFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/frame.html", async (req, res) =>
        {
            await frameMayFinish.Task;
            res.SetHeader("Content-Type", "text/html");
            res.End("<body>frame</body>");
        });
        server.SetRoute("/opener.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($@"
      <iframe src=""{server.CrossProcessPrefix}/frame.html""></iframe>
      <script>window.open('{server.EmptyPage}')</script>
    ");
        });

        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.GotoAsync(server.Prefix + "/opener.html", new() { WaitUntil = WaitUntilState.Commit }));
        // The iframe is still loading when the popup opens.
        frameMayFinish.SetResult();
        await popup.WaitForLoadStateAsync(LoadState.Load);
        await page.WaitForLoadStateAsync(LoadState.Load);
        Assert.That(await page.EvaluateAsync<string>("() => document.readyState"), Is.EqualTo("complete"));
    }
}
