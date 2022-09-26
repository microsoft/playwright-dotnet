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

public class LocatorIsVisibleTests : PageTestEx
{

    [PlaywrightTest("locator-is-visible.spec.ts", "isVisible and isHidden should work")]
    public async Task IsVisibleAndIsHiddenShouldWork()
    {
        await Page.SetContentAsync("<div>Hi</div><span></span>");

        var div = Page.Locator("div");
        Assert.IsTrue(await div.IsVisibleAsync());
        Assert.IsFalse(await div.IsHiddenAsync());
        Assert.IsTrue(await Page.IsVisibleAsync("div"));
        Assert.IsFalse(await Page.IsHiddenAsync("div"));

        var span = Page.Locator("span");
        Assert.IsFalse(await span.IsVisibleAsync());
        Assert.IsTrue(await span.IsHiddenAsync());
        Assert.IsFalse(await Page.IsVisibleAsync("span"));
        Assert.IsTrue(await Page.IsHiddenAsync("span"));

        Assert.IsFalse(await Page.IsVisibleAsync("no-such-element"));
        Assert.IsTrue(await Page.IsHiddenAsync("no-such-element"));
    }

    [PlaywrightTest("locator-is-visible.spec.ts", "isVisible should be true for opacity:0")]
    public async Task IsVisibleShouldBeTrueForOpacity0()
    {
        await Page.SetContentAsync("<div style=\"opacity:0\">Hi</div>");
        await Expect(Page.Locator("div")).ToBeVisibleAsync();
    }

    [PlaywrightTest("locator-is-visible.spec.ts", "isVisible should be true for element outside view")]
    public async Task IsVisibleShouldBeTrueForElementOutsideView()
    {
        await Page.SetContentAsync("<div style=\"position: absolute; left: -1000px\">Hi</div>");
        await Expect(Page.Locator("div")).ToBeVisibleAsync();
    }

    [PlaywrightTest("locator-is-visible.spec.ts", "isVisible and isHidden should work with details")]
    public async Task IsVisibleAndIsHiddenShouldWorkWithDetails()
    {
        await Page.SetContentAsync(@"
            <details>
                <summary>click to open</summary>
                <ul>
                    <li>hidden item 1</li>
                    <li>hidden item 2</li>
                    <li>hidden item 3</li>
                </ul
            </details>
            ");
        await Expect(Page.Locator("ul")).ToBeHiddenAsync();
    }

    [PlaywrightTest("locator-is-visible.spec.ts", "isVisible inside a button")]
    public async Task IsVisibleInsideAButton()
    {
        await Page.SetContentAsync("<button><span></span>a button</button>");
        var span = Page.Locator("span");
        Assert.IsFalse(await span.IsVisibleAsync());
        Assert.IsTrue(await span.IsHiddenAsync());
        Assert.IsFalse(await Page.IsVisibleAsync("span"));
        Assert.IsTrue(await Page.IsHiddenAsync("span"));
        await Expect(span).Not.ToBeVisibleAsync();
        await Expect(span).ToBeHiddenAsync();
        await span.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.Locator("button").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    [PlaywrightTest("locator-is-visible.spec.ts", "isVisible inside a role=button")]
    public async Task IsVisibleInsideARoleButton()
    {
        await Page.SetContentAsync("<div role=button><span></span>a button</div>");
        var span = Page.Locator("span");
        Assert.IsFalse(await span.IsVisibleAsync());
        Assert.IsTrue(await span.IsHiddenAsync());
        Assert.IsFalse(await Page.IsVisibleAsync("span"));
        Assert.IsTrue(await Page.IsHiddenAsync("span"));
        await Expect(span).Not.ToBeVisibleAsync();
        await Expect(span).ToBeHiddenAsync();
        await span.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.Locator("[role=button]").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }
}
