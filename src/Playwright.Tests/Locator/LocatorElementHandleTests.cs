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

public class LocatorElementHandleTests : PageTestEx
{
    [PlaywrightTest("locator-element-handle.spec.ts", "should query existing element")]
    public async Task ShouldQueryExistingElement()
    {
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");

        var html = Page.Locator("html");
        var second = html.Locator(".second");
        var inner = second.Locator(".inner");
        var content = await Page.EvaluateAsync<string>("e => e.textContent", await inner.ElementHandleAsync());
        Assert.AreEqual("A", content);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "should query existing elements")]
    public async Task ShouldQueryExistingElements()
    {
        await Page.SetContentAsync("<html><body><div>A</div><br/><div>B</div></body></html>");
        var html = Page.Locator("html");
        var elements = await html.Locator("div").ElementHandlesAsync();
        Assert.AreEqual(2, elements.Count);
        var promises = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
        CollectionAssert.AreEqual(new string[] { "A", "B" }, await Task.WhenAll(promises));
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "should return empty array for non-existing elements")]
    public async Task ShouldReturnEmptyArrayForNonExistingElements()
    {
        await Page.SetContentAsync("<html><body><span>A</span><br/><span>B</span></body></html>");
        var html = Page.Locator("html");
        var elements = await html.Locator("div").ElementHandlesAsync();
        Assert.AreEqual(0, elements.Count);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "xpath should query existing element")]
    public async Task XPathShouldQueryExistingElement()
    {
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = Page.Locator("html");
        var second = html.Locator("xpath=./body/div[contains(@class, 'second')]");
        var inner = second.Locator("xpath=./div[contains(@class, 'inner')]");
        var content = await Page.EvaluateAsync<string>("e => e.textContent", await inner.ElementHandleAsync());
        Assert.AreEqual("A", content);
    }

    [PlaywrightTest("locator-element-handle.spec.ts", "xpath should return null for non-existing element")]
    public async Task XPathShouldReturnNullForNonExistingElement()
    {
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = Page.Locator("html");
        var second = await html.Locator("xpath=/div[contains(@class, 'third')]").ElementHandlesAsync();
        Assert.AreEqual(0, second.Count);
    }
}
