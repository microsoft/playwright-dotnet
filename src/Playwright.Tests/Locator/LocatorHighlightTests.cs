/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

namespace Microsoft.Playwright.Tests.Locator;


public class LocatorHighlightTests : PageTestEx
{
    [PlaywrightTest("locator-highlight.spec.ts", "should highlight locator")]
    public async Task ShouldHighlightLocator()
    {
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.Locator(".box").Nth(3).HighlightAsync();
        Assert.AreEqual(await Page.Locator("x-pw-glass").IsVisibleAsync(), true);
    }

    [PlaywrightTest("locator-highlight.spec.ts", "highlight should accept a CSS string style")]
    public async Task HighlightShouldAcceptCssStringStyle()
    {
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.Locator(".box").Nth(3).HighlightAsync(new() { Style = "outline: 3px solid rgb(255, 0, 0)" });
        Assert.AreEqual(await Page.Locator("x-pw-glass").IsVisibleAsync(), true);
    }

    [PlaywrightTest("locator-highlight.spec.ts", "hideHighlight should not throw")]
    public async Task HideHighlightShouldNotThrow()
    {
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        var locator = Page.Locator(".box").Nth(3);
        await locator.HighlightAsync();
        await locator.HideHighlightAsync();
    }

    [PlaywrightTest("locator-highlight.spec.ts", "page.hideHighlight should work")]
    public async Task PageHideHighlightShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.Locator(".box").Nth(3).HighlightAsync();
        await Page.HideHighlightAsync();
    }

    [PlaywrightTest("locator-highlight.spec.ts", "highlight returns disposable")]
    public async Task HighlightReturnsDisposable()
    {
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        var locator = Page.Locator(".box").Nth(3);
        await using var disposable = await locator.HighlightAsync();
        Assert.IsNotNull(disposable);
    }
}
