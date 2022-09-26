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

public class ElementHandleQuerySelectorTests : PageTestEx
{
    [PlaywrightTest("elementhandle-query-selector.spec.ts", "should query existing element")]
    public async Task ShouldQueryExistingElement()
    {
        await Page.GotoAsync(Server.Prefix + "/playground.html");
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = await Page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAsync(".second");
        var inner = await second.QuerySelectorAsync(".inner");
        string content = await Page.EvaluateAsync<string>("e => e.textContent", inner);
        Assert.AreEqual("A", content);
    }

    [PlaywrightTest("elementhandle-query-selector.spec.ts", "should return null for non-existing element")]
    public async Task ShouldReturnNullForNonExistingElement()
    {
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
        var html = await Page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAsync(".third");
        Assert.Null(second);
    }

    [PlaywrightTest("elementhandle-query-selector.spec.ts", "should work for adopted elements")]
    public async Task ShouldWorkForAdoptedElements()
    {
        await Page.GotoAsync(Server.EmptyPage);

        var (popup, _) = await TaskUtils.WhenAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("url => window.__popup = window.open(url)", Server.EmptyPage));

        var divHandle = await Page.EvaluateHandleAsync(@"() => {
                const div = document.createElement('div');
                document.body.appendChild(div);
                const span = document.createElement('span');
                span.textContent = 'hello';
                div.appendChild(span);
                return div;
            }") as IElementHandle;

        Assert.NotNull(divHandle);
        Assert.AreEqual("hello", await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"));

        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.EvaluateAsync(@"() => {
              const div = document.querySelector('div');
              window.__popup.document.body.appendChild(div);
            }");

        Assert.NotNull(await divHandle.QuerySelectorAsync("span"));
        Assert.AreEqual("hello", await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"));
    }

    [PlaywrightTest("elementhandle-query-selector.spec.ts", "should query existing elements")]
    public async Task ShouldQueryExistingElements()
    {
        await Page.SetContentAsync("<html><body><div>A</div><br/><div>B</div></body></html>");
        var html = await Page.QuerySelectorAsync("html");
        var elements = await html.QuerySelectorAllAsync("div");
        Assert.AreEqual(2, elements.Count());
        var tasks = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
        Assert.AreEqual(new[] { "A", "B" }, await TaskUtils.WhenAll(tasks));
    }

    [PlaywrightTest("elementhandle-query-selector.spec.ts", "should return empty array for non-existing elements")]
    public async Task ShouldReturnEmptyArrayForNonExistingElements()
    {
        await Page.SetContentAsync("<html><body><span>A</span><br/><span>B</span></body></html>");
        var html = await Page.QuerySelectorAsync("html");
        var elements = await html.QuerySelectorAllAsync("div");
        Assert.IsEmpty(elements);
    }

    [PlaywrightTest("elementhandle-query-selector.spec.ts", "xpath should query existing element")]
    public async Task XPathShouldQueryExistingElement()
    {
        await Page.GotoAsync(Server.Prefix + "/playground.html");
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
        var html = await Page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAllAsync("xpath=./body/div[contains(@class, 'second')]");
        var inner = await second.First().QuerySelectorAllAsync("xpath=./div[contains(@class, 'inner')]");
        string content = await Page.EvaluateAsync<string>("e => e.textContent", inner.First());
        Assert.AreEqual("A", content);
    }

    [PlaywrightTest("elementhandle-query-selector.spec.ts", "xpath should return null for non-existing element")]
    public async Task XPathShouldReturnNullForNonExistingElement()
    {
        await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
        var html = await Page.QuerySelectorAsync("html");
        var second = await html.QuerySelectorAllAsync("xpath=/div[contains(@class, 'third')]");
        Assert.IsEmpty(second);
    }
}
