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

public class SelectorsCssTests : PageTestEx
{
    [PlaywrightTest("selectors-css.spec.ts", "should work for open shadow roots")]
    public async Task ShouldWorkForOpenShadowRoots()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.AreEqual("Hello from root1", await Page.EvalOnSelectorAsync<string>("css=span", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await Page.EvalOnSelectorAsync<string>("css =[attr=\"value\\ space\"]", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await Page.EvalOnSelectorAsync<string>("css =[attr='value\\ \\space']", "e => e.textContent"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("css=div div span", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await Page.EvalOnSelectorAsync<string>("css=div span + span", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await Page.EvalOnSelectorAsync<string>("css=span + [attr *=\"value\"]", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await Page.EvalOnSelectorAsync<string>("css=[data-testid=\"foo\"] + [attr*=\"value\"]", "e => e.textContent"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("css=#target", "e => e.textContent"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("css=div #target", "e => e.textContent"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("css=div div #target", "e => e.textContent"));
        Assert.Null(await Page.QuerySelectorAsync("css=div div div #target"));
        Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("css=section > div div span", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await Page.EvalOnSelectorAsync<string>("css=section > div div span:nth-child(2)", "e => e.textContent"));
        Assert.Null(await Page.QuerySelectorAsync("css=section div div div div"));

        var root2 = await Page.QuerySelectorAsync("css=div div");
        Assert.AreEqual("Hello from root2", await root2.EvalOnSelectorAsync<string>("css=#target", "e => e.textContent"));
        Assert.Null(await root2.QuerySelectorAsync("css:light=#target"));
        var root2Shadow = (IElementHandle)await root2.EvaluateHandleAsync("r => r.shadowRoot");
        Assert.AreEqual("Hello from root2", await root2Shadow.EvalOnSelectorAsync<string>("css:light=#target", "e => e.textContent"));
        var root3 = (await Page.QuerySelectorAllAsync("css=div div")).ElementAt(1);
        Assert.AreEqual("Hello from root3", await root3.EvalOnSelectorAsync<string>("text=root3", "e => e.textContent"));
        Assert.AreEqual("Hello from root3 #2", await root3.EvalOnSelectorAsync<string>("css=[attr *=\"value\"]", "e => e.textContent"));
        Assert.Null(await root3.QuerySelectorAsync("css:light=[attr*=\"value\"]"));
    }

    [PlaywrightTest("selectors-css.spec.ts", "should work with > combinator and spaces")]
    public async Task ShouldWorkWithCombinatorAndSpaces()
    {
        await Page.SetContentAsync("<div foo=\"bar\" bar=\"baz\"><span></span></div>");
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("div[foo=\"bar\"][bar=\"baz\"] > span", "e => e.outerHTML"));
    }

    [PlaywrightTest("selectors-css.spec.ts", "should work with comma separated list")]
    public async Task ShouldWorkWithCommaSeparatedList()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        Assert.AreEqual(5, await Page.EvalOnSelectorAllAsync<int>("css=span, section #root1", "els => els.length"));
        Assert.AreEqual(5, await Page.EvalOnSelectorAllAsync<int>("css=section #root1, div span", "els => els.length"));
        Assert.AreEqual("root1", await Page.EvalOnSelectorAsync<string>("css=doesnotexist, section #root1", "e => e.id"));
        Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("css=doesnotexist, section #root1", "els => els.length"));
        Assert.AreEqual(4, await Page.EvalOnSelectorAllAsync<int>("css=span,div span", "els => els.length"));
        Assert.AreEqual(4, await Page.EvalOnSelectorAllAsync<int>("css=span,div span, div div span", "els => els.length"));
        Assert.AreEqual(2, await Page.EvalOnSelectorAllAsync<int>("css=#target,[attr=\"value\\ space\"]", "els => els.length"));
        Assert.AreEqual(4, await Page.EvalOnSelectorAllAsync<int>("css=#target,[data-testid=\"foo\"],[attr=\"value\\ space\"]", "els => els.length"));
        Assert.AreEqual(4, await Page.EvalOnSelectorAllAsync<int>("css=#target,[data-testid=\"foo\"],[attr=\"value\\ space\"],span", "els => els.length"));
    }

    [PlaywrightTest("selectors-css.spec.ts", "should keep dom order with comma separated list")]
    public async Task ShouldKeepDomOrderWithCommaSeparatedList()
    {
        await Page.SetContentAsync("<section><span><div><x></x><y></y></div></span></section>");
        Assert.AreEqual("SPAN,DIV", await Page.EvalOnSelectorAllAsync<string>("css=span, div", "els => els.map(e => e.nodeName).join(',')"));
        Assert.AreEqual("SPAN,DIV", await Page.EvalOnSelectorAllAsync<string>("css=div, span", "els => els.map(e => e.nodeName).join(',')"));
        Assert.AreEqual("DIV", await Page.EvalOnSelectorAllAsync<string>("css=span div, div", "els => els.map(e => e.nodeName).join(',')"));
        Assert.AreEqual("SECTION", await Page.EvalOnSelectorAllAsync<string>("*css = section >> css = div, span", "els => els.map(e => e.nodeName).join(',')"));
        Assert.AreEqual("DIV", await Page.EvalOnSelectorAllAsync<string>("css=section >> *css = div >> css = x, y", "els => els.map(e => e.nodeName).join(',')"));
        Assert.AreEqual("SPAN,DIV", await Page.EvalOnSelectorAllAsync<string>("css=section >> *css = div, span >> css = x, y", "els => els.map(e => e.nodeName).join(',')"));
        Assert.AreEqual("SPAN,DIV", await Page.EvalOnSelectorAllAsync<string>("css=section >> *css = div, span >> css = y", "els => els.map(e => e.nodeName).join(',')"));
    }

    [PlaywrightTest("selectors-css.spec.ts", "should work with comma inside text")]
    public async Task ShouldWorkWithCommaInsideText()
    {
        await Page.SetContentAsync("<span></span><div attr=\"hello,world!\"></div>");
        Assert.AreEqual("<div attr=\"hello,world!\"></div>", await Page.EvalOnSelectorAsync<string>("css=div[attr=\"hello,world!\"]", "e => e.outerHTML"));
        Assert.AreEqual("<div attr=\"hello,world!\"></div>", await Page.EvalOnSelectorAsync<string>("css =[attr=\"hello,world!\"]", "e => e.outerHTML"));
        Assert.AreEqual("<div attr=\"hello,world!\"></div>", await Page.EvalOnSelectorAsync<string>("css=div[attr='hello,world!']", "e => e.outerHTML"));
        Assert.AreEqual("<div attr=\"hello,world!\"></div>", await Page.EvalOnSelectorAsync<string>("css=[attr='hello,world!']", "e => e.outerHTML"));
        Assert.AreEqual("<span></span>", await Page.EvalOnSelectorAsync<string>("css=div[attr=\"hello,world!\"], span", "e => e.outerHTML"));
    }

    [PlaywrightTest("selectors-css.spec.ts", "should work with attribute selectors")]
    public async Task ShouldWorkWithAttributeSelectors()
    {
        await Page.SetContentAsync("<div attr=\"hello world\" attr2=\"hello-''>>foo=bar[]\" attr3=\"] span\"><span></span></div>");
        await Page.EvaluateAsync("() => window.div = document.querySelector('div')");
        string[] selectors = new[] {
                "[attr=\"hello world\"]",
                "[attr = \"hello world\"]",
                "[attr ~= world]",
                "[attr ^=hello ]",
                "[attr $= world ]",
                "[attr *= \"llo wor\" ]",
                "[attr2 |= hello]",
                "[attr = \"Hello World\" i ]",
                "[attr *= \"llo WOR\" i]",
                "[attr $= woRLD i]",
                "[attr2 = \"hello-''>>foo=bar[]\"]",
                "[attr2 $=\"foo=bar[]\"]",
            };

        foreach (string selector in selectors)
        {
            Assert.True(await Page.EvalOnSelectorAsync<bool>(selector, "e => e === div"));
        }

        Assert.True(await Page.EvalOnSelectorAsync<bool>("[attr*=hello] span", "e => e.parentNode === div"));
        Assert.True(await Page.EvalOnSelectorAsync<bool>("[attr*=hello] >> span", "e => e.parentNode === div"));
        Assert.True(await Page.EvalOnSelectorAsync<bool>("[attr3=\"] span\"] >> span", "e => e.parentNode === div"));
    }
}
