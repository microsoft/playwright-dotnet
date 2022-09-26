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

public class LocatorConvenienceTests : PageTestEx
{
    [PlaywrightTest("locator-convenience.spec.ts", "should have a nice preview")]
    public async Task ShouldHaveANicePreview()
    {
        await Page.GotoAsync(Server.Prefix + "/dom.html");
        var outer = Page.Locator("#outer");
        var inner = outer.Locator("#inner");
        var check = Page.Locator("#check");

        var text = await inner.EvaluateHandleAsync("e => e.firstChild");
        await Page.EvaluateAsync("() => 1");

        Assert.AreEqual("Locator@#outer", outer.ToString());
        Assert.AreEqual("Locator@#outer >> #inner", inner.ToString());
        Assert.AreEqual("JSHandle@#text=Text,↵more text", text.ToString());
        Assert.AreEqual("Locator@#check", check.ToString());
    }

    [PlaywrightTest("locator-convenience.spec.ts", "getAttribute should work")]
    public async Task GetAttributeShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/dom.html");
        var locator = Page.Locator("#outer");
        Assert.AreEqual("value", await locator.GetAttributeAsync("name"));
        Assert.IsNull(await locator.GetAttributeAsync("foo"));
        Assert.AreEqual("value", await Page.GetAttributeAsync("#outer", "name"));
        Assert.IsNull(await Page.GetAttributeAsync("#outer", "foo"));
    }

    [PlaywrightTest("locator-convenience.spec.ts", "inputValue should work")]
    public async Task InputValueShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/dom.html");

        await Page.SelectOptionAsync("#select", "foo");
        Assert.AreEqual("foo", await Page.InputValueAsync("#select"));

        await Page.FillAsync("#textarea", "text value");
        Assert.AreEqual("text value", await Page.InputValueAsync("#textarea"));

        await Page.FillAsync("#input", "input value");
        Assert.AreEqual("input value", await Page.InputValueAsync("#input"));

        var locator = Page.Locator("#input");
        Assert.AreEqual("input value", await locator.InputValueAsync());

        var e = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.InputValueAsync("#inner"));
        StringAssert.Contains("Node is not an <input>, <textarea> or <select> element", e.Message);

        var locator2 = Page.Locator("#inner");
        e = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await locator2.InputValueAsync());

        StringAssert.Contains("Node is not an <input>, <textarea> or <select> element", e.Message);
    }

    [PlaywrightTest("locator-convenience.spec.ts", "innerHTML should work")]
    public async Task InnerHTMLShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/dom.html");
        var locator = Page.Locator("#outer");
        Assert.AreEqual("<div id=\"inner\">Text,\nmore text</div>", await locator.InnerHTMLAsync());
        Assert.AreEqual("<div id=\"inner\">Text,\nmore text</div>", await Page.InnerHTMLAsync("#outer"));
    }

    [PlaywrightTest("locator-convenience.spec.ts", "innerText should work")]
    public async Task InnerTextShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/dom.html");
        var locator = Page.Locator("#inner");
        Assert.AreEqual("Text, more text", await locator.InnerTextAsync());
        Assert.AreEqual("Text, more text", await Page.InnerTextAsync("#inner"));
    }


    [PlaywrightTest("locator-convenience.spec.ts", "innerText should throw")]
    public async Task InnerTextShouldThrow()
    {
        await Page.SetContentAsync("<svg>text</svg>");
        var e = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.InnerTextAsync("svg"));
        StringAssert.Contains("Node is not an HTMLElement", e.Message);

        var locator = Page.Locator("svg");
        e = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await locator.InnerTextAsync());
        StringAssert.Contains("Node is not an HTMLElement", e.Message);
    }

    [PlaywrightTest("locator-convenience.spec.ts", "innerText should produce log")]
    public async Task InnerTextShouldProduceLog()
    {
        await Page.SetContentAsync("<div>hello</div>");
        var locator = Page.Locator("span");
        var error = await PlaywrightAssert.ThrowsAsync<TimeoutException>(async () => await locator.InnerTextAsync(new() { Timeout = 1000 }));
        StringAssert.Contains("waiting for selector \"span\"", error.Message);
    }

    [PlaywrightTest("locator-convenience.spec.ts", "textContent should work")]
    public async Task TextContentShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/dom.html");
        var locator = Page.Locator("#inner");
        Assert.AreEqual("Text,\nmore text", await locator.TextContentAsync());
        Assert.AreEqual("Text,\nmore text", await Page.TextContentAsync("#inner"));
    }

    [PlaywrightTest("locator-convenience.spec.ts", "isEnabled and isDisabled should work")]
    public async Task IsEnabledAndIsDisabledShouldWork()
    {
        await Page.SetContentAsync(@"
<button disabled>button1</button>
<button>button2</button>
<div>div</div>
");
        var div = Page.Locator("div");
        Assert.IsTrue(await div.IsEnabledAsync());
        Assert.IsFalse(await div.IsDisabledAsync());
        Assert.IsTrue(await Page.IsEnabledAsync("div"));
        Assert.IsFalse(await Page.IsDisabledAsync("div"));

        var button1 = Page.Locator(":text(\"button1\")");
        Assert.IsFalse(await button1.IsEnabledAsync());
        Assert.IsTrue(await button1.IsDisabledAsync());
        Assert.IsFalse(await Page.IsEnabledAsync(":text(\"button1\")"));
        Assert.IsTrue(await Page.IsDisabledAsync(":text(\"button1\")"));

        var button2 = Page.Locator(":text(\"button2\")");
        Assert.IsTrue(await button2.IsEnabledAsync());
        Assert.IsFalse(await button2.IsDisabledAsync());
        Assert.IsTrue(await Page.IsEnabledAsync(":text(\"button2\")"));
        Assert.IsFalse(await Page.IsDisabledAsync(":text(\"button2\")"));
    }

    [PlaywrightTest("locator-convenience.spec.ts", "isEditable should work")]
    public async Task IsEditableShouldWork()
    {
        await Page.SetContentAsync("<input id=input1 disabled><textarea></textarea><input id=input2>");
        await Page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");

        var input1 = Page.Locator("#input1");
        Assert.IsFalse(await input1.IsEditableAsync());
        Assert.IsFalse(await Page.IsEditableAsync("#input1"));

        var input2 = Page.Locator("#input2");
        Assert.IsTrue(await input2.IsEditableAsync());
        Assert.IsTrue(await Page.IsEditableAsync("#input2"));

        var textarea = Page.Locator("textarea");
        Assert.IsFalse(await textarea.IsEditableAsync());
        Assert.IsFalse(await Page.IsEditableAsync("textarea"));
    }

    [PlaywrightTest("locator-convenience.spec.ts", "isChecked should work")]
    public async Task IsCheckedShouldWork()
    {
        await Page.SetContentAsync("<input type='checkbox' checked><div>Not a checkbox</div>");
        var element = Page.Locator("input");
        Assert.IsTrue(await element.IsCheckedAsync());
        Assert.IsTrue(await Page.IsCheckedAsync("input"));
        await element.EvaluateAsync("input => input.checked = false");

        Assert.IsFalse(await element.IsCheckedAsync());
        Assert.IsFalse(await Page.IsCheckedAsync("input"));

        var e = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(async () => await Page.IsCheckedAsync("div"));
        StringAssert.Contains("Not a checkbox or radio button", e.Message);
    }

    [PlaywrightTest("locator-convenience.spec.ts", "allTextContents should work")]
    public async Task AllTextContentsShouldWork()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div><div>C</div>");
        CollectionAssert.AreEqual(new string[] { "A", "B", "C" }, (await Page.Locator("div").AllTextContentsAsync()).ToArray());
    }

    [PlaywrightTest("locator-convenience.spec.ts", "allInnerTexts should work")]
    public async Task AllInnerTextsShouldWork()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div><div>C</div>");
        CollectionAssert.AreEqual(new string[] { "A", "B", "C" }, (await Page.Locator("div").AllInnerTextsAsync()).ToArray());
    }

    [PlaywrightTest("locator-convenience.spec.ts", "should return page")]
    public async Task ShouldReturnPageInstance()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/two-frames.html");
        var outer = Page.Locator("#outer");
        Assert.AreEqual(outer.Page, Page);

        var inner = outer.Locator("#inner");
        Assert.AreEqual(inner.Page, Page);

        var inFrame = Page.Frames[1].Locator("div");
        Assert.AreEqual(inFrame.Page, Page);
    }
}
