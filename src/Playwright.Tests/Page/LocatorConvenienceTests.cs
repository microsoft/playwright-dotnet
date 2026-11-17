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
public class LocatorConvenienceTests
{
    [UpstreamTest("LO-057 should have a nice preview")]
    [LocalFixme("dotnet: Locator.ToString() gives the C# form, 'Locator(\"#outer\")'; upstream expects \"locator('#outer')\"")]
    public async Task ShouldHaveANicePreview(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var outer = page.Locator("#outer");
        var inner = outer.Locator("#inner");
        var check = page.Locator("#check");
        var text = await inner.EvaluateHandleAsync("e => e.firstChild");
        await page.EvaluateAsync("() => 1");  // Give them a chance to calculate the preview.
        Assert.That(outer.ToString(), Is.EqualTo("locator('#outer')"));
        Assert.That(inner.ToString(), Is.EqualTo("locator('#outer').locator('#inner')"));
        Assert.That(text.ToString(), Is.EqualTo("JSHandle@#text=Text,↵more text"));
        Assert.That(check.ToString(), Is.EqualTo("locator('#check')"));
    }

    [UpstreamTest("LO-058 getAttribute should work")]
    public async Task GetAttributeShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var locator = page.Locator("#outer");
        Assert.That(await locator.GetAttributeAsync("name"), Is.EqualTo("value"));
        Assert.That(await locator.GetAttributeAsync("foo"), Is.Null);
        Assert.That(await page.GetAttributeAsync("#outer", "name"), Is.EqualTo("value"));
        Assert.That(await page.GetAttributeAsync("#outer", "foo"), Is.Null);
    }

    [UpstreamTest("LO-059 inputValue should work")]
    public async Task InputValueShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");

        await page.SelectOptionAsync("#select", "foo");
        Assert.That(await page.InputValueAsync("#select"), Is.EqualTo("foo"));

        await page.FillAsync("#textarea", "text value");
        Assert.That(await page.InputValueAsync("#textarea"), Is.EqualTo("text value"));

        await page.FillAsync("#input", "input value");
        Assert.That(await page.InputValueAsync("#input"), Is.EqualTo("input value"));
        var locator = page.Locator("#input");
        Assert.That(await locator.InputValueAsync(), Is.EqualTo("input value"));

        Assert.That((await Catch(() => page.InputValueAsync("#inner"))).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
        var locator2 = page.Locator("#inner");
        Assert.That((await Catch(() => locator2.InputValueAsync())).Message, Does.Contain("Node is not an <input>, <textarea> or <select> element"));
    }

    [UpstreamTest("LO-060 innerHTML should work")]
    public async Task InnerHTMLShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var locator = page.Locator("#outer");
        Assert.That(await locator.InnerHTMLAsync(), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
        Assert.That(await page.InnerHTMLAsync("#outer"), Is.EqualTo("<div id=\"inner\">Text,\nmore text</div>"));
    }

    [UpstreamTest("LO-061 innerText should work")]
    public async Task InnerTextShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var locator = page.Locator("#inner");
        Assert.That(await locator.InnerTextAsync(), Is.EqualTo("Text, more text"));
        Assert.That(await page.InnerTextAsync("#inner"), Is.EqualTo("Text, more text"));
    }

    [UpstreamTest("LO-062 innerText should throw")]
    public async Task InnerTextShouldThrow(IPage page, Server server)
    {
        await page.SetContentAsync("<svg>text</svg>");
        var error1 = await Catch(() => page.InnerTextAsync("svg"));
        Assert.That(error1.Message, Does.Contain("Node is not an HTMLElement"));
        var locator = page.Locator("svg");
        var error2 = await Catch(() => locator.InnerTextAsync());
        Assert.That(error2.Message, Does.Contain("Node is not an HTMLElement"));
    }

    [UpstreamTest("LO-063 innerText should produce log")]
    [LocalFixme("dotnet: the call log has Locator(\"span\"), upstream expects locator('span')")]
    public async Task InnerTextShouldProduceLog(IPage page, Server server)
    {
        await page.SetContentAsync("<div>Hello</div>");
        var locator = page.Locator("span");
        var error = await Catch(() => locator.InnerTextAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("waiting for locator('span')"));
    }

    [UpstreamTest("LO-064 textContent should work")]
    public async Task TextContentShouldWork(IPage page, Server server)
    {
        await page.GotoAsync($"{server.Prefix}/dom.html");
        var locator = page.Locator("#inner");
        Assert.That(await locator.TextContentAsync(), Is.EqualTo("Text,\nmore text"));
        Assert.That(await page.TextContentAsync("#inner"), Is.EqualTo("Text,\nmore text"));
    }

    [UpstreamTest("LO-065 isEnabled and isDisabled should work")]
    public async Task IsEnabledAndIsDisabledShouldWork(IPage page)
    {
        await page.SetContentAsync(@"
    <button disabled>button1</button>
    <button>button2</button>
    <div>div</div>
  ");
        var div = page.Locator("div");
        Assert.That(await div.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await div.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEnabledAsync("div"), Is.EqualTo(true));
        Assert.That(await page.IsDisabledAsync("div"), Is.EqualTo(false));
        var button1 = page.Locator(":text(\"button1\")");
        Assert.That(await button1.IsEnabledAsync(), Is.EqualTo(false));
        Assert.That(await button1.IsDisabledAsync(), Is.EqualTo(true));
        Assert.That(await page.IsEnabledAsync(":text(\"button1\")"), Is.EqualTo(false));
        Assert.That(await page.IsDisabledAsync(":text(\"button1\")"), Is.EqualTo(true));
        var button2 = page.Locator(":text(\"button2\")");
        Assert.That(await button2.IsEnabledAsync(), Is.EqualTo(true));
        Assert.That(await button2.IsDisabledAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEnabledAsync(":text(\"button2\")"), Is.EqualTo(true));
        Assert.That(await page.IsDisabledAsync(":text(\"button2\")"), Is.EqualTo(false));
    }

    [UpstreamTest("LO-066 isEditable should work")]
    public async Task IsEditableShouldWork(IPage page)
    {
        await page.SetContentAsync(@"
    <input id=input1 disabled>
    <textarea></textarea>
    <input id=input2>
    <div contenteditable=""true""></div>
    <span id=span1 role=textbox aria-readonly=true></span>
    <span id=span2 role=textbox></span>
    <button>button</button>
  ");
        await page.EvalOnSelectorAsync("textarea", "t => t.readOnly = true");
        var input1 = page.Locator("#input1");
        Assert.That(await input1.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEditableAsync("#input1"), Is.EqualTo(false));
        var input2 = page.Locator("#input2");
        Assert.That(await input2.IsEditableAsync(), Is.EqualTo(true));
        Assert.That(await page.IsEditableAsync("#input2"), Is.EqualTo(true));
        var textarea = page.Locator("textarea");
        Assert.That(await textarea.IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await page.IsEditableAsync("textarea"), Is.EqualTo(false));
        Assert.That(await page.Locator("div").IsEditableAsync(), Is.EqualTo(true));
        Assert.That(await page.Locator("#span1").IsEditableAsync(), Is.EqualTo(false));
        Assert.That(await page.Locator("#span2").IsEditableAsync(), Is.EqualTo(true));
        var error = await Catch(() => page.Locator("button").IsEditableAsync());
        Assert.That(error.Message, Does.Contain("Element is not an <input>, <textarea>, <select> or [contenteditable] and does not have a role allowing [aria-readonly]"));
    }

    [UpstreamTest("LO-067 isChecked should work")]
    public async Task IsCheckedShouldWork(IPage page)
    {
        await page.SetContentAsync("<input type='checkbox' checked><div>Not a checkbox</div>");
        var element = page.Locator("input");
        Assert.That(await element.IsCheckedAsync(), Is.EqualTo(true));
        Assert.That(await page.IsCheckedAsync("input"), Is.EqualTo(true));
        await element.EvaluateAsync("input => input.checked = false");
        Assert.That(await element.IsCheckedAsync(), Is.EqualTo(false));
        Assert.That(await page.IsCheckedAsync("input"), Is.EqualTo(false));
        var error = await Catch(() => page.IsCheckedAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }

    [UpstreamTest("LO-068 isChecked should work for indeterminate input")]
    public async Task IsCheckedShouldWorkForIndeterminateInput(IPage page)
    {
        await page.SetContentAsync("<input type=\"checkbox\" checked>");
        await page.Locator("input").EvaluateAsync("e => e.indeterminate = true");

        Assert.That(await page.Locator("input").IsCheckedAsync(), Is.EqualTo(true));
        await Expect(page.Locator("input")).ToBeCheckedAsync();

        await page.Locator("input").UncheckAsync();

        Assert.That(await page.Locator("input").IsCheckedAsync(), Is.EqualTo(false));
        await Expect(page.Locator("input")).Not.ToBeCheckedAsync();
    }

    [UpstreamTest("LO-069 allTextContents should work")]
    public async Task AllTextContentsShouldWork(IPage page)
    {
        await page.SetContentAsync("<div>A</div><div>B</div><div>C</div>");
        Assert.That(await page.Locator("div").AllTextContentsAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [UpstreamTest("LO-070 allInnerTexts should work")]
    public async Task AllInnerTextsShouldWork(IPage page)
    {
        await page.SetContentAsync("<div>A</div><div>B</div><div>C</div>");
        Assert.That(await page.Locator("div").AllInnerTextsAsync(), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [UpstreamTest("LO-071 should return page")]
    public async Task ShouldReturnPage(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/two-frames.html");
        var outer = page.Locator("#outer");
        Assert.That(outer.Page, Is.SameAs(page));

        var inner = outer.Locator("#inner");
        Assert.That(inner.Page, Is.SameAs(page));

        var inFrame = page.Frames[1].Locator("div");
        Assert.That(inFrame.Page, Is.SameAs(page));
    }

    [UpstreamTest("LO-072 description should return null for locator without description")]
    public async Task DescriptionShouldReturnNullForLocatorWithoutDescription(IPage page)
    {
        var locator = page.Locator("button");
        Assert.That(locator.Description, Is.Null);
    }

    [UpstreamTest("LO-073 description should return description for locator with simple description")]
    public async Task DescriptionShouldReturnDescriptionForLocatorWithSimpleDescription(IPage page)
    {
        var locator = page.Locator("button").Describe("Submit button");
        Assert.That(locator.Description, Is.EqualTo("Submit button"));
    }

    [UpstreamTest("LO-074 description should return description with special characters")]
    public async Task DescriptionShouldReturnDescriptionWithSpecialCharacters(IPage page)
    {
        var locator = page.Locator("div").Describe("Button with \"quotes\" and 'apostrophes'");
        Assert.That(locator.Description, Is.EqualTo("Button with \"quotes\" and 'apostrophes'"));
    }

    [UpstreamTest("LO-075 description should return description for chained locators")]
    public async Task DescriptionShouldReturnDescriptionForChainedLocators(IPage page)
    {
        var locator = page.Locator("form").Locator("input").Describe("Form input field");
        Assert.That(locator.Description, Is.EqualTo("Form input field"));
    }

    [UpstreamTest("LO-076 description should return description for locator with multiple describe calls")]
    public async Task DescriptionShouldReturnDescriptionForLocatorWithMultipleDescribeCalls(IPage page)
    {
        var locator1 = page.Locator("foo").Describe("First description");
        Assert.That(locator1.Description, Is.EqualTo("First description"));
        var locator2 = locator1.Locator("button").Describe("Second description");
        Assert.That(locator2.Description, Is.EqualTo("Second description"));
        var locator3 = locator2.Locator("button");
        Assert.That(locator3.Description, Is.Null);
    }

    [UpstreamTest("LO-077 toString() returns formatted locator")]
    [LocalFixme("dotnet: Locator.ToString() gives the C# form, 'GetByRole(AriaRole.Button, new() { Name = \"Submit\" })'; upstream expects \"getByRole('button', { name: 'Submit' })\"")]
    public async Task ToStringReturnsFormattedLocator(IPage page)
    {
        var locator = page.GetByRole(AriaRole.Button, new() { Name = "Submit" });
        Assert.That(locator.ToString(), Is.EqualTo("getByRole('button', { name: 'Submit' })"));
        Assert.That(locator.Description, Is.Null);
    }

    [UpstreamTest("LO-078 toString() prefers description")]
    public async Task ToStringPrefersDescription(IPage page)
    {
        var locator = page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).Describe("Submit button");
        Assert.That(locator.ToString(), Is.EqualTo("Submit button"));
        Assert.That(locator.ToString(), Is.EqualTo(locator.Description));
    }
}
