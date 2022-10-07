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


public class SelectorsRoleTests : PageTestEx
{
    [PlaywrightTest("selectors-role.spec.ts", "should detect roles")]
    public async Task ShouldDetectRoles()
    {
        await Page.SetContentAsync(@"
          <button>Hello</button>
          <select multiple="""" size=""2""></select>
          <select></select>
          <h3>Heading</h3>
          <details><summary>Hello</summary></details>
          <div role=""dialog"">I am a dialog</div>
        ");
        Assert.AreEqual(await Page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hello</button>" });
        Assert.AreEqual(await Page.Locator("role=listbox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<select multiple=\"\" size=\"2\"></select>" });
        Assert.AreEqual(await Page.Locator("role=combobox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<select></select>" });
        Assert.AreEqual(await Page.Locator("role=heading").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<h3>Heading</h3>" });
        Assert.AreEqual(await Page.Locator("role=group").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<details><summary>Hello</summary></details>" });
        Assert.AreEqual(await Page.Locator("role=dialog").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"dialog\">I am a dialog</div>" });
        Assert.AreEqual(await Page.Locator("role=menuitem").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Menuitem).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { });
    }

    [PlaywrightTest("selectors-role.spec.ts", "should support selected")]
    public async Task ShouldSupportSelected()
    {
        await Page.SetContentAsync(@"
        <select>
          <option>Hi</option>
          <option selected>Hello</option>
        </select>
        <div>
          <div role=""option"" aria-selected=""true"">Hi</div>
          <div role=""option"" aria-selected=""false"">Hello</div>
        </div>
        ");
        Assert.AreEqual(await Page.Locator("role=option[selected]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<option selected=\"\">Hello</option>", "<div role=\"option\" aria-selected=\"true\">Hi</div>" });
        Assert.AreEqual(await Page.Locator("role=option[selected=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<option selected=\"\">Hello</option>", "<div role=\"option\" aria-selected=\"true\">Hi</div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Option, new() { Selected = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<option selected=\"\">Hello</option>", "<div role=\"option\" aria-selected=\"true\">Hi</div>" });

        Assert.AreEqual(await Page.Locator("role=option[selected=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<option>Hi</option>", "<div role=\"option\" aria-selected=\"false\">Hello</div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Option, new() { Selected = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<option>Hi</option>", "<div role=\"option\" aria-selected=\"false\">Hello</div>" });
    }

    [PlaywrightTest("selectors-role.spec.ts", "should support checked")]
    public async Task ShouldSupportChecked()
    {
        await Page.SetContentAsync(@"
        <input type=checkbox>
        <input type=checkbox checked>
        <input type=checkbox indeterminate>
        <div role=checkbox aria-checked=""true"">Hi</div>
        <div role=checkbox aria-checked=""false"">Hello</div>
        <div role=checkbox>Unknown</div>
        ");
        await Page.EvalOnSelectorAsync("[indeterminate]", "e => e.indeterminate = true");

        Assert.AreEqual(await Page.Locator("role=checkbox[checked]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\" checked=\"\">", "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>" });
        Assert.AreEqual(await Page.Locator("role=checkbox[checked=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\" checked=\"\">", "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Checkbox, new() { Checked = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\" checked=\"\">", "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>" });

        Assert.AreEqual(await Page.Locator("role=checkbox[checked=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\">", "<div role=\"checkbox\" aria-checked=\"false\">Hello</div>", "<div role=\"checkbox\">Unknown</div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Checkbox, new() { Checked = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\">", "<div role=\"checkbox\" aria-checked=\"false\">Hello</div>", "<div role=\"checkbox\">Unknown</div>" });

        Assert.AreEqual(await Page.Locator("role=checkbox[checked=mixed]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\" indeterminate=\"\">" });
        Assert.AreEqual(await Page.Locator("role=checkbox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<input type=\"checkbox\">", "<input type=\"checkbox\" checked=\"\">", "<input type=\"checkbox\" indeterminate=\"\">", "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>", "<div role=\"checkbox\" aria-checked=\"false\">Hello</div>", "<div role=\"checkbox\">Unknown</div>" });
    }

    [PlaywrightTest("selectors-role.spec.ts", "should support pressed")]
    public async Task ShouldSupportPressed()
    {
        await Page.SetContentAsync(@"
        <button>Hi</button>
        <button aria-pressed=""true"">Hello</button>
        <button aria-pressed=""false"">Bye</button>
        <button aria-pressed=""mixed"">Mixed</button>
        ");
        Assert.AreEqual(await Page.Locator("role=button[pressed]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-pressed=\"true\">Hello</button>" });
        Assert.AreEqual(await Page.Locator("role=button[pressed=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-pressed=\"true\">Hello</button>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { Pressed = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-pressed=\"true\">Hello</button>" });
        Assert.AreEqual(await Page.Locator("role=button[pressed=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-pressed=\"false\">Bye</button>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { Pressed = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-pressed=\"false\">Bye</button>" });
        Assert.AreEqual(await Page.Locator("role=button[pressed=mixed]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-pressed=\"mixed\">Mixed</button>" });
        Assert.AreEqual(await Page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-pressed=\"true\">Hello</button>", "<button aria-pressed=\"false\">Bye</button>", "<button aria-pressed=\"mixed\">Mixed</button>" });
    }

    [PlaywrightTest("selectors-role.spec.ts", "should support expanded")]
    public async Task ShouldSupportExpanded()
    {
        await Page.SetContentAsync(@"
        <button>Hi</button>
        <button aria-expanded=""true"">Hello</button>
        <button aria-expanded=""false"">Bye</button>
        ");
        Assert.AreEqual(await Page.Locator("role=button[expanded]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-expanded=\"true\">Hello</button>" });
        Assert.AreEqual(await Page.Locator("role=button[expanded=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-expanded=\"true\">Hello</button>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { Expanded = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button aria-expanded=\"true\">Hello</button>" });
        Assert.AreEqual(await Page.Locator("role=button[expanded=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-expanded=\"false\">Bye</button>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { Expanded = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-expanded=\"false\">Bye</button>" });
    }

    [PlaywrightTest("selectors-role.spec.ts", "should support disabled")]
    public async Task ShouldSupportDisabled()
    {
        await Page.SetContentAsync(@"
        <button>Hi</button>
        <button disabled>Bye</button>
        <button aria-disabled=""true"">Hello</button>
        <button aria-disabled=""false"">Oh</button>
        <fieldset disabled>
          <button>Yay</button>
        </fieldset>
        ");
        Assert.AreEqual(await Page.Locator("role=button[disabled]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button disabled=\"\">Bye</button>", "<button aria-disabled=\"true\">Hello</button>", "<button>Yay</button>" });
        Assert.AreEqual(await Page.Locator("role=button[disabled=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button disabled=\"\">Bye</button>", "<button aria-disabled=\"true\">Hello</button>", "<button>Yay</button>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { Disabled = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button disabled=\"\">Bye</button>", "<button aria-disabled=\"true\">Hello</button>", "<button>Yay</button>" });
        Assert.AreEqual(await Page.Locator("role=button[disabled=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-disabled=\"false\">Oh</button>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { Disabled = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<button>Hi</button>", "<button aria-disabled=\"false\">Oh</button>" });
    }

    [PlaywrightTest("selectors-role.spec.ts", "should support level")]
    public async Task ShouldSupportLevel()
    {
        await Page.SetContentAsync(@"
        <h1>Hello</h1>
        <h3>Hi</h3>
        <div role=""heading"" aria-level=""5"">Bye</div>
        ");
        Assert.AreEqual(await Page.Locator("role=heading[level=1]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<h1>Hello</h1>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Heading, new() { Level = 1 }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<h1>Hello</h1>" });
        Assert.AreEqual(await Page.Locator("role=heading[level=3]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<h3>Hi</h3>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Heading, new() { Level = 3 }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<h3>Hi</h3>" });
        Assert.AreEqual(await Page.Locator("role=heading[level=5]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"heading\" aria-level=\"5\">Bye</div>" });
    }

    [PlaywrightTest("selectors.spec.ts", "should filter hidden, unless explicitly asked for")]
    public async Task ShouldFilterHiddenUnlessExplicitlyAskedFor()
    {
        await Page.SetContentAsync(@"
            <button>Hi</button>
            <button hidden>Hello</button>
            <button aria-hidden=""true"">Yay</button>
            <button aria-hidden=""false"">Nay</button>
            <button style=""visibility:hidden"">Bye</button>
            <div style=""visibility:hidden"">
            <button>Oh</button>
            </div>
            <div style=""visibility:hidden"">
            <button style=""visibility:visible"">Still here</button>
            </div>
            <button style=""display:none"">Never</button>
            <div id=host1></div>
            <div id=host2 style=""display:none""></div>
            <script>
            function addButton(host, text) {
                const root = host.attachShadow({ mode: 'open' });
                const button = document.createElement('button');
                button.textContent = text;
                root.appendChild(button);
            }
            addButton(document.getElementById('host1'), 'Shadow1');
            addButton(document.getElementById('host2'), 'Shadow2');
            </script>
        ");
        Assert.AreEqual(await Page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new[] {
            "<button>Hi</button>",
            "<button aria-hidden=\"false\">Nay</button>",
            "<button style=\"visibility:visible\">Still here</button>",
            "<button>Shadow1</button>",
        });
        Assert.AreEqual(await Page.Locator("role=button[include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new[] {
            "<button>Hi</button>",
            "<button hidden=\"\">Hello</button>",
            "<button aria-hidden=\"true\">Yay</button>",
            "<button aria-hidden=\"false\">Nay</button>",
            "<button style=\"visibility:hidden\">Bye</button>",
            "<button>Oh</button>",
            "<button style=\"visibility:visible\">Still here</button>",
            "<button style=\"display:none\">Never</button>",
            "<button>Shadow1</button>",
            "<button>Shadow2</button>",
        });
        Assert.AreEqual(await Page.Locator("role=button[include-hidden=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new[] {
            "<button>Hi</button>",
            "<button hidden=\"\">Hello</button>",
            "<button aria-hidden=\"true\">Yay</button>",
            "<button aria-hidden=\"false\">Nay</button>",
            "<button style=\"visibility:hidden\">Bye</button>",
            "<button>Oh</button>",
            "<button style=\"visibility:visible\">Still here</button>",
            "<button style=\"display:none\">Never</button>",
            "<button>Shadow1</button>",
            "<button>Shadow2</button>",
        });
        Assert.AreEqual(await Page.Locator("role=button[include-hidden=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new[] {
            "<button>Hi</button>",
            "<button aria-hidden=\"false\">Nay</button>",
            "<button style=\"visibility:visible\">Still here</button>",
            "<button>Shadow1</button>",
        });
    }

    [PlaywrightTest("selectors.spec.ts", "should support name")]
    public async Task ShouldSupportName()
    {
        await Page.SetContentAsync(@"
            <div role=""button"" aria-label=""Hello""></div>
            <div role=""button"" aria-label=""Hallo""></div>
            <div role=""button"" aria-label=""Hello"" aria-hidden=""true""></div>
            <div role=""button"" aria-label=""123"" aria-hidden=""true""></div>
            <div role=""button"" aria-label='foo""bar' aria-hidden=""true""></div>
        ");
        Assert.AreEqual(await Page.Locator("role=button[name=Hello]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { NameString = "Hello" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>" });

        Assert.AreEqual(await Page.Locator("role=button[name*=all]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hallo\"></div>" });

        Assert.AreEqual(await Page.Locator("role=button[name=/^H[ae]llo$/]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hallo\"></div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^H[ae]llo$") }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hallo\"></div>" });

        Assert.AreEqual(await Page.Locator("role=button[name=/h.*o/i]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hallo\"></div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("h.*o", RegexOptions.IgnoreCase) }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hallo\"></div>" });

        Assert.AreEqual(await Page.Locator("role=button[name=Hello][include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hello\" aria-hidden=\"true\"></div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { NameString = "Hello", IncludeHidden = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hello\" aria-hidden=\"true\"></div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { NameString = "hello", IncludeHidden = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>", "<div role=\"button\" aria-label=\"Hello\" aria-hidden=\"true\"></div>" });

        Assert.AreEqual(await Page.Locator("role=button[name=Hello]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"Hello\"></div>" });
        Assert.AreEqual(await Page.Locator("role=button[name=123][include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"123\" aria-hidden=\"true\"></div>" });
        Assert.AreEqual(await Page.GetByRole(AriaRole.Button, new() { NameString = "123", IncludeHidden = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), new string[] { "<div role=\"button\" aria-label=\"123\" aria-hidden=\"true\"></div>" });
    }


    [PlaywrightTest("selectors.spec.ts", "errors")]
    public async Task Errors()
    {
        var exception0 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=[bar]"));
        StringAssert.Contains("Role must not be empty", exception0.Message);

        var exception1 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=foo[sElected]"));
        StringAssert.Contains("Unknown attribute \"sElected\", must be one of \"checked\", \"disabled\", \"expanded\", \"include-hidden\", \"level\", \"name\", \"pressed\", \"selected\"", exception1.Message);

        var exception2 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=foo[bar . qux=true]"));
        StringAssert.Contains("Unknown attribute \"bar.qux\"", exception2.Message);

        var exception3 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=heading[level=\"bar\"]"));
        StringAssert.Contains("\"level\" attribute must be compared to a number", exception3.Message);

        var exception4 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=checkbox[checked=\"bar\"]"));
        StringAssert.Contains("\"checked\" must be one of true, false, \"mixed\"", exception4.Message);

        var exception5 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=checkbox[checked~=true]"));
        StringAssert.Contains("cannot use ~= in attribute with non-string matching value", exception5.Message);

        var exception6 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=button[level=3]"));
        StringAssert.Contains("\"level\" attribute is only supported for roles: \"heading\", \"listitem\", \"row\", \"treeitem\"", exception6.Message);

        var exception7 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.QuerySelectorAsync("role=button[name]"));
        StringAssert.Contains("\"name\" attribute must have a value", exception7.Message);
    }
}
