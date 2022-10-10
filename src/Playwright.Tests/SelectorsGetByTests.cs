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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

///<playwright-file>selectors-register.spec.ts</playwright-file>
public class SelectorsGetByTests : PageTestEx
{
    [PlaywrightTest("selector-get-by.spec.ts", "getByTestId should work")]
    public async Task GetByTestIdShouldWork()
    {
        await Page.SetContentAsync("<div><div data-testid=\"Hello\">Hello world</div></div>");
        await Expect(Page.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByTestId with custom testId should work")]
    public async Task GetByTestIdWithCustomTestIdShouldWork()
    {
        await Page.SetContentAsync("<div><div data-my-custom-testid=\"Hello\">Hello world</div></div>");
        Playwright.Selectors.SetTestIdAttribute("data-my-custom-testid");
        await Expect(Page.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.MainFrame.GetByTestId("Hello")).ToHaveTextAsync("Hello world");
        await Expect(Page.Locator("div").GetByTestId("Hello")).ToHaveTextAsync("Hello world");
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByTestId should escape id")]
    public async Task GetByTestIdShouldEscapeId()
    {
        await Page.SetContentAsync("<div><div data-testid='He\"llo'>Hello world</div></div>");
        await Expect(Page.GetByTestId("He\"llo")).ToHaveTextAsync("Hello world");
        await Expect(Page.MainFrame.GetByTestId("He\"llo")).ToHaveTextAsync("Hello world");
        await Expect(Page.Locator("div").GetByTestId("He\"llo")).ToHaveTextAsync("Hello world");
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByText should work")]
    public async Task GetByTextShouldWork()
    {
        await Page.SetContentAsync("<div>yo</div><div>ya</div><div>\nye  </div>");
        StringAssert.Contains(">\nye  </div>", await Page.GetByText("ye").EvaluateAsync<string>("e => e.outerHTML"));
        StringAssert.Contains(">\nye  </div>", await Page.GetByText(new Regex("ye")).EvaluateAsync<string>("e => e.outerHTML"));
        StringAssert.Contains(">\nye  </div>", await Page.GetByText(new Regex("e")).EvaluateAsync<string>("e => e.outerHTML"));

        await Page.SetContentAsync("<div> ye </div><div>ye</div>");
        StringAssert.Contains("> ye </div>", await Page.GetByText("ye", new() { Exact = true }).First.EvaluateAsync<string>("e => e.outerHTML"));

        await Page.SetContentAsync("<div>Hello world</div><div>Hello</div>");
        StringAssert.Contains(">Hello</div>", await Page.GetByText("Hello", new() { Exact = true }).EvaluateAsync<string>("e => e.outerHTML"));
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByLabel should work")]
    public async Task GetByLabelShouldWork()
    {
        await Page.SetContentAsync("<div><label for=target>Name</label><input id=target type=text></div>");
        Assert.AreEqual(await Page.GetByText("Name").EvaluateAsync<string>("e => e.nodeName"), "LABEL");
        Assert.AreEqual(await Page.GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), "INPUT");
        Assert.AreEqual(await Page.MainFrame.GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), "INPUT");
        Assert.AreEqual(await Page.Locator("div").GetByLabel("Name").EvaluateAsync<string>("e => e.nodeName"), "INPUT");
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByLabel should work with nested elements")]
    public async Task GetByLabelShouldworkWithNestedElements()
    {
        await Page.SetContentAsync("<label for=target>Last <span>Name</span></label><input id=target type=text>");

        await Expect(Page.GetByLabel("last name")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel("st na")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel("Name")).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel("Last Name", new() { Exact = true })).ToHaveAttributeAsync("id", "target");
        await Expect(Page.GetByLabel(new Regex(@"Last\s+name", RegexOptions.IgnoreCase))).ToHaveAttributeAsync("id", "target");

        Assert.AreEqual((await Page.GetByLabel("Last", new() { Exact = true }).ElementHandlesAsync()).Count, 0);
        Assert.AreEqual((await Page.GetByLabel("last name", new() { Exact = true }).ElementHandlesAsync()).Count, 0);
        Assert.AreEqual((await Page.GetByLabel("Name", new() { Exact = true }).ElementHandlesAsync()).Count, 0);
        Assert.AreEqual((await Page.GetByLabel("what?", new() { Exact = true }).ElementHandlesAsync()).Count, 0);
        Assert.AreEqual((await Page.GetByLabel(new Regex(@"last name"), new() { Exact = true }).ElementHandlesAsync()).Count, 0);

    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByPlaceholder should work")]
    public async Task GetByPlaceholderShouldWork()
    {
        await Page.SetContentAsync("<div>\n    <input placeholder='Hello'>\n    <input placeholder='Hello World'>\n  </div>");
        await Expect(Page.GetByPlaceholder("hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByPlaceholder("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByPlaceholder(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(Page.MainFrame.GetByPlaceholder("hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").GetByPlaceholder("hello")).ToHaveCountAsync(2);
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByAltText should work")]
    public async Task GetByAltTextShouldWork()
    {
        await Page.SetContentAsync("<div>\n    <input alt='Hello'>\n    <input alt='Hello World'>\n  </div>");
        await Expect(Page.GetByAltText("hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByAltText("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByAltText(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(Page.MainFrame.GetByAltText("hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").GetByAltText("hello")).ToHaveCountAsync(2);
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getByTitle should work")]
    public async Task GetByTitleShouldWork()
    {
        await Page.SetContentAsync("<div>\n    <input title='Hello'>\n    <input title='Hello World'>\n  </div>");
        await Expect(Page.GetByTitle("hello")).ToHaveCountAsync(2);
        await Expect(Page.GetByTitle("Hello", new() { Exact = true })).ToHaveCountAsync(1);
        await Expect(Page.GetByTitle(new Regex("wor", RegexOptions.IgnoreCase))).ToHaveCountAsync(1);

        // Coverage
        await Expect(Page.MainFrame.GetByTitle("hello")).ToHaveCountAsync(2);
        await Expect(Page.Locator("div").GetByTitle("hello")).ToHaveCountAsync(2);
    }

    [PlaywrightTest("selector-get-by.spec.ts", "getBy escaping")]
    public async Task GetByEscapingShouldWork()
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
    }
}
