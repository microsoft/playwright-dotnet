/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-convenience.spec.ts.</summary>
public class LocatorConvenienceTests : UpstreamPageTest
{
    [UpstreamTest("LO-057 should have a nice preview")]
    public async Task ShouldHaveANicePreview()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var outer = Page.Locator("#outer");
        var inner = outer.Locator("#inner");
        var check = Page.Locator("#check");
        var text = await inner.EvaluateHandleAsync("e => e.firstChild");
        await Page.EvaluateAsync("() => 1");  // Give them a chance to calculate the preview.
        Assert.That(outer.ToString(), Is.EqualTo("Locator(\"#outer\")"));
        Assert.That(inner.ToString(), Is.EqualTo("Locator(\"#outer\").Locator(\"#inner\")"));
        Assert.That(text.ToString(), Is.EqualTo("JSHandle@#text=Text,↵more text"));
        Assert.That(check.ToString(), Is.EqualTo("Locator(\"#check\")"));
    }

    [UpstreamTest("LO-058 getAttribute should work")]
    public async Task GetAttributeShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var locator = Page.Locator("#outer");
        Assert.That(await locator.GetAttributeAsync("name"), Is.EqualTo("value"));
        Assert.That(await locator.GetAttributeAsync("foo"), Is.Null);
        Assert.That(await Page.GetAttributeAsync("#outer", "name"), Is.EqualTo("value"));
        Assert.That(await Page.GetAttributeAsync("#outer", "foo"), Is.Null);
    }

    [UpstreamTest("LO-059 inputValue should work")]
    public async Task InputValueShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");

        await Page.SelectOptionAsync("#select", "foo");
        Assert.That(await Page.InputValueAsync("#select"), Is.EqualTo("foo"));

        await Page.FillAsync("#textarea", "text value");
        Assert.That(await Page.InputValueAsync("#textarea"), Is.EqualTo("text value"));

        await Page.FillAsync("#input", "input value");
        Assert.That(await Page.InputValueAsync("#input"), Is.EqualTo("input value"));
        var locator = Page.Locator("#input");
        Assert.That(await locator.InputValueAsync(), Is.EqualTo("input value"));

        Assert.That((await Catch(() => Page.InputValueAsync("#inner"))).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
        var locator2 = Page.Locator("#inner");
        Assert.That((await Catch(() => locator2.InputValueAsync())).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
    }

    [UpstreamTest("LO-060 innerHTML should work")]
    public async Task InnerHTMLShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var locator = Page.Locator("#outer");
        Assert.That(await locator.InnerHTMLAsync(), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
        Assert.That(await Page.InnerHTMLAsync("#outer"), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
    }

    [UpstreamTest("LO-061 innerText should work")]
    public async Task InnerTextShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var locator = Page.Locator("#inner");
        Assert.That(await locator.InnerTextAsync(), Is.EqualTo("Text, more text"));
        Assert.That(await Page.InnerTextAsync("#inner"), Is.EqualTo("Text, more text"));
    }

    [UpstreamTest("LO-062 innerText should throw")]
    public async Task InnerTextShouldThrow()
    {
        await Page.SetContentAsync("<svg>text</svg>");
        var error1 = await Catch(() => Page.InnerTextAsync("svg"));
        Assert.That(error1.Message, Does.Contain("Node is not an HTMLElement"));
        var locator = Page.Locator("svg");
        var error2 = await Catch(() => locator.InnerTextAsync());
        Assert.That(error2.Message, Does.Contain("Node is not an HTMLElement"));
    }

    [UpstreamTest("LO-063 innerText should produce log")]
    public async Task InnerTextShouldProduceLog()
    {
        await Page.SetContentAsync("<div>Hello</div>");
        var locator = Page.Locator("span");
        var error = await Catch(() => locator.InnerTextAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("waiting for Locator(\"span\")"));
    }

    [UpstreamTest("LO-064 textContent should work")]
    public async Task TextContentShouldWork()
    {
        await Page.GotoAsync($"{Server.Prefix}/dom.html");
        var locator = Page.Locator("#inner");
        Assert.That(await locator.TextContentAsync(), Is.EqualTo("Text,\nmore text"));
        Assert.That(await Page.TextContentAsync("#inner"), Is.EqualTo("Text,\nmore text"));
    }

    [UpstreamTest("LO-065 isEnabled and isDisabled should work")]
    public async Task IsEnabledAndIsDisabledShouldWork()
    {
        await Page.SetContentAsync(@"
    <button disabled>button1</button>
    <button>button2</button>
    <div>div</div>
  ");
        var div = Page.Locator("div");
        Assert.That(await div.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await div.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEnabledAsync("div"), Is.EqualTo(true));
        Assert.That(await Page.IsDisabledAsync("div"), Is.EqualTo(false));
        var button1 = Page.Locator(":text(\"button1\")");
        Assert.That(await button1.IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await button1.IsDisabledAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsEnabledAsync(":text(\"button1\")"), Is.EqualTo(false));
        Assert.That(await Page.IsDisabledAsync(":text(\"button1\")"), Is.EqualTo(true));
        var button2 = Page.Locator(":text(\"button2\")");
        Assert.That(await button2.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await button2.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEnabledAsync(":text(\"button2\")"), Is.EqualTo(true));
        Assert.That(await Page.IsDisabledAsync(":text(\"button2\")"), Is.EqualTo(false));
    }

    [UpstreamTest("LO-066 isEditable should work")]
    public async Task IsEditableShouldWork()
    {
        await Page.SetContentAsync(@"
    <input id=input1 disabled>
    <textarea></textarea>
    <input id=input2>
    <div contenteditable=""true""></div>
    <span id=span1 role=textbox aria-readonly=true></span>
    <span id=span2 role=textbox></span>
    <button>button</button>
  ");
        await Page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");
        var input1 = Page.Locator("#input1");
        Assert.That(await input1.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEditableAsync("#input1"), Is.EqualTo(false));
        var input2 = Page.Locator("#input2");
        Assert.That(await input2.IsEditableAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsEditableAsync("#input2"), Is.EqualTo(true));
        var textarea = Page.Locator("textarea");
        Assert.That(await textarea.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsEditableAsync("textarea"), Is.EqualTo(false));
        Assert.That(await Page.Locator("div").IsEditableAsync(), Is.EqualTo(true));
        Assert.That(await Page.Locator("#span1").IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await Page.Locator("#span2").IsEditableAsync(), Is.EqualTo(true));
        var error = await Catch(() => Page.Locator("button").IsEditableAsync());
        Assert.That(error.Message, Does.Contain("Element is not an <input>, <textarea>, <select> or [contenteditable] and does not have a role allowing [aria-readonly]"));
    }

    [UpstreamTest("LO-067 isChecked should work")]
    public async Task IsCheckedShouldWork()
    {
        await Page.SetContentAsync("<input type='checkbox' checked><div>Not a checkbox</div>");
        var element = Page.Locator("input");
        Assert.That(await element.IsCheckedAsync(), Is.EqualTo(true));
        Assert.That(await Page.IsCheckedAsync("input"), Is.EqualTo(true));
        await element.EvaluateAsync("input => input.checked = false");
        Assert.That(await element.IsCheckedAsync(), Is.EqualTo(false));
        Assert.That(await Page.IsCheckedAsync("input"), Is.EqualTo(false));
        var error = await Catch(() => Page.IsCheckedAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }

    [UpstreamTest("LO-068 isChecked should work for indeterminate input")]
    public async Task IsCheckedShouldWorkForIndeterminateInput()
    {
        await Page.SetContentAsync("<input type=\"checkbox\" checked>");
        await Page.Locator("input").EvaluateAsync("e => e.indeterminate = true");

        Assert.That(await Page.Locator("input").IsCheckedAsync(), Is.EqualTo(true));
        await Expect(Page.Locator("input")).ToBeCheckedAsync();

        await Page.Locator("input").UncheckAsync();

        Assert.That(await Page.Locator("input").IsCheckedAsync(), Is.EqualTo(false));
        await Expect(Page.Locator("input")).Not.ToBeCheckedAsync();
    }

    [UpstreamTest("LO-069 allTextContents should work")]
    public async Task AllTextContentsShouldWork()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div><div>C</div>");
        Assert.That(await Page.Locator("div").AllTextContentsAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [UpstreamTest("LO-070 allInnerTexts should work")]
    public async Task AllInnerTextsShouldWork()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div><div>C</div>");
        Assert.That(await Page.Locator("div").AllInnerTextsAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [UpstreamTest("LO-071 should return page")]
    public async Task ShouldReturnPage()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/two-frames.html");
        var outer = Page.Locator("#outer");
        Assert.That(outer.Page, Is.SameAs(Page));

        var inner = outer.Locator("#inner");
        Assert.That(inner.Page, Is.SameAs(Page));

        var inFrame = Page.Frames[1].Locator("div");
        Assert.That(inFrame.Page, Is.SameAs(Page));
    }

    [UpstreamTest("LO-072 description should return null for locator without description")]
    public async Task DescriptionShouldReturnNullForLocatorWithoutDescription()
    {
        var locator = Page.Locator("button");
        Assert.That(locator.Description, Is.Null);
    }

    [UpstreamTest("LO-073 description should return description for locator with simple description")]
    public async Task DescriptionShouldReturnDescriptionForLocatorWithSimpleDescription()
    {
        var locator = Page.Locator("button").Describe("Submit button");
        Assert.That(locator.Description, Is.EqualTo("Submit button"));
    }

    [UpstreamTest("LO-074 description should return description with special characters")]
    public async Task DescriptionShouldReturnDescriptionWithSpecialCharacters()
    {
        var locator = Page.Locator("div").Describe("Button with \"quotes\" and 'apostrophes'");
        Assert.That(locator.Description, Is.EqualTo("Button with \"quotes\" and 'apostrophes'"));
    }

    [UpstreamTest("LO-075 description should return description for chained locators")]
    public async Task DescriptionShouldReturnDescriptionForChainedLocators()
    {
        var locator = Page.Locator("form").Locator("input").Describe("Form input field");
        Assert.That(locator.Description, Is.EqualTo("Form input field"));
    }

    [UpstreamTest("LO-076 description should return description for locator with multiple describe calls")]
    public async Task DescriptionShouldReturnDescriptionForLocatorWithMultipleDescribeCalls()
    {
        var locator1 = Page.Locator("foo").Describe("First description");
        Assert.That(locator1.Description, Is.EqualTo("First description"));
        var locator2 = locator1.Locator("button").Describe("Second description");
        Assert.That(locator2.Description, Is.EqualTo("Second description"));
        var locator3 = locator2.Locator("button");
        Assert.That(locator3.Description, Is.Null);
    }

    [UpstreamTest("LO-077 toString() returns formatted locator")]
    public async Task ToStringReturnsFormattedLocator()
    {
        var locator = Page.GetByRole(AriaRole.Button, new() { Name = "Submit" });
        Assert.That(locator.ToString(), Is.EqualTo("GetByRole(AriaRole.Button, new() { Name = \"Submit\" })"));
        Assert.That(locator.Description, Is.Null);
    }

    [UpstreamTest("LO-078 toString() prefers description")]
    public async Task ToStringPrefersDescription()
    {
        var locator = Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).Describe("Submit button");
        Assert.That(locator.ToString(), Is.EqualTo("Submit button"));
        Assert.That(locator.ToString(), Is.EqualTo(locator.Description));
    }
}
