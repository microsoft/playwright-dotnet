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

public class QuerySelectorTests : PageTestEx
{
    [PlaywrightTest("queryselector.spec.ts", "should query existing elements")]
    public async Task ShouldQueryExistingElements()
    {
        await Page.SetContentAsync("<div>A</div><br/><div>B</div>");
        var elements = await Page.QuerySelectorAllAsync("div");
        Assert.AreEqual(2, elements.Count());
        var tasks = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
        Assert.AreEqual(new[] { "A", "B" }, await TaskUtils.WhenAll(tasks));
    }

    [PlaywrightTest("queryselector.spec.ts", "should return empty array if nothing is found")]
    public async Task ShouldReturnEmptyArrayIfNothingIsFound()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var elements = await Page.QuerySelectorAllAsync("div");
        Assert.IsEmpty(elements);
    }

    [PlaywrightTest("queryselector.spec.ts", "xpath should query existing element")]
    public async Task XpathShouldQueryExistingElement()
    {
        await Page.SetContentAsync("<section>test</section>");
        var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/section");
        Assert.NotNull(elements.FirstOrDefault());
        Assert.That(elements, Has.Count.EqualTo(1));
    }

    [PlaywrightTest("queryselector.spec.ts", "should return empty array for non-existing element")]
    public async Task ShouldReturnEmptyArrayForNonExistingElement()
    {
        var elements = await Page.QuerySelectorAllAsync("//html/body/non-existing-element");
        Assert.IsEmpty(elements);
    }

    [PlaywrightTest("queryselector.spec.ts", "should return multiple elements")]
    public async Task ShouldReturnMultipleElements()
    {
        await Page.SetContentAsync("<div></div><div></div>");
        var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/div");
        Assert.AreEqual(2, elements.Count());
    }

    [PlaywrightTest("queryselector.spec.ts", "should query existing element with css selector")]
    public async Task ShouldQueryExistingElementWithCssSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("css=section");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should query existing element with text selector")]
    public async Task ShouldQueryExistingElementWithTextSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("text=\"test\"");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should query existing element with xpath selector")]
    public async Task ShouldQueryExistingElementWithXpathSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("xpath=/html/body/section");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should return null for non-existing element")]
    public async Task ShouldReturnNullForNonExistingElement()
    {
        var element = await Page.QuerySelectorAsync("non-existing-element");
        Assert.Null(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should auto-detect xpath selector")]
    public async Task ShouldAutoDetectXpathSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("//html/body/section");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should auto-detect xpath selector with starting parenthesis")]
    public async Task ShouldAutoDetectXpathSelectorWithStartingParenthesis()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("(//section)[1]");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should auto-detect text selector")]
    public async Task ShouldAutoDetectTextSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("\"test\"");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should auto-detect css selector")]
    public async Task ShouldAutoDetectCssSelector()
    {
        await Page.SetContentAsync("<section>test</section>");
        var element = await Page.QuerySelectorAsync("section");
        Assert.NotNull(element);
    }

    [PlaywrightTest("queryselector.spec.ts", "should support >> syntax")]
    public async Task ShouldSupportDoubleGreaterThanSyntax()
    {
        await Page.SetContentAsync("<section><div>test</div></section>");
        var element = await Page.QuerySelectorAsync("css=section >> css=div");
        Assert.NotNull(element);
    }
}
