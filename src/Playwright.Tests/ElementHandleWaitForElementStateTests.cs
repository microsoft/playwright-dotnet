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

///<playwright-file>elementhandle-wait-for-element-state.spec.ts</playwright-file>
public class ElementHandleWaitForElementStateTests : PageTestEx
{
    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for visible")]
    public async Task ShouldWaitForVisible()
    {
        await Page.SetContentAsync("<div style='display:none'>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var task = div.WaitForElementStateAsync(ElementState.Visible);
        await GiveItAChanceToResolve(Page);
        Assert.False(task.IsCompleted);
        await div.EvaluateAsync("div => div.style.display = 'block'");
        await task;
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for already visible")]
    public async Task ShouldWaitForAlreadyVisible()
    {
        await Page.SetContentAsync("<div>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.WaitForElementStateAsync(ElementState.Visible);
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should timeout waiting for visible")]
    public async Task ShouldTimeoutWaitingForVisible()
    {
        await Page.SetContentAsync("<div style='display:none'>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => div.WaitForElementStateAsync(ElementState.Visible, new() { Timeout = 1000 }));
        StringAssert.Contains("Timeout 1000ms exceeded", exception.Message);
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should throw waiting for visible when detached")]
    public async Task ShouldThrowWaitingForVisibleWhenDetached()
    {
        await Page.SetContentAsync("<div style='display:none'>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var task = div.WaitForElementStateAsync(ElementState.Visible);
        await div.EvaluateAsync("div => div.remove()");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => task);
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for hidden")]
    public async Task ShouldWaitForHidden()
    {
        await Page.SetContentAsync("<div>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var task = div.WaitForElementStateAsync(ElementState.Hidden);
        await GiveItAChanceToResolve(Page);
        Assert.False(task.IsCompleted);
        await div.EvaluateAsync("div => div.style.display = 'none'");
        await task;
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for already hidden")]
    public async Task ShouldWaitForAlreadyHidden()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.WaitForElementStateAsync(ElementState.Hidden);
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should throw waiting for hidden when detached")]
    public async Task ShouldThrowWaitingForHiddenWhenDetached()
    {
        await Page.SetContentAsync("<div>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var task = div.WaitForElementStateAsync(ElementState.Hidden);
        await GiveItAChanceToResolve(Page);
        Assert.False(task.IsCompleted);
        await div.EvaluateAsync("div => div.remove()");
        await task;
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for enabled button")]
    public async Task ShouldWaitForEnabledButton()
    {
        await Page.SetContentAsync("<button disabled><span>Target</span></button>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var task = span.WaitForElementStateAsync(ElementState.Enabled);
        await GiveItAChanceToResolve(Page);
        Assert.False(task.IsCompleted);
        await span.EvaluateAsync("span => span.parentElement.disabled = false");
        await task;
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should throw waiting for enabled when detached")]
    public async Task ShouldThrowWaitingForEnabledWhenDetached()
    {
        await Page.SetContentAsync("<button disabled>Target</button>");
        var button = await Page.QuerySelectorAsync("button");
        var task = button.WaitForElementStateAsync(ElementState.Enabled);
        await button.EvaluateAsync("button => button.remove()");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => task);
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for disabled button")]
    public async Task ShouldWaitForDisabledButton()
    {
        await Page.SetContentAsync("<button><span>Target</span></button>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var task = span.WaitForElementStateAsync(ElementState.Disabled);
        await GiveItAChanceToResolve(Page);
        Assert.False(task.IsCompleted);
        await span.EvaluateAsync("span => span.parentElement.disabled = true");
        await task;
    }

    [PlaywrightTest("elementhandle-wait-for-element-state.spec.ts", "should wait for stable position")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWaitForStablePosition()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.transition = 'margin 10000ms linear 0s';
                button.style.marginLeft = '20000px';
            }");

        var task = button.WaitForElementStateAsync(ElementState.Stable);
        await GiveItAChanceToResolve(Page);
        Assert.False(task.IsCompleted);
        await button.EvaluateAsync("button => button.style.transition = ''");
        await task;
    }

    private async Task GiveItAChanceToResolve(IPage page)
    {
        for (int i = 0; i < 5; i++)
        {
            await page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
        }
    }
}
