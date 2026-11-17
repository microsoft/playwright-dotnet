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
public class SelectorsGetByTests : UpstreamPageTest
{
    [UpstreamTest("SE-071 getByTestId should work")]
    public async Task GetByTestIdShouldWork()
    {
        await Page.SetContentAsync("<div><div data-testid=\"Hello\">Hello world</div></div>");
        await Expect(Page.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-072 getByTestId with custom testId should work")]
    public async Task GetByTestIdWithCustomTestIdShouldWork()
    {
        await Page.SetContentAsync("<div><div data-my-custom-testid=\"Hello\">Hello world</div></div>");
        Playwright.Selectors.SetTestIdAttribute("data-my-custom-testid");
        await Expect(Page.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-073 getByTestId with comma-separated testIdAttributes should match any")]
    public async Task GetByTestIdWithCommaSeparatedTestIdAttributesShouldMatchAny()
    {
        await Page.SetContentAsync(@"
    <section>
      <div data-pw=""Hello"">first</div>
      <div data-ti=""Hello"">second</div>
      <div data-testid=""Hello"">third</div>
    </section>
  ");
        Playwright.Selectors.SetTestIdAttribute("data-pw,data-ti");
        await Expect(Page.GetByTestId("Hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByTestId("Hello")).ToHaveTextAsync(new[] { "first", "second" });
        await Expect(Page.MainFrame.GetByTestId("Hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("section").GetByTestId("Hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-074 getByTestId should escape id")]
    public async Task GetByTestIdShouldEscapeId()
    {
        await Page.SetContentAsync("<div><div data-testid='He\"llo'>Hello world</div></div>");
        await Expect(Page.GetByTestId("He\"llo")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-075 getByTestId should work for regex")]
    public async Task GetByTestIdShouldWorkForRegex()
    {
        await Page.SetContentAsync("<div><div data-testid=\"Hello\">Hello world</div></div>");
        await Expect(Page.GetByTestId(new Regex("He[l]*o"))).ToHaveTextAsync("Hello world");
        await Expect(Page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [UpstreamTest("SE-076 getByText should work")]
    public async Task GetByTextShouldWork()
    {
        await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        Assert.That(await Page.GetByText("ye").EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));
        Assert.That(await Page.GetByText(new Regex("ye")).EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));
        Assert.That(await Page.GetByText(new Regex("e")).EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">\nye  </div>"));

        await Page.SetContentAsync("<div> ye </div><div>ye</div>");
        Assert.That(await Page.GetByText("ye", new() { Exact = true }).First.EvaluateAsync<string>("e => e.outerHTML"), Does.Contain("> ye </div>"));

        await Page.SetContentAsync("<div>Hello world</div><div>Hello</div>");
        Assert.That(await Page.GetByText("Hello", new() { Exact = true }).EvaluateAsync<string>("e => e.outerHTML"), Does.Contain(">Hello</div>"));
    }

    [UpstreamTest("SE-077 getByLabel should work")]
    public async Task GetByLabelShouldWork()
    {
        await Page.SetContentAsync("<div><label for=target>Name</label><input id=target type=text></div>");
        Assert.That(await Page.GetByText("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("LABEL"));
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("INPUT"));
        Assert.That(await Page.MainFrame.GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("INPUT"));
        Assert.That(await Page.Locator("div").GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), Is.EqualTo("INPUT"));

        await Page.SetContentAsync(@"
    <label for=""city"">City</label><my-field id=""city"" role=""combobox"">Pick one</my-field>
    <label>Street <my-field id=""street"" role=""combobox"">Pick one</my-field></label>
  ");
        await Page.EvaluateAsync(@"() => {
    customElements.define('my-field', class extends HTMLElement {
      static formAssociated = true;
      constructor() {
        super();
        this.attachInternals();
      }
    });
  }");
        Assert.That(await Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('label')].map(label => label.control?.id)"), Is.EqualTo(new[] { "city", "street" }));
        Assert.That(await Page.GetByLabel("City").EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "city" }));
        Assert.That(await Page.GetByLabel("Street").EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "street" }));
        Assert.That(await Page.GetByRole(AriaRole.Combobox, new() { Name = "City", Exact = true }).EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "city" }));
        Assert.That(await Page.GetByRole(AriaRole.Combobox, new() { Name = "Street", Exact = true }).EvaluateAllAsync<string[]>("elements => elements.map(e => e.id)"), Is.EqualTo(new[] { "street" }));
    }

    [UpstreamTest("SE-078 getByLabel should work with nested elements")]
    public async Task GetByLabelShouldWorkWithNestedElements()
    {
        await Page.SetContentAsync("<label for=target>Last <span>Name</span></label><input id=target type=text>");

        await Expect(Page.GetByLabel("last name")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel("st na")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel("Name")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel("Last Name", new() { Exact = true })).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel(new Regex(@"Last\s+name", RegexOptions.IgnoreCase))).ToHaveAttributeAsync("id", "target");

        Assert.That(await Page.GetByLabel("Last", new() { Exact = true }).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await Page.GetByLabel("last name", new() { Exact = true }).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await Page.GetByLabel("Name", new() { Exact = true }).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await Page.GetByLabel("what?").ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
        Assert.That(await Page.GetByLabel(new Regex("last name")).ElementHandlesAsync(), Is.EqualTo(Array.Empty<IElementHandle>()));
    }

    [UpstreamTest("SE-079 getByLabel should work with multiply-labelled input")]
    public async Task GetByLabelShouldWorkWithMultiplyLabelledInput()
    {
        await Page.SetContentAsync("<label for=target>Name</label><input id=target type=text><label for=target>First or Last</label>");
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
        Assert.That(await Page.GetByLabel("First or Last").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-080 getByLabel should work with ancestor label and multiple controls")]
    public async Task GetByLabelShouldWorkWithAncestorLabelAndMultipleControls()
    {
        await Page.SetContentAsync("<label>Name<button id=target>Click me</button><input type=text></label>");
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-081 getByLabel should work with ancestor label and for")]
    public async Task GetByLabelShouldWorkWithAncestorLabelAndFor()
    {
        await Page.SetContentAsync(@"
    <label for=target>Name<input type=text id=nontarget></label>
    <input type=text id=target>
  ");
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-082 getByLabel should work with aria-labelledby")]
    public async Task GetByLabelShouldWorkWithAriaLabelledby()
    {
        await Page.SetContentAsync("<label id=name-label>Name</label><button aria-labelledby=name-label>Click me</button>");
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("Click me"));
    }

    [UpstreamTest("SE-083 getByLabel should prioritize aria-labelledby over native label")]
    public async Task GetByLabelShouldPrioritizeAriaLabelledbyOverNativeLabel()
    {
        await Page.SetContentAsync(@"
    <label id=name-label>Name</label>
    <label>Wrong<button aria-labelledby=name-label>Click me</button></label>
  ");
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("Click me"));
    }

    [UpstreamTest("SE-084 getByLabel should work with aria-label")]
    public async Task GetByLabelShouldWorkWithAriaLabel()
    {
        await Page.SetContentAsync("<input id=target aria-label=\"Name\">");
        Assert.That(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-085 getByLabel should ignore empty aria-label")]
    public async Task GetByLabelShouldIgnoreEmptyAriaLabel()
    {
        await Page.SetContentAsync("<label for=target>Last Name</label><input id=target type=text aria-label>");
        Assert.That(await Page.GetByLabel("Last Name").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-086 getByLabel should prioritize aria-labelledby over aria-label")]
    public async Task GetByLabelShouldPrioritizeAriaLabelledbyOverAriaLabel()
    {
        await Page.SetContentAsync("<label id=other-label>Other</label><input id=target aria-label=\"Name\" aria-labelledby=other-label>");
        Assert.That(await Page.GetByLabel("Other").EvaluateAsync<string>("e => e.id"), Is.EqualTo("target"));
    }

    [UpstreamTest("SE-087 getByPlaceholder should work")]
    public async Task GetByPlaceholderShouldWork()
    {
        await Page.SetContentAsync(@"<div>
    <input placeholder='Hello'>
    <input placeholder='Hello World'>
  </div>");
        await Expect(Page.GetByPlaceholder("hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByPlaceholder("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByPlaceholder(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(Page.MainFrame.GetByPlaceholder("hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").GetByPlaceholder("hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-088 getByAltText should work")]
    public async Task GetByAltTextShouldWork()
    {
        await Page.SetContentAsync(@"<div>
    <input alt='Hello'>
    <input alt='Hello World'>
  </div>");
        await Expect(Page.GetByAltText("hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByAltText("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByAltText(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(Page.MainFrame.GetByAltText("hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").GetByAltText("hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-089 getByTitle should work")]
    public async Task GetByTitleShouldWork()
    {
        await Page.SetContentAsync(@"<div>
    <input title='Hello'>
    <input title='Hello World'>
  </div>");
        await Expect(Page.GetByTitle("hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByTitle("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByTitle(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(Page.MainFrame.GetByTitle("hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").GetByTitle("hello")).ToHaveCountAsync(2);
    }

    [UpstreamTest("SE-090 getBy escaping")]
    public async Task GetByEscaping()
    {
        await Page.SetContentAsync(@"<label id=label for=control>Hello my
wo""rld</label><input id=control />");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.setAttribute('placeholder', 'hello my\nwo""rld');
    input.setAttribute('title', 'hello my\nwo""rld');
    input.setAttribute('alt', 'hello my\nwo""rld');
  }");
        await Expect(Page.GetByText("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "label");
        await Expect(Page.GetByText("hello       my     wo\"rld")).ToHaveAttributeAsync("id", "label");
        await Expect(Page.GetByLabel("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");
        await Expect(Page.GetByPlaceholder("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");
        await Expect(Page.GetByAltText("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");
        await Expect(Page.GetByTitle("hello my\nwo\"rld")).ToHaveAttributeAsync("id", "control");

        await Page.SetContentAsync(@"<label id=label for=control>Hello my
world</label><input id=control />");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.setAttribute('placeholder', 'hello my\nworld');
    input.setAttribute('title', 'hello my\nworld');
    input.setAttribute('alt', 'hello my\nworld');
  }");
        await Expect(Page.GetByText("hello my\nworld")).ToHaveAttributeAsync("id", "label");
        await Expect(Page.GetByText("hello        my    world")).ToHaveAttributeAsync("id", "label");
        await Expect(Page.GetByLabel("hello my\nworld")).ToHaveAttributeAsync("id", "control");
        await Expect(Page.GetByPlaceholder("hello my\nworld")).ToHaveAttributeAsync("id", "control");
        await Expect(Page.GetByAltText("hello my\nworld")).ToHaveAttributeAsync("id", "control");
        await Expect(Page.GetByTitle("hello my\nworld")).ToHaveAttributeAsync("id", "control");

        await Page.SetContentAsync("<div id=target title=\"my title\">Text here</div>");
        await Expect(Page.GetByTitle("my title", new() { Exact = true })).ToHaveCountAsync(1, new() { Timeout = 500 });
        // .NET: 'my t\itle' - C# has no \i escape, in JavaScript it is the letter i
        await Expect(Page.GetByTitle("my title", new() { Exact = true })).ToHaveCountAsync(1, new() { Timeout = 500 });
        await Expect(Page.GetByTitle("my t\\itle", new() { Exact = true })).ToHaveCountAsync(0, new() { Timeout = 500 });
        // .NET: 'my t\\\itle' - C# has no \i escape, in JavaScript it is the letter i
        await Expect(Page.GetByTitle("my t\\itle", new() { Exact = true })).ToHaveCountAsync(0, new() { Timeout = 500 });
        await Expect(Page.GetByTitle("my t\\\\itle", new() { Exact = true })).ToHaveCountAsync(0, new() { Timeout = 500 });

        await Page.SetContentAsync("<label for=target>foo &gt;&gt; bar</label><input id=target>");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.setAttribute('placeholder', 'foo >> bar');
    input.setAttribute('title', 'foo >> bar');
    input.setAttribute('alt', 'foo >> bar');
  }");
        Assert.That(await Page.GetByText("foo >> bar").TextContentAsync(), Is.EqualTo("foo >> bar"));
        await Expect(Page.Locator("label")).ToHaveTextAsync("foo >> bar");
        await Expect(Page.GetByText("foo >> bar")).ToHaveTextAsync("foo >> bar");
        Assert.That(await Page.GetByText(new Regex("foo >> bar")).TextContentAsync(), Is.EqualTo("foo >> bar"));
        await Expect(Page.GetByLabel("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByPlaceholder("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByAltText("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByTitle("foo >> bar")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByPlaceholder(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByAltText(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByTitle(new Regex("foo >> bar"))).ToHaveAttributeAsync("id", "target");
    }

    [UpstreamTest("SE-091 getByRole escaping")]
    public async Task GetByRoleEscaping()
    {
        await Page.SetContentAsync(@"
    <a href=""https://playwright.dev"">issues 123</a>
    <a href=""https://playwright.dev"">he llo 56</a>
    <button>Click me</button>
  ");
        Assert.That(await Page.GetByRole(AriaRole.Button).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Click me</button>",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Link).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">issues 123</a>",
            "<a href=\"https://playwright.dev\">he llo 56</a>",
        }));

        Assert.That(await Page.GetByRole(AriaRole.Link, new() { Name = "issues 123" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">issues 123</a>",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Link, new() { Name = "sues" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">issues 123</a>",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Link, new() { Name = "  he    \n  llo " }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">he llo 56</a>",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "issues" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));

        Assert.That(await Page.GetByRole(AriaRole.Link, new() { Name = "sues", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await Page.GetByRole(AriaRole.Link, new() { Name = "   he \n llo 56 ", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<a href=\"https://playwright.dev\">he llo 56</a>",
        }));

        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Click me</button>",
        }));
        // .NET: 'Click \me' - C# has no \m escape, in JavaScript it is the letter m
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Click me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Click me</button>",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Click \\me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
        // .NET: 'Click \\\me' - C# has no \m escape, in JavaScript it is the letter m
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Click \\me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Click \\\\me", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(Array.Empty<string>()));
    }

    [UpstreamTest("SE-093 getByRole with description")]
    public async Task GetByRoleWithDescription()
    {
        await Page.SetContentAsync(@"
    <div role=""alert"" aria-label=""Upload successful"" aria-description=""File doc-2025.pdf was uploaded successfully"">Alert 1</div>
    <div role=""alert"" aria-label=""Upload successful"" aria-description=""File report-2026.pdf was uploaded successfully"">Alert 2</div>
    <div role=""alert"" aria-label=""Invalid file"" aria-description=""File demo.doc has an invalid file format"">Alert 3</div>
  ");

        // description substring match (default)
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Description = "doc-2025" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Description = "report-2026" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 2",
        }));

        // description with name combined
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Name = "Upload successful", Description = "doc-2025" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Name = "Upload successful", Description = "report-2026" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 2",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Name = "Invalid file", Description = "doc-2025" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(Array.Empty<string>()));

        // description exact match
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Description = "doc-2025", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Description = "File doc-2025.pdf was uploaded successfully", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
        }));

        // description regex match
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { DescriptionRegex = new Regex(@"report-\d+") }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 2",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { DescriptionRegex = new Regex("uploaded successfully$") }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert 1",
            "Alert 2",
        }));
    }

    [UpstreamTest("SE-094 getByRole with description via aria-describedby")]
    public async Task GetByRoleWithDescriptionViaAriaDescribedby()
    {
        await Page.SetContentAsync(@"
    <button aria-describedby=""desc1"">Submit</button>
    <span id=""desc1"">Submits the form data</span>
    <button aria-describedby=""desc2"">Submit</button>
    <span id=""desc2"">Saves as draft</span>
  ");

        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Submit", Description = "form data" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Submit",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Name = "Submit", Description = "draft" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Submit",
        }));
    }

    [UpstreamTest("SE-095 getByRole with description via title fallback")]
    public async Task GetByRoleWithDescriptionViaTitleFallback()
    {
        await Page.SetContentAsync(@"
    <button title=""Submits the form"">Submit</button>
    <button title=""Resets the form"">Reset</button>
  ");

        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Description = "Submits" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Submit",
        }));
        Assert.That(await Page.GetByRole(AriaRole.Button, new() { Description = "Resets" }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Reset",
        }));
    }

    [UpstreamTest("SE-096 getByRole with description whitespace normalization")]
    public async Task GetByRoleWithDescriptionWhitespaceNormalization()
    {
        await Page.SetContentAsync(@"
    <div role=""alert"" aria-description=""File  doc-2025.pdf   was uploaded   successfully"">Alert</div>
  ");

        Assert.That(await Page.GetByRole(AriaRole.Alert, new() { Description = "  doc-2025.pdf \n was  uploaded " }).EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[]
        {
            "Alert",
        }));
    }
}
