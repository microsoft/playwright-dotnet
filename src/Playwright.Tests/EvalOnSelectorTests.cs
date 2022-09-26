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

public class EvalOnSelectorTests : PageTestEx
{
    [PlaywrightTest("eval-on-selector.spec.ts", "should work with css selector")]
    public async Task ShouldWorkWithCssSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("css=section", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with id selector")]
    public async Task ShouldWorkWithIdSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("id=testAttribute", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with data-test selector")]
    public async Task ShouldWorkWithDataTestSelector()
    {
        await Page.SetContentAsync("<section data-test=foo id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("data-test=foo", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with data-testid selector")]
    public async Task ShouldWorkWithDataTestidSelector()
    {
        await Page.SetContentAsync("<section data-testid=foo id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with data-test-id selector")]
    public async Task ShouldWorkWithDataTestIdSelector()
    {
        await Page.SetContentAsync("<section data-test-id=foo id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("data-test-id=foo", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with text selector")]
    public async Task ShouldWorkWithTextSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("text=\"43543\"", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with xpath selector")]
    public async Task ShouldWorkWithXpathSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("xpath=/html/body/section", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with text selector")]
    public async Task ShouldWorkWithTextSelector2()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("text=43543", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should auto-detect css selector")]
    public async Task ShouldAutoDetectCssSelector()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("section", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should auto-detect nested selectors")]
    public async Task ShouldAutoDetectNestedSelectors()
    {
        await Page.SetContentAsync("<div foo=bar><section>43543<span>Hello<div id=target></div></span></section></div>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("div[foo=bar] > section >> \"Hello\" >> div", "e => e.id");
        Assert.AreEqual("target", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should auto-detect css selector with attributes")]
    public async Task ShouldAutoDetectCssSelectorWithAttributes()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        string idAttribute = await Page.EvalOnSelectorAsync<string>("section[id=\"testAttribute\"]", "e => e.id");
        Assert.AreEqual("testAttribute", idAttribute);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should accept arguments")]
    public async Task ShouldAcceptArguments()
    {
        await Page.SetContentAsync("<section>hello</section>");
        string text = await Page.EvalOnSelectorAsync<string>("section", "(e, suffix) => e.textContent + suffix", " world!");
        Assert.AreEqual("hello world!", text);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should accept ElementHandles as arguments")]
    public async Task ShouldAcceptElementHandlesAsArguments()
    {
        await Page.SetContentAsync("<section>hello</section><div> world</div>");
        var divHandle = await Page.QuerySelectorAsync("div");
        string text = await Page.EvalOnSelectorAsync<string>("section", "(e, div) => e.textContent + div.textContent", divHandle);
        Assert.AreEqual("hello world", text);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should throw error if no element is found")]
    public async Task ShouldThrowErrorIfNoElementIsFound()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(()
            => Page.EvalOnSelectorAsync("section", "e => e.id"));
        StringAssert.Contains("failed to find element matching selector \"section\"", exception.Message);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should support >> syntax")]
    public async Task ShouldSupportDoubleGreaterThanSyntax()
    {
        await Page.SetContentAsync("<section><div>hello</div></section>");
        string text = await Page.EvalOnSelectorAsync<string>("css=section >> css=div", "(e, suffix) => e.textContent + suffix", " world!");
        Assert.AreEqual("hello world!", text);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should support >> syntax with different engines")]
    public async Task ShouldSupportDoubleGreaterThanSyntaxWithDifferentEngines()
    {
        await Page.SetContentAsync("<section><div><span>hello</span></div></section>");
        string text = await Page.EvalOnSelectorAsync<string>("xpath=/html/body/section >> css=div >> text=\"hello\"", "(e, suffix) => e.textContent + suffix", " world!");
        Assert.AreEqual("hello world!", text);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should support spaces with >> syntax")]
    public async Task ShouldSupportSpacesWithDoubleGreaterThanSyntax()
    {
        await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
        string text = await Page.EvalOnSelectorAsync<string>(" css = div >>css=div>>css   = span  ", "e => e.textContent");
        Assert.AreEqual("Hello from root2", text);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should not stop at first failure with >> syntax")]
    public async Task ShouldNotStopAtFirstFailureWithDoubleGraterThanSyntax()
    {
        await Page.SetContentAsync("<div><span>Next</span><button>Previous</button><button>Next</button></div>");
        string text = await Page.EvalOnSelectorAsync<string>("button >> \"Next\"", "(e) => e.outerHTML");
        Assert.AreEqual("<button>Next</button>", text);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should support * capture")]
    public async Task ShouldSupportStarCapture()
    {
        await Page.SetContentAsync("<section><div><span>a</span></div></section><section><div><span>b</span></div></section>");
        Assert.AreEqual("<div><span>b</span></div>", await Page.EvalOnSelectorAsync<string>("*css=div >> \"b\"", "(e) => e.outerHTML"));
        Assert.AreEqual("<div><span>b</span></div>", await Page.EvalOnSelectorAsync<string>("section >> *css=div >> \"b\"", "(e) => e.outerHTML"));
        Assert.AreEqual("<span>b</span>", await Page.EvalOnSelectorAsync<string>("css=div >> *text=\"b\"", "(e) => e.outerHTML"));
        Assert.NotNull(await Page.QuerySelectorAsync("*"));
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should throw on multiple * captures")]
    public async Task ShouldThrowOnMultipleStarCaptures()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvalOnSelectorAsync<string>("*css=div >> *css=span", "(e) => e.outerHTML"));
        Assert.AreEqual("Only one of the selectors can capture using * modifier", exception.Message);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should throw on malformed * capture")]
    public async Task ShouldThrowOnMalformedStarCapture()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.EvalOnSelectorAsync<string>("*=div", "(e) => e.outerHTML"));
        Assert.AreEqual("Unknown engine \"\" while parsing selector *=div", exception.Message);
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with spaces in css attributes")]
    public async Task ShouldWorkWithSpacesInCssAttributes()
    {
        await Page.SetContentAsync("<div><input placeholder=\"Select date\"></div>");
        Assert.NotNull(await Page.WaitForSelectorAsync("[placeholder = \"Select date\"]"));
        Assert.NotNull(await Page.WaitForSelectorAsync("[placeholder = 'Select date']"));
        Assert.NotNull(await Page.WaitForSelectorAsync("input[placeholder = \"Select date\"]"));
        Assert.NotNull(await Page.WaitForSelectorAsync("input[placeholder = 'Select date']"));
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select date\"]"));
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select date']"));
        Assert.NotNull(await Page.QuerySelectorAsync("input[placeholder = \"Select date\"]"));
        Assert.NotNull(await Page.QuerySelectorAsync("input[placeholder = 'Select date']"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("[placeholder = \"Select date\"]", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("[placeholder = 'Select date']", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("input[placeholder = \"Select date\"]", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("input[placeholder = 'Select date']", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css =[placeholder = \"Select date\"]", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css =[placeholder = 'Select date']", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css = input[placeholder = \"Select date\"]", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css = input[placeholder = 'Select date']", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("div >> [placeholder = \"Select date\"]", "e => e.outerHTML"));
        Assert.AreEqual("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("div >> [placeholder = 'Select date']", "e => e.outerHTML"));
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with quotes in css attributes")]
    public async Task ShouldWorkWihQuotesInCssAttributes()
    {
        await Page.SetContentAsync("<div><input placeholder=\"Select&quot;date\"></div>");
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select\\\"date\"]"));
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select\"date']"));

        await Page.SetContentAsync("<div><input placeholder=\"Select &quot; date\"></div>");
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select \\\" date\"]"));
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select \" date']"));

        await Page.SetContentAsync("<div><input placeholder=\"Select&apos;date\"></div>");
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select'date\"]"));
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select\\'date']"));

        await Page.SetContentAsync("<div><input placeholder=\"Select &apos; date\"></div>");
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select ' date\"]"));
        Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select \\' date']"));
    }

    [PlaywrightTest("eval-on-selector.spec.ts", "should work with quotes in css attributes when missing")]
    public async Task ShouldWorkWihQuotesInCssAttributesWhenMissing()
    {
        var inputTask = Page.WaitForSelectorAsync("[placeholder = \"Select\\\"date\"]");
        Assert.Null(await Page.QuerySelectorAsync("[placeholder = \"Select\\\"date\"]"));
        await Page.SetContentAsync("<div><input placeholder=\"Select&quot;date\"></div>");
        await inputTask;
    }
}
