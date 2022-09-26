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

public class PageEventCrashTests : PageTestEx
{
    // We skip all browser because crash uses internals.
    [PlaywrightTest("page-event-crash.spec.ts", "should emit crash event when page crashes")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldEmitCrashEventWhenPageCrashes()
    {
        await Page.SetContentAsync("<div>This page should crash</div>");
        var crashEvent = new TaskCompletionSource<bool>();
        Page.Crash += (_, _) => crashEvent.TrySetResult(true);

        await CrashAsync(Page);
        await crashEvent.Task;
    }

    // We skip all browser because crash uses internals.
    [PlaywrightTest("page-event-crash.spec.ts", "should throw on any action after page crashes")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldThrowOnAnyActionAfterPageCrashes()
    {
        await Page.SetContentAsync("<div>This page should crash</div>");
        var crashEvent = new TaskCompletionSource<bool>();
        Page.Crash += (_, _) => crashEvent.TrySetResult(true);

        await CrashAsync(Page);
        await crashEvent.Task;
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvaluateAsync("() => {}"));
        StringAssert.Contains("Target crashed", exception.Message);
    }

    // We skip all browser because crash uses internals.
    [PlaywrightTest("page-event-crash.spec.ts", "should cancel waitForEvent when page crashes")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldCancelWaitForEventWhenPageCrashes()
    {
        await Page.SetContentAsync("<div>This page should crash</div>");
        var responseTask = Page.WaitForResponseAsync("**/*");
        await CrashAsync(Page);
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => responseTask);
        StringAssert.Contains("Page crashed", exception.Message);
    }

    // We skip all browser because crash uses internals.
    [PlaywrightTest("page-event-crash.spec.ts", "should cancel navigation when page crashes")]
    [Ignore("Not relevant downstream")]
    public void ShouldCancelNavigationWhenPageCrashes()
    {
    }

    // We skip all browser because crash uses internals.
    [PlaywrightTest("page-event-crash.spec.ts", "should be able to close context when page crashes")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldBeAbleToCloseContextWhenPageCrashes()
    {
        await Page.SetContentAsync("<div>This page should crash</div>");

        var crashEvent = new TaskCompletionSource<bool>();
        Page.Crash += (_, _) => crashEvent.TrySetResult(true);

        await CrashAsync(Page);
        await crashEvent.Task;
        await Page.Context.CloseAsync();
    }

    private async Task CrashAsync(IPage page)
    {
        try
        {
            await page.GotoAsync("chrome://crash");
        }
        catch
        {
        }
    }
}
