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

public class PageEventPopupTests : PageTestEx
{
    [PlaywrightTest("page-event-popup.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('about:blank')")
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with window features")]
    public async Task ShouldWorkWithWindowFeatures()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0')")
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
    public async Task ShouldEmitForImmediatelyClosedPopups()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync<string>(@"() => {
                const win = window.open('about:blank');
                win.close();
            }")
        );
        Assert.NotNull(popup);
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
    public async Task ShouldEmitForImmediatelyClosedPopupsWithLocation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync<string>(@"() => {
                const win = window.open(window.location.href);
                win.close();
            }")
        );
        Assert.NotNull(popup);
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should be able to capture alert")]
    public async Task ShouldBeAbleToCaptureAlert()
    {
        var evaluateTask = Page.EvaluateAsync<string>(@"() => {
            const win = window.open('');
            win.alert('hello');
        }");
        var dialogTask = new TaskCompletionSource<IDialog>();
        Page.Context.Dialog += (_, dialog) => dialogTask.SetResult(dialog);
        var (popup, dialog) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            dialogTask.Task
        );
        Assert.AreEqual("hello", dialog.Message);
        Assert.AreEqual(popup, dialog.Page);
        await dialog.DismissAsync();
        await evaluateTask;
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with empty url")]
    public async Task ShouldWorkWithEmptyUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('')")
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and no url")]
    public async Task ShouldWorkWithNoopenerAndNoUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open(undefined, null, 'noopener')")
        );
        // Chromium reports `about:blank#blocked` here.
        Assert.AreEqual("about:blank", popup.Url.Split('#')[0]);
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and about:blank")]
    public async Task ShouldWorkWithNoopenerAndAboutBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('about:blank', null, 'noopener')")
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and url")]
    public async Task ShouldWorkWithNoopenerAndUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("url => window.open(url, null, 'noopener')", Server.EmptyPage)
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank")]
    public async Task ShouldWorkWithClickingTargetBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("a")
        );

        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.AreEqual(popup.MainFrame.Page, popup);
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with fake-clicking target=_blank and rel=noopener")]
    public async Task ShouldWorkWithFakeClickingTargetBlankAndRelNoopener()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvalOnSelectorAsync("a", "a => a.click()")
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank and rel=noopener")]
    public async Task ShouldWorkWithClickingTargetBlankAndRelNoopener()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("a")
        );
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should not treat navigations as new popups")]
    public async Task ShouldNotTreatNavigationsAsNewPopups()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.ClickAsync("a")
        );
        bool badSecondPopup = false;
        Page.Popup += (_, _) => badSecondPopup = true;
        await popup.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        await Page.CloseAsync();
        Assert.False(badSecondPopup);
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should report popup opened from iframes")]
    public async Task ShouldReportPopupOpenedFromIframes()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/two-frames.html");
        var frame = Page.Frames.First(f => f.Name == "uno");
        var popup = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            frame.EvaluateAsync("() => window.open('')")
        );
        Assert.NotNull(popup);
    }
}
