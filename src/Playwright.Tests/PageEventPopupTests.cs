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
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync("() => window.open('about:blank')")
        );
        var popup = popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with window features")]
    public async Task ShouldWorkWithWindowFeatures()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync("() => window.open('about:blank', 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=780,height=200,top=0,left=0')")
        );
        var popup = popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
    public async Task ShouldEmitForImmediatelyClosedPopups()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync<string>(@"() => {
                    const win = window.open('about:blank');
                    win.close();
                }")
        );
        Assert.NotNull(popupTask.Result);
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should emit for immediately closed popups")]
    public async Task ShouldEmitForImmediatelyClosedPopupsWithLocation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popup = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await Page.EvaluateAsync<string>(@"() => {
                    const win = window.open(window.location.href);
                    win.close();
                }");
        });
        Assert.NotNull(popup);
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should be able to capture alert")]
    public void ShouldBeAbleToCaptureAlert()
    {
        // Too fancy.
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with empty url")]
    public async Task ShouldWorkWithEmptyUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync("() => window.open('')")
        );
        var popup = popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and no url")]
    public async Task ShouldWorkWithNoopenerAndNoUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync("() => window.open(undefined, null, 'noopener')")
        );
        var popup = popupTask.Result;
        Assert.AreEqual("about:blank", popup.Url.Split('#')[0]);
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and about:blank")]
    public async Task ShouldWorkWithNoopenerAndAboutBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync("() => window.open('about:blank', null, 'noopener')")
        );
        var popup = popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with noopener and url")]
    public async Task ShouldWorkWithNoopenerAndUrl()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvaluateAsync("url => window.open(url, null, 'noopener')", Server.EmptyPage)
        );
        var popup = popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithClickingTargetBlank()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=\"opener\" href=\"/one-style.html\">yo</a>");
        var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
        {
            var popup = task.Result;
            await popup.WaitForLoadStateAsync();
            return popup;
        });
        await TaskUtils.WhenAll(
            popupTask,
            Page.ClickAsync("a")
        );

        var popup = await popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.True(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with fake-clicking target=_blank and rel=noopener")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithFakeClickingTargetBlankAndRelNoopener()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
        {
            var popup = task.Result;
            await popup.WaitForLoadStateAsync();
            return popup;
        });
        await TaskUtils.WhenAll(
            popupTask,
            Page.EvalOnSelectorAsync("a", "a => a.click()")
        );
        var popup = await popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should work with clicking target=_blank and rel=noopener")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithClickingTargetBlankAndRelNoopener()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
        {
            var popup = task.Result;
            await popup.WaitForLoadStateAsync();
            return popup;
        });
        await TaskUtils.WhenAll(
            popupTask,
            Page.ClickAsync("a")
        );
        var popup = await popupTask.Result;
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.opener"));
        Assert.False(await popup.EvaluateAsync<bool>("() => !!window.opener"));
    }

    [PlaywrightTest("page-event-popup.spec.ts", "should not treat navigations as new popups")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldNotTreatNavigationsAsNewPopups()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var popupTask = Page.WaitForPopupAsync().ContinueWith(async task =>
        {
            var popup = task.Result;
            await popup.WaitForLoadStateAsync();
            return popup;
        });
        await TaskUtils.WhenAll(
            popupTask,
            Page.ClickAsync("a")
        );
        var popup = await popupTask.Result;
        bool badSecondPopup = false;
        Page.Popup += (_, _) => badSecondPopup = true;
        await popup.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.False(badSecondPopup);
    }
}
