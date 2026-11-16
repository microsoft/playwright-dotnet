/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/selectors-get-by.spec.ts.</summary>
public class SelectorsGetByTests
{
    [UpstreamTest("SE-071 getByTestId should work")]
    public async Task GetByTestIdShouldWork(IPage page)
    {
        await page.SetContentAsync("<div><div data-testid=\"Hello\">Hello world</div></div>");
        await Expect(page.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-072 getByTestId with custom testId should work")]
    public async Task GetByTestIdWithCustomTestIdShouldWork(IPage page, IPlaywright playwright)
    {
        await page.SetContentAsync("<div><div data-my-custom-testid=\"Hello\">Hello world</div></div>");
        playwright.Selectors.SetTestIdAttribute("data-my-custom-testid");
        await Expect(page.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-073 getByTestId with comma-separated testIdAttributes should match any")]
    public async Task GetByTestIdWithCommaSeparatedTestIdAttributesShouldMatchAny(IPage page, IPlaywright playwright)
    {
        await page.SetContentAsync(@"
    <section>
      <div data-pw=""Hello"">first</div>
      <div data-ti=""Hello"">second</div>
      <div data-testid=""Hello"">third</div>
    </section>
  ");
        playwright.Selectors.SetTestIdAttribute("data-pw,data-ti");
        await Expect(page.GetByTestId("Hello")).ToHaveCountAsync(2);
        await Expect(page.GetByTestId("Hello")).ToHaveTextAsync(new[] { "first", "second" });
        await Expect(page.MainFrame.GetByTestId("Hello")).ToHaveCountAsync(2);
        await Expect(page.Locator("section").GetByTestId("Hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-074 getByTestId should escape id")]
    public async Task GetByTestIdShouldEscapeId(IPage page)
    {
        await page.SetContentAsync("<div><div data-testid='He\"llo'>Hello world</div></div>");
        await Expect(page.GetByTestId("He\"llo")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-075 getByTestId should work for regex")]
    public async Task GetByTestIdShouldWorkForRegex(IPage page)
    {
        await page.SetContentAsync("<div><div data-testid=\"Hello\">Hello world</div></div>");
        await Expect(page.GetByTestId(new Regex("He[l]*o"))).ToHaveTextAsync("Hello world");
        await Expect(page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-076 getByText should work")]
    public async Task GetByTextShouldWork(IPage page)
    {
        await page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        Assert.That(await page.GetByText("ye").EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));
        Assert.That(await page.GetByText(new Regex("ye")).EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));
        Assert.That(await page.GetByText(new Regex("e")).EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));

        await page.SetContentAsync("<div> ye </div><div>ye</div>");
        Assert.That(await page.GetByText("ye", new() { Exact = true }).First.EvaluateAsync<string>("e => e.outerHTML"), Does.Contain("> ye </div>"));

        await page.SetContentAsync("<div>Hello world</div><div>Hello</div>");
        Assert.That(await page.GetByText("Hello", new() { Exact = true }).EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">Hello</div>"));
    }

    [UpstreamTest("SE-077 getByLabel should work")]
    public async Task GetByLabelShouldWork(IPage page)
    {
        await page.SetContentAsync("<div><label for=target>Name</label><input id=target type=text></div>");
        Assert.That(await page.GetByText("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("LABEL"));
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("INPUT"));
        Assert.That(await page.MainFrame.GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("INPUT"));
        Assert.That(await page.Locator("div").GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("INPUT"));

        await page.SetContentAsync(@"
    <label for=""city"">City</label><my-field id=""city"" role=""combobox"">Pick one</my-field>
    <label>Street <my-field id=""street"" role=""combobox"">Pick one</my-field></label>
  ");
        await page.EvaluateAsync(@"() => {
    customElements.define('my-field', class extends HTMLElement {
      static formAssociated = true;
      constructor() {
        super();
        this.attachInternals();
      }
    });
  }");
        Assert.That(await page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('label')].map(label => label.control?.id)"), Is.EqualTo(new[] { "city", "street" }));
        Assert.That(await page.GetByLabel("City").EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "city" }));
        Assert.That(await page.GetByLabel("Street").EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "street" }));
        Assert.That(await page.GetByRole(AriaRole.Combobox, new() { Name = "City", Exact = true }).EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "city" }));
        Assert.That(await page.GetByRole(AriaRole.Combobox, new() { Name = "Street", Exact = true }).EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "street" }));
    }

    [UpstreamTest("SE-078 getByLabel should work with nested elements")]
    public async Task GetByLabelShouldWorkWithNestedElements(IPage page)
    {
        await page.SetContentAsync("<label for=target>Last <span>Name</span></label><input id=target type=text>");

        await Expect(page.GetByLabel("last name")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByLabel("st na")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByLabel("Name")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByLabel("Last Name", new() { Exact = true })).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByLabel(new Regex(@"Last\s+name", RegexOptions.IgnoreCase))).ToHaveAttributeAsync("id", "target");

        Assert.That(await page.GetByLabel("Last", new() { Exact = true }).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await page.GetByLabel("last name", new() { Exact = true }).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await page.GetByLabel("Name", new() { Exact = true }).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await page.GetByLabel("what?").ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await page.GetByLabel(new Regex("last name")).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
    }

    [UpstreamTest("SE-079 getByLabel should work with multiply-labelled input")]
    public async Task GetByLabelShouldWorkWithMultiplyLabelledInput(IPage page)
    {
        await page.SetContentAsync("<label for=target>Name</label><input id=target type=text><label for=target>First or Last</label>");
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
        Assert.That(await page.GetByLabel("First or Last").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-080 getByLabel should work with ancestor label and multiple controls")]
    public async Task GetByLabelShouldWorkWithAncestorLabelAndMultipleControls(IPage page)
    {
        await page.SetContentAsync("<label>Name<button id=target>Click me</button><input type=text></label>");
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-081 getByLabel should work with ancestor label and for")]
    public async Task GetByLabelShouldWorkWithAncestorLabelAndFor(IPage page)
    {
        await page.SetContentAsync(@"
    <label for=target>Name<input type=text id=nontarget></label>
    <input type=text id=target>
  ");
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-082 getByLabel should work with aria-labelledby")]
    public async Task GetByLabelShouldWorkWithAriaLabelledby(IPage page)
    {
        await page.SetContentAsync("<label id=name-label>Name</label><button aria-labelledby=name-label>Click me</button>");
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("Click me"));
    }

    [UpstreamTest("SE-083 getByLabel should prioritize aria-labelledby over native label")]
    public async Task GetByLabelShouldPrioritizeAriaLabelledbyOverNativeLabel(IPage page)
    {
        await page.SetContentAsync(@"
    <label id=name-label>Name</label>
    <label>Wrong<button aria-labelledby=name-label>Click me</button></label>
  ");
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("Click me"));
    }

    [UpstreamTest("SE-084 getByLabel should work with aria-label")]
    public async Task GetByLabelShouldWorkWithAriaLabel(IPage page)
    {
        await page.SetContentAsync("<input id=target aria-label=\"Name\">");
        Assert.That(await page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-085 getByLabel should ignore empty aria-label")]
    public async Task GetByLabelShouldIgnoreEmptyAriaLabel(IPage page)
    {
        await page.SetContentAsync("<label for=target>Last Name</label><input id=target type=text aria-label>");
        Assert.That(await page.GetByLabel("Last Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-086 getByLabel should prioritize aria-labelledby over aria-label")]
    public async Task GetByLabelShouldPrioritizeAriaLabelledbyOverAriaLabel(IPage page)
    {
        await page.SetContentAsync("<label id=other-label>Other</label><input id=target aria-label=\"Name\" aria-labelledby=other-label>");
        Assert.That(await page.GetByLabel("Other").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-087 getByPlaceholder should work")]
    public async Task GetByPlaceholderShouldWork(IPage page)
    {
        await page.SetContentAsync(@"<div>
    <input placeholder='Hello'>
    <input placeholder='Hello World'>
  </div>");
        await Expect(page.GetByPlaceholder("hello")).ToHaveCountAsync(2);
        await Expect(page.GetByPlaceholder("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(page.GetByPlaceholder(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(page.MainFrame.GetByPlaceholder("hello")).ToHaveCountAsync(2);
        await Expect(page.Locator("div").GetByPlaceholder("hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-088 getByAltText should work")]
    public async Task GetByAltTextShouldWork(IPage page)
    {
        await page.SetContentAsync(@"<div>
    <input alt='Hello'>
    <input alt='Hello World'>
  </div>");
        await Expect(page.GetByAltText("hello")).ToHaveCountAsync(2);
        await Expect(page.GetByAltText("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(page.GetByAltText(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(page.MainFrame.GetByAltText("hello")).ToHaveCountAsync(2);
        await Expect(page.Locator("div").GetByAltText("hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-089 getByTitle should work")]
    public async Task GetByTitleShouldWork(IPage page)
    {
        await page.SetContentAsync(@"<div>
    <input title='Hello'>
    <input title='Hello World'>
  </div>");
        await Expect(page.GetByTitle("hello")).ToHaveCountAsync(2);
        await Expect(page.GetByTitle("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(page.GetByTitle(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(page.MainFrame.GetByTitle("hello")).ToHaveCountAsync(2);
        await Expect(page.Locator("div").GetByTitle("hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-090 getBy escaping")]
    public async Task GetByEscaping(IPage page)
    {
        await page.SetContentAsync(@"<label id=label for=control>Hello my
wo""rld</label><input id=control />");
        await page.EvalOnSelectorAsync("input", @"input => {
    input.setAttribute('placeholder', 'hello my\nwo""rld');
    input.setAttribute('title', 'hello my\nwo""rld');
    input.setAttribute('alt', 'hello my\nwo""rld');
  }");
        await Expect(page.GetByText("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "label");
        await Expect(page.GetByText("hello       my     wo\"rld")).ToHaveAttributeAsync("id", "label");
        await Expect(page.GetByLabel("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");
        await Expect(page.GetByPlaceholder("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");
        await Expect(page.GetByAltText("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");
        await Expect(page.GetByTitle("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");

        await page.SetContentAsync(@"<label id=label for=control>Hello my
world</label><input id=control />");
        await page.EvalOnSelectorAsync("input", @"input => {
    input.setAttribute('placeholder', 'hello my\nworld');
    input.setAttribute('title', 'hello my\nworld');
    input.setAttribute('alt', 'hello my\nworld');
  }");
        await Expect(page.GetByText("hello my\nworld")).ToHaveAttributeAsync("id", "label");
        await Expect(page.GetByText("hello        my    world")).ToHaveAttributeAsync("id", "label");
        await Expect(page.GetByLabel("hello my\nworld")).ToHaveAttributeAsync("id", "control");
        await Expect(page.GetByPlaceholder("hello my\nworld")).ToHaveAttributeAsync("id", "control");
        await Expect(page.GetByAltText("hello my\nworld")).ToHaveAttributeAsync("id", "control");
        await Expect(page.GetByTitle("hello my\nworld")).ToHaveAttributeAsync("id", "control");

        await page.SetContentAsync("<div id=target title=\"my title\">Text here</div>");
        await Expect(page.GetByTitle("my title", new() { Exact = true })).ToHaveCountAsync(1, new() { Timeout = 500 });
        // .NET: 'my t\itle' - C# has no \i escape, in JavaScript it is the letter i
        await Expect(page.GetByTitle("my title", new() { Exact = true })).ToHaveCountAsync(1, new() { Timeout = 500 });
        await Expect(page.GetByTitle("my t\\itle", new() { Exact = true })).ToHaveCountAsync(0, new() { Timeout = 500 });
        // .NET: 'my t\\\itle' - C# has no \i escape, in JavaScript it is the letter i
        await Expect(page.GetByTitle("my t\\itle", new() { Exact = true })).ToHaveCountAsync(0, new() { Timeout = 500 });
        await Expect(page.GetByTitle("my t\\\\itle", new() { Exact = true })).ToHaveCountAsync(0, new() { Timeout = 500 });

        await page.SetContentAsync("<label for=target>foo &gt;&gt; bar</label><input id=target>");
        await page.EvalOnSelectorAsync("input", @"input => {
    input.setAttribute('placeholder', 'foo >> bar');
    input.setAttribute('title', 'foo >> bar');
    input.setAttribute('alt', 'foo >> bar');
  }");
        Assert.That(await page.GetByText("foo >> bar").TextContentAsync(), Is.EqualTo("foo >> bar"));
        await Expect(page.Locator("label")).ToHaveTextAsync("foo >> bar");
        await Expect(page.GetByText("foo >> bar")).ToHaveTextAsync("foo >> bar");
        Assert.That(await page.GetByText(new Regex("foo >> bar")).TextContentAsync(), Is.EqualTo("foo >> bar"));
        await Expect(page.GetByLabel("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByLabel(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByPlaceholder("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByAltText("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByTitle("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByPlaceholder(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByAltText(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
        await Expect(page.GetByTitle(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
    }

    [UpstreamTest("SE-091 getByRole escaping")]
    public async Task GetByRoleEscaping(IPage page)
    {
        await page.SetContentAsync(@"
    <a href=""https://playwright.dev"">issues 123</a>
    <a href=""https://playwright.dev"">he llo 56</a>
    <button>Click me</button>
  ");
        Assert.That(await page.GetByRole(AriaRole.Button).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Click me</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Link).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">issues 123</a>",
            "<a href=\"https://playwright.dev\">he llo 56</a>",
        }));

        Assert.That(await page.GetByRole(AriaRole.Link, new() { Name = "issues 123" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">issues 123</a>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Link, new() { Name = "sues" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">issues 123</a>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Link, new() { Name = "  he    \n  llo " }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">he llo 56</a>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "issues" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));

        Assert.That(await page.GetByRole(AriaRole.Link, new() { Name = "sues", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await page.GetByRole(AriaRole.Link, new() { Name = "   he \n llo 56 ", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">he llo 56</a>",
        }));

        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Click me</button>",
        }));
        // .NET: 'Click \me' - C# has no \m escape, in JavaScript it is the letter m
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Click me</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Click \\me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
        // .NET: 'Click \\\me' - C# has no \m escape, in JavaScript it is the letter m
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Click \\me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Click \\\\me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
    }

    [UpstreamTest("SE-093 getByRole with description")]
    public async Task GetByRoleWithDescription(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=""alert"" aria-label=""Upload successful"" aria-description=""File doc-2025.pdf was uploaded successfully"">Alert 1</div>
    <div role=""alert"" aria-label=""Upload successful"" aria-description=""File report-2026.pdf was uploaded successfully"">Alert 2</div>
    <div role=""alert"" aria-label=""Invalid file"" aria-description=""File demo.doc has an invalid file format"">Alert 3</div>
  ");

        // description substring match (default)
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Description = "doc-2025" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
        }));
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Description = "report-2026" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 2",
        }));

        // description with name combined
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Name = "Upload successful", Description = "doc-2025" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
        }));
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Name = "Upload successful", Description = "report-2026" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 2",
        }));
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Name = "Invalid file", Description = "doc-2025" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(Array.Empty<string>()));

        // description exact match
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Description = "doc-2025", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Description = "File doc-2025.pdf was uploaded successfully", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
        }));

        // description regex match
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { DescriptionRegex = new Regex(@"report-\d+") }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 2",
        }));
        Assert.That(await page.GetByRole(AriaRole.Alert, new() { DescriptionRegex = new Regex("uploaded successfully$") }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
            "Alert 2",
        }));
    }

    [UpstreamTest("SE-094 getByRole with description via aria-describedby")]
    public async Task GetByRoleWithDescriptionViaAriaDescribedby(IPage page)
    {
        await page.SetContentAsync(@"
    <button aria-describedby=""desc1"">Submit</button>
    <span id=""desc1"">Submits the form data</span>
    <button aria-describedby=""desc2"">Submit</button>
    <span id=""desc2"">Saves as draft</span>
  ");

        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Submit", Description = "form data" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Submit",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Submit", Description = "draft" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Submit",
        }));
    }

    [UpstreamTest("SE-095 getByRole with description via title fallback")]
    public async Task GetByRoleWithDescriptionViaTitleFallback(IPage page)
    {
        await page.SetContentAsync(@"
    <button title=""Submits the form"">Submit</button>
    <button title=""Resets the form"">Reset</button>
  ");

        Assert.That(await page.GetByRole(AriaRole.Button, new() { Description = "Submits" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Submit",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Description = "Resets" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Reset",
        }));
    }

    [UpstreamTest("SE-096 getByRole with description whitespace normalization")]
    public async Task GetByRoleWithDescriptionWhitespaceNormalization(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=""alert"" aria-description=""File  doc-2025.pdf   was uploaded   successfully"">Alert</div>
  ");

        Assert.That(await page.GetByRole(AriaRole.Alert, new() { Description = "  doc-2025.pdf \n was  uploaded " }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert",
        }));
    }
}
