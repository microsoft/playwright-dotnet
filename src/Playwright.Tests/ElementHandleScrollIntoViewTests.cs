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

public class ElementHandleScrollIntoViewTests : PageTestEx
{
    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");
        for (int i = 0; i < 11; ++i)
        {
            var button = await Page.QuerySelectorAsync("#btn" + i);
            double before = await button.EvaluateAsync<double>(@"button => {
                    return button.getBoundingClientRect().right - window.innerWidth;
                }");
            Assert.AreEqual(10 * i, before);
            await button.ScrollIntoViewIfNeededAsync();
            double after = await button.EvaluateAsync<double>(@"button => {
                    return button.getBoundingClientRect().right - window.innerWidth;
                }");
            Assert.True(after <= 0);
            await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should throw for detached element")]
    public async Task ShouldThrowForDetachedElement()
    {
        await Page.SetContentAsync("<div>Hello</div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.EvaluateAsync("div => div.remove()");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => div.ScrollIntoViewIfNeededAsync());
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for display:none to become visible")]
    public async Task ShouldWaitForDisplayNoneToBecomeVisible()
    {
        await Page.SetContentAsync("<div style=\"display: none\">Hello</div>");
        await TestWaitingAsync(Page, "div => div.style.display = 'block'");
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should scroll display:contents into view'")]
    [Ignore("https://github.com/microsoft/playwright/issues/15034")]
    public async Task ShouldScollDisplayContentsIntoView()
    {
        await Page.SetContentAsync(@"
                <div id=container style=""width:200px;height:200px;overflow:scroll;border:1px solid black;"">
                    <div style=""margin-top:500px;background:red;"">
                        <div style=""height:50px;width:100px;background:cyan;"">
                        <div id=target style=""display:contents"">Hello</div>
                        </div>
                    <div>
                </div>
            ");
        var div = await Page.QuerySelectorAsync("#target");
        await div.ScrollIntoViewIfNeededAsync();
        Assert.AreEqual(350, await Page.EvalOnSelectorAsync<int>("#container", "e => e.scrollTop"));
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should work for visibility:hidden element")]
    public async Task ShouldWaitForVisibilityHiddenToBecomeVisible()
    {
        await Page.SetContentAsync("<div style=\"visibility:hidden\">Hello</div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.ScrollIntoViewIfNeededAsync();
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should work for zero-sized element")]
    public async Task ShouldWaitForZeroSizedElementToBecomeVisible()
    {
        await Page.SetContentAsync("<div style=\"height:0\">Hello</div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.ScrollIntoViewIfNeededAsync();
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should wait for nested display:none to become visible")]
    public async Task ShouldWaitForNestedDisplayNoneToBecomeVisible()
    {
        await Page.SetContentAsync("<span style=\"display: none\"><div>Hello</div></span>");
        await TestWaitingAsync(Page, "div => div.parentElement.style.display = 'block'");
    }

    [PlaywrightTest("elementhandle-scroll-into-view.spec.ts", "should timeout waiting for visible")]
    public async Task ShouldTimeoutWaitingForVisible()
    {
        await Page.SetContentAsync("<div style=\"display: none\">Hello</div>");
        var div = await Page.QuerySelectorAsync("div");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => div.ScrollIntoViewIfNeededAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("element is not displayed, retrying in 100ms", exception.Message);
    }

    private async Task TestWaitingAsync(IPage page, string after)
    {
        var div = await page.QuerySelectorAsync("div");
        var task = div.ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
        Assert.False(task.IsCompleted);
        await div.EvaluateAsync(after);
        await task;
    }
}
