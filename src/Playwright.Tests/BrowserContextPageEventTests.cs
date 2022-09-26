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

namespace Microsoft.Playwright.Tests;

public class BrowserContextPageEventTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-page-event.spec.ts", "should have url")]
    public async Task ShouldHaveUrl()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var (otherPage, _) = await TaskUtils.WhenAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

        Assert.AreEqual(Server.EmptyPage, otherPage.Url);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should have url after domcontentloaded")]
    public async Task ShouldHaveUrlAfterDomcontentloaded()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var (otherPage, _) = await TaskUtils.WhenAll(
            context.WaitForPageAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

        await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual(Server.EmptyPage, otherPage.Url);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should have about:blank url with domcontentloaded")]
    public async Task ShouldHaveAboutBlankUrlWithDomcontentloaded()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var otherPage = await context.RunAndWaitForPageAsync(async () =>
        {
            await page.EvaluateAsync("url => window.open(url)", "about:blank");
        });
        await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual("about:blank", otherPage.Url);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should have about:blank for empty url with domcontentloaded")]
    public async Task ShouldHaveAboutBlankUrlForEmptyUrlWithDomcontentloaded()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var otherPage = await context.RunAndWaitForPageAsync(async () =>
        {
            await page.EvaluateAsync("() => window.open()");
        });
        await otherPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual("about:blank", otherPage.Url);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should report when a new page is created and closed")]
    public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var otherPage = await context.RunAndWaitForPageAsync(async () =>
        {
            await page.EvaluateAsync("url => window.open(url)", Server.CrossProcessPrefix + "/empty.html");
        });

        StringAssert.Contains(Server.CrossProcessPrefix, otherPage.Url);
        Assert.AreEqual("Hello world", await otherPage.EvaluateAsync<string>("() => ['Hello', 'world'].join(' ')"));
        Assert.NotNull(await otherPage.QuerySelectorAsync("body"));


        var allPages = context.Pages;
        CollectionAssert.Contains(allPages, page);
        CollectionAssert.Contains(allPages, otherPage);

        var closeEventReceived = new TaskCompletionSource<bool>();
        otherPage.Close += (_, _) => closeEventReceived.TrySetResult(true);

        await otherPage.CloseAsync();
        await closeEventReceived.Task;

        allPages = context.Pages;
        CollectionAssert.Contains(allPages, page);
        CollectionAssert.DoesNotContain(allPages, otherPage);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should report initialized pages")]
    public async Task ShouldReportInitializedPages()
    {
        await using var context = await Browser.NewContextAsync();
        var pageTask = context.WaitForPageAsync();
        _ = context.NewPageAsync();
        var newPage = await pageTask;
        Assert.AreEqual("about:blank", newPage.Url);

        var popupTask = context.WaitForPageAsync();
        var evaluateTask = newPage.EvaluateAsync("() => window.open('about:blank')");
        var popup = await popupTask;
        Assert.AreEqual("about:blank", popup.Url);
        await evaluateTask;
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should not crash while redirecting of original request was missed")]
    public async Task ShouldNotCrashWhileRedirectingOfOriginalRequestWasMissed()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        Server.SetRoute("/one-style.css", context =>
        {
            context.Response.Redirect("/one-style.css");
            return Task.CompletedTask;
        });

        // Open a new page. Use window.open to connect to the page later.
        var pageCreatedTask = context.WaitForPageAsync();
        await TaskUtils.WhenAll(
            pageCreatedTask,
            page.EvaluateAsync("url => window.open(url)", Server.Prefix + "/one-style.html"),
            Server.WaitForRequest("/one-style.css"));

        var newPage = pageCreatedTask.Result;

        await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual(Server.Prefix + "/one-style.html", newPage.Url);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should have an opener")]
    public async Task ShouldHaveAnOpener()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        var (popupEvent, _) = await TaskUtils.WhenAll(
          context.WaitForPageAsync(),
          page.GotoAsync(Server.Prefix + "/popup/window-open.html"));

        var popup = popupEvent;
        Assert.AreEqual(Server.Prefix + "/popup/popup.html", popup.Url);
        Assert.AreEqual(page, await popup.OpenerAsync());
        Assert.Null(await page.OpenerAsync());
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should fire page lifecycle events")]
    public async Task ShouldFirePageLoadStates()
    {
        await using var context = await Browser.NewContextAsync();
        var events = new List<string>();

        context.Page += (_, e) =>
        {
            events.Add("CREATED: " + e.Url);
            e.Close += (sender, _) => events.Add("DESTROYED: " + ((IPage)sender).Url);
        };

        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.CloseAsync();
        Assert.AreEqual(
            new List<string>()
            {
                    "CREATED: about:blank",
                    $"DESTROYED: {Server.EmptyPage}"
            },
            events);
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should work with Shift-clicking")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithShiftClicking()
    {
        // WebKit: Shift+Click does not open a new window.
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

        var popupEventTask = context.WaitForPageAsync();
        await TaskUtils.WhenAll(
          popupEventTask,
          page.ClickAsync("a", new() { Modifiers = new[] { KeyboardModifier.Shift } }));

        Assert.Null(await popupEventTask.Result.OpenerAsync());
    }

    [PlaywrightTest("browsercontext-page-event.spec.ts", "should report when a new page is created and closed")]
    [Skip(SkipAttribute.Targets.Webkit, SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithCtrlClicking()
    {
        // Firefox: reports an opener in this case.
        // WebKit: Ctrl+Click does not open a new tab.
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a href=\"/one-style.html\">yo</a>");

        var popupEventTask = context.WaitForPageAsync();
        await TaskUtils.WhenAll(
          popupEventTask,
          page.ClickAsync("a", new() { Modifiers = new[] { TestConstants.IsMacOSX ? KeyboardModifier.Meta : KeyboardModifier.Control } }));

        Assert.Null(await popupEventTask.Result.OpenerAsync());
    }
}
