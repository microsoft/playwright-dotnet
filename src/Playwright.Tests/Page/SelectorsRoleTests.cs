/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/selectors-role.spec.ts.</summary>
public class SelectorsRoleTests
{
    [UpstreamTest("SE-050 should detect roles")]
    public async Task ShouldDetectRoles(IPage page)
    {
        await page.SetContentAsync(@"
    <button>Hello</button>
    <select multiple="""" size=""2""></select>
    <select></select>
    <h3>Heading</h3>
    <details><summary>Hello</summary></details>
    <div role=""dialog"">I am a dialog</div>
  ");
        Assert.That(await page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hello</button>",
        }));
        Assert.That(await page.Locator("role=listbox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<select multiple=\"\" size=\"2\"></select>",
        }));
        Assert.That(await page.Locator("role=combobox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<select></select>",
        }));
        Assert.That(await page.Locator("role=heading").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<h3>Heading</h3>",
        }));
        Assert.That(await page.Locator("role=group").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<details><summary>Hello</summary></details>",
        }));
        Assert.That(await page.Locator("role=dialog").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"dialog\">I am a dialog</div>",
        }));
        Assert.That(await page.Locator("role=menuitem").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new string[]
        {
        }));
        Assert.That(await page.GetByRole(AriaRole.Menuitem).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new string[]
        {
        }));
    }

    [UpstreamTest("SE-051 should support selected")]
    public async Task ShouldSupportSelected(IPage page)
    {
        await page.SetContentAsync(@"
    <select>
      <option>Hi</option>
      <option selected>Hello</option>
    </select>
    <div>
      <div role=""option"" aria-selected=""true"">Hi</div>
      <div role=""option"" aria-selected=""false"">Hello</div>
    </div>
  ");
        Assert.That(await page.Locator("role=option[selected]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option selected=\"\">Hello</option>",
            "<div role=\"option\" aria-selected=\"true\">Hi</div>",
        }));

        Assert.That(await page.Locator("role=option[selected=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option selected=\"\">Hello</option>",
            "<div role=\"option\" aria-selected=\"true\">Hi</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Option, new() { Selected = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option selected=\"\">Hello</option>",
            "<div role=\"option\" aria-selected=\"true\">Hi</div>",
        }));

        Assert.That(await page.Locator("role=option[selected=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option>Hi</option>",
            "<div role=\"option\" aria-selected=\"false\">Hello</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Option, new() { Selected = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option>Hi</option>",
            "<div role=\"option\" aria-selected=\"false\">Hello</div>",
        }));
    }

    [UpstreamTest("SE-052 should support checked")]
    public async Task ShouldSupportChecked(IPage page)
    {
        await page.SetContentAsync(@"
    <input type=checkbox>
    <input type=checkbox checked>
    <input type=checkbox indeterminate>
    <div role=checkbox aria-checked=""true"">Hi</div>
    <div role=checkbox aria-checked=""false"">Hello</div>
    <div role=checkbox>Unknown</div>
  ");
        await page.EvalOnSelectorAsync("[indeterminate]", "input => input.indeterminate = true");

        Assert.That(await page.Locator("role=checkbox[checked]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\" checked=\"\">",
            "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>",
        }));
        Assert.That(await page.Locator("role=checkbox[checked=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\" checked=\"\">",
            "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Checkbox, new() { Checked = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\" checked=\"\">",
            "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>",
        }));

        Assert.That(await page.Locator("role=checkbox[checked=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\">",
            "<div role=\"checkbox\" aria-checked=\"false\">Hello</div>",
            "<div role=\"checkbox\">Unknown</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Checkbox, new() { Checked = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\">",
            "<div role=\"checkbox\" aria-checked=\"false\">Hello</div>",
            "<div role=\"checkbox\">Unknown</div>",
        }));

        Assert.That(await page.Locator("role=checkbox[checked=\"mixed\"]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\" indeterminate=\"\">",
        }));
        Assert.That(await page.Locator("role=checkbox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input type=\"checkbox\">",
            "<input type=\"checkbox\" checked=\"\">",
            "<input type=\"checkbox\" indeterminate=\"\">",
            "<div role=\"checkbox\" aria-checked=\"true\">Hi</div>",
            "<div role=\"checkbox\" aria-checked=\"false\">Hello</div>",
            "<div role=\"checkbox\">Unknown</div>",
        }));

        await page.SetContentAsync(@"
    <div role=""checkbox"" aria-checked=""True"" aria-readonly=""True""
      onclick=""this.setAttribute('aria-checked', String(this.getAttribute('aria-checked').toLowerCase() !== 'true'))"">Remember me</div>
    <button aria-pressed=""True"">Bold</button>
    <button aria-expanded=""True"">Menu</button>
    <div role=""Button"" tabindex=""0"">Save</div>
  ");
        var checkbox = page.GetByRole(AriaRole.Checkbox);
        Assert.That(await page.GetByRole(AriaRole.Checkbox, new() { Checked = true }).CountAsync(), Is.EqualTo(1));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Pressed = true }).CountAsync(), Is.EqualTo(1));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Expanded = true }).CountAsync(), Is.EqualTo(1));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).CountAsync(), Is.EqualTo(1));
        await Expect(checkbox).ToBeCheckedAsync();
        await Expect(checkbox).Not.ToBeEditableAsync();
        await Expect(page.Locator("body")).ToMatchAriaSnapshotAsync(@"
    - checkbox ""Remember me"" [checked]
    - button ""Bold"" [pressed]
    - button ""Menu"" [expanded]
    - button ""Save""
  ");
        await checkbox.CheckAsync();
        await Expect(checkbox).ToHaveAttributeAsync("aria-checked", "True");

        await checkbox.EvaluateAsync("element => element.setAttribute('aria-checked', 'Mixed')");
        await page.GetByRole(AriaRole.Button, new() { Name = "Bold" }).EvaluateAsync("element => element.setAttribute('aria-pressed', 'Mixed')");
        Assert.That(await page.Locator("role=checkbox[checked=mixed]").CountAsync(), Is.EqualTo(1));
        Assert.That(await page.Locator("role=button[pressed=mixed]").CountAsync(), Is.EqualTo(1));
    }

    [UpstreamTest("SE-053 should support pressed")]
    public async Task ShouldSupportPressed(IPage page)
    {
        await page.SetContentAsync(@"
    <button>Hi</button>
    <button aria-pressed=""true"">Hello</button>
    <button aria-pressed=""false"">Bye</button>
    <button aria-pressed=""mixed"">Mixed</button>
  ");
        Assert.That(await page.Locator("role=button[pressed]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button aria-pressed=\"true\">Hello</button>",
        }));
        Assert.That(await page.Locator("role=button[pressed=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button aria-pressed=\"true\">Hello</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Pressed = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button aria-pressed=\"true\">Hello</button>",
        }));
        Assert.That(await page.Locator("role=button[pressed=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-pressed=\"false\">Bye</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Pressed = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-pressed=\"false\">Bye</button>",
        }));
        Assert.That(await page.Locator("role=button[pressed=\"mixed\"]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button aria-pressed=\"mixed\">Mixed</button>",
        }));
        Assert.That(await page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-pressed=\"true\">Hello</button>",
            "<button aria-pressed=\"false\">Bye</button>",
            "<button aria-pressed=\"mixed\">Mixed</button>",
        }));
    }

    [UpstreamTest("SE-054 should support expanded")]
    public async Task ShouldSupportExpanded(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=""treeitem"">Hi</div>
    <div role=""treeitem"" aria-expanded=""true"">Hello</div>
    <div role=""treeitem"" aria-expanded=""false"">Bye</div>
  ");

        Assert.That(await page.Locator("role=treeitem").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\">Hi</div>",
            "<div role=\"treeitem\" aria-expanded=\"true\">Hello</div>",
            "<div role=\"treeitem\" aria-expanded=\"false\">Bye</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Treeitem).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\">Hi</div>",
            "<div role=\"treeitem\" aria-expanded=\"true\">Hello</div>",
            "<div role=\"treeitem\" aria-expanded=\"false\">Bye</div>",
        }));

        Assert.That(await page.Locator("role=treeitem[expanded]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\" aria-expanded=\"true\">Hello</div>",
        }));
        Assert.That(await page.Locator("role=treeitem[expanded=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\" aria-expanded=\"true\">Hello</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Treeitem, new() { Expanded = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\" aria-expanded=\"true\">Hello</div>",
        }));

        Assert.That(await page.Locator("role=treeitem[expanded=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\" aria-expanded=\"false\">Bye</div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Treeitem, new() { Expanded = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\" aria-expanded=\"false\">Bye</div>",
        }));

        // Workaround for expanded="none".
        Assert.That(await page.Locator("[role=treeitem]:not([aria-expanded])").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"treeitem\">Hi</div>",
        }));
    }

    [UpstreamTest("SE-055 should support disabled")]
    public async Task ShouldSupportDisabled(IPage page)
    {
        await page.SetContentAsync(@"
    <button>Hi</button>
    <button disabled>Bye</button>
    <button aria-disabled=""true"">Hello</button>
    <button aria-disabled=""false"">Oh</button>
    <fieldset disabled>
      <button>Yay</button>
    </fieldset>
    <select>
      <optgroup disabled>
        <option>one</option>
      </optgroup>
      <optgroup>
        <option>two</option>
      </optgroup>
      <option disabled>three</option>
    </select>
  ");
        Assert.That(await page.Locator("role=button[disabled]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button disabled=\"\">Bye</button>",
            "<button aria-disabled=\"true\">Hello</button>",
            "<button>Yay</button>",
        }));
        Assert.That(await page.Locator("role=button[disabled=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button disabled=\"\">Bye</button>",
            "<button aria-disabled=\"true\">Hello</button>",
            "<button>Yay</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Disabled = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button disabled=\"\">Bye</button>",
            "<button aria-disabled=\"true\">Hello</button>",
            "<button>Yay</button>",
        }));
        Assert.That(await page.Locator("role=button[disabled=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-disabled=\"false\">Oh</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Disabled = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-disabled=\"false\">Oh</button>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Option, new() { Disabled = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option>one</option>",
            "<option disabled=\"\">three</option>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Option, new() { Disabled = false }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option>two</option>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Option).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option>one</option>",
            "<option>two</option>",
            "<option disabled=\"\">three</option>",
        }));
    }

    [UpstreamTest("SE-056 should inherit disabled from the ancestor")]
    public async Task ShouldInheritDisabledFromTheAncestor(IPage page)
    {
        await page.SetContentAsync(@"
    <span aria-disabled=""true"">
      <button>Click me!</button>
    </span>
  ");
        await Expect(page.Locator("button")).ToBeDisabledAsync();

        await page.SetContentAsync(@"
    <span aria-disabled=""true"">
      <h1>Heading</h1>
    </span>
  ");
        await Expect(page.Locator("h1")).Not.ToBeDisabledAsync();
    }

    [UpstreamTest("SE-057 should support disabled fieldset")]
    public async Task ShouldSupportDisabledFieldset(IPage page)
    {
        await page.SetContentAsync(@"
    <fieldset disabled>
      <input></input>
      <button data-testid=""inside-fieldset-element"">x</button>
      <legend>
        <button data-testid=""inside-legend-element"">legend</button>
      </legend>
    </fieldset>

    <fieldset disabled>
      <legend>
        <div>
          <button data-testid=""nested-inside-legend-element"">x</button>
        </div>
      </legend>
    </fieldset>

    <fieldset disabled>
      <div></div>
      <legend>
        <button data-testid=""first-legend-element"">x</button>
      </legend>
      <legend>
        <button data-testid=""second-legend-element"">x</button>
      </legend>
    </fieldset>

    <fieldset disabled>
      <fieldset>
        <button data-testid=""deep-button"">x</button>
      </fieldset>
    </fieldset>
  ");

        await Expect(page.GetByTestId("inside-legend-element")).ToBeEnabledAsync();
        await Expect(page.GetByTestId("nested-inside-legend-element")).ToBeEnabledAsync();
        await Expect(page.GetByTestId("first-legend-element")).ToBeEnabledAsync();
        await Expect(page.GetByTestId("second-legend-element")).ToBeDisabledAsync();
        await Expect(page.GetByTestId("deep-button")).ToBeDisabledAsync();
    }

    [UpstreamTest("SE-058 should support level")]
    public async Task ShouldSupportLevel(IPage page)
    {
        await page.SetContentAsync(@"
    <h1>Hello</h1>
    <h3>Hi</h3>
    <div role=""heading"" aria-level=""5"">Bye</div>
  ");
        Assert.That(await page.Locator("role=heading[level=1]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<h1>Hello</h1>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Heading, new() { Level = 1 }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<h1>Hello</h1>",
        }));
        Assert.That(await page.Locator("role=heading[level=3]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<h3>Hi</h3>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Heading, new() { Level = 3 }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<h3>Hi</h3>",
        }));
        Assert.That(await page.Locator("role=heading[level=5]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"heading\" aria-level=\"5\">Bye</div>",
        }));
    }

    [UpstreamTest("SE-059 should filter hidden, unless explicitly asked for")]
    public async Task ShouldFilterHiddenUnlessExplicitlyAskedFor(IPage page)
    {
        await page.SetContentAsync(@"
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

    <input name=""one"">
    <details>
      <summary>Open form</summary>
      <label>
         Label
         <input name=""two"">
      </label>
    </details>

    <select>
      <option style=""visibility:hidden"">One</option>
      <option style=""display:none"">Two</option>
      <option>Three</option>
    </select>

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
        Assert.That(await page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-hidden=\"false\">Nay</button>",
            "<button style=\"visibility:visible\">Still here</button>",
            "<button>Shadow1</button>",
        }));
        Assert.That(await page.Locator("role=button[include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
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
        }));
        Assert.That(await page.Locator("role=button[include-hidden=true]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
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
        }));
        Assert.That(await page.Locator("role=button[include-hidden=false]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>Hi</button>",
            "<button aria-hidden=\"false\">Nay</button>",
            "<button style=\"visibility:visible\">Still here</button>",
            "<button>Shadow1</button>",
        }));
        Assert.That(await page.Locator("role=textbox").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<input name=\"one\">",
        }));
        Assert.That(await page.Locator("role=option").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<option style=\"visibility:hidden\">One</option>",
            "<option>Three</option>",
        }));
    }

    [UpstreamTest("SE-060 should support name")]
    public async Task ShouldSupportName(IPage page, string browserName, int browserMajorVersion)
    {
        await page.SetContentAsync(@"
    <div role=""button"" aria-label="" Hello ""></div>
    <div role=""button"" aria-label=""Hallo""></div>
    <div role=""button"" aria-label=""Hello"" aria-hidden=""true""></div>
    <div role=""button"" aria-label=""123"" aria-hidden=""true""></div>
    <div role=""button"" aria-label='foo""bar' aria-hidden=""true""></div>

    <span id=""title"">Chosen name</span>
    <div role=""group"" aria-labelledby=""title""></div>
  ");

        var group = page.GetByRole(AriaRole.Group);
        await Expect(group).ToHaveAccessibleNameAsync("Chosen name");

        Assert.That(await page.Locator("role=button[name=\"Hello\"]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
        }));
        Assert.That(await page.Locator("role=button[name=\" \n Hello \"]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Hello" }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
        }));

        Assert.That(await page.Locator("role=button[name*=\"all\"]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\"Hallo\"></div>",
        }));

        Assert.That(await page.Locator("role=button[name=/^H[ae]llo$/]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hallo\"></div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^H[ae]llo$") }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hallo\"></div>",
        }));

        Assert.That(await page.Locator("role=button[name=/h.*o/i]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hallo\"></div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("h.*o", RegexOptions.IgnoreCase) }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hallo\"></div>",
        }));

        Assert.That(await page.Locator("role=button[name=\"Hello\"][include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hello\" aria-hidden=\"true\"></div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "Hello", IncludeHidden = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hello\" aria-hidden=\"true\"></div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "hello", IncludeHidden = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
            "<div role=\"button\" aria-label=\"Hello\" aria-hidden=\"true\"></div>",
        }));

        Assert.That(await page.Locator("role=button[name=Hello]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\" Hello \"></div>",
        }));
        Assert.That(await page.Locator("role=button[name=123][include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\"123\" aria-hidden=\"true\"></div>",
        }));
        Assert.That(await page.GetByRole(AriaRole.Button, new() { Name = "123", IncludeHidden = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<div role=\"button\" aria-label=\"123\" aria-hidden=\"true\"></div>",
        }));

        // Old electron/android do not have this API.
        var isMissingAriaLabelledByElements = browserName == "chromium" && browserMajorVersion < 135;
        if (!isMissingAriaLabelledByElements)
        {
            await group.EvaluateAsync(@"element => {
      element.ariaLabelledByElements = [document.querySelector('#title')];
    }");
            Assert.That(await page.GetByRole(AriaRole.Group, new() { Name = "Chosen name", Exact = true }).EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
            {
                "<div role=\"group\" aria-labelledby=\"\"></div>",
            }));
        }
    }

    [UpstreamTest("SE-061 should support option name with html whitespace")]
    public async Task ShouldSupportOptionNameWithHtmlWhitespace(IPage page)
    {
        await page.SetContentAsync(@"
    <select>
      <option value=""html"">&nbsp;HTML</option>
    </select>
  ");
        await Expect(page.GetByRole(AriaRole.Option, new() { Name = "HTML" })).ToHaveCountAsync(1);
    }

    [UpstreamTest("SE-062 errors")]
    public async Task Errors(IPage page)
    {
        var e0 = await Catch(() => page.QuerySelectorAsync("role=[bar]"));
        Assert.That(e0.Message, Does.Contain("Role must not be empty"));

        var e1 = await Catch(() => page.QuerySelectorAsync("role=foo[sElected]"));
        Assert.That(e1.Message, Does.Contain("Unknown attribute \"sElected\", must be one of \"checked\", \"description\", \"disabled\", \"expanded\", \"include-hidden\", \"level\", \"name\", \"pressed\", \"selected\""));

        var e2 = await Catch(() => page.QuerySelectorAsync("role=foo[bar . qux=true]"));
        Assert.That(e2.Message, Does.Contain("Unknown attribute \"bar.qux\""));

        var e3 = await Catch(() => page.QuerySelectorAsync("role=heading[level=\"bar\"]"));
        Assert.That(e3.Message, Does.Contain("\"level\" attribute must be compared to a number"));

        var e4 = await Catch(() => page.QuerySelectorAsync("role=checkbox[checked=\"bar\"]"));
        Assert.That(e4.Message, Does.Contain("\"checked\" must be one of true, false, \"mixed\""));

        var e5 = await Catch(() => page.QuerySelectorAsync("role=checkbox[checked~=true]"));
        Assert.That(e5.Message, Does.Contain("cannot use ~= in attribute with non-string matching value"));

        var e6 = await Catch(() => page.QuerySelectorAsync("role=button[level=3]"));
        Assert.That(e6.Message, Does.Contain("\"level\" attribute is only supported for roles: \"heading\", \"listitem\", \"row\", \"treeitem\""));

        var e7 = await Catch(() => page.QuerySelectorAsync("role=button[name]"));
        Assert.That(e7.Message, Does.Contain("\"name\" attribute must have a value"));

        var e8 = await Catch(() => page.QuerySelectorAsync("role=treeitem[expanded=\"none\"]"));
        Assert.That(e8.Message, Does.Contain("\"expanded\" must be one of true, false"));
    }

    [UpstreamTest("SE-063 hidden with shadow dom slots")]
    public async Task HiddenWithShadowDomSlots(IPage page)
    {
        await page.SetContentAsync(@"
    <div make-hidden>
      <button>hidden1</button>
    </div>
    <div make-hidden>
      <span><button>hidden2</button></v>
    </div>
    <div>
      <button>visible1</button>
    </div>
    <div>
      <span><button>visible2</button></span>
    </div>
    <script>
      for (const div of document.querySelectorAll('div')) {
        const hidden = div.hasAttribute('make-hidden');
        div.attachShadow({ mode: 'open' }).innerHTML = hidden ? 'nothing to see here' : '<slot></slot>';
      }
    </script>
  ");
        Assert.That(await page.Locator("role=button").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>visible1</button>",
            "<button>visible2</button>",
        }));
        Assert.That(await page.Locator("role=button[include-hidden]").EvaluateAllAsync<string[]>("els => els.map(e => e.outerHTML)"), Is.EqualTo(new[]
        {
            "<button>hidden1</button>",
            "<button>hidden2</button>",
            "<button>visible1</button>",
            "<button>visible2</button>",
        }));
    }

    [UpstreamTest("SE-064 should support output accessible name")]
    public async Task ShouldSupportOutputAccessibleName(IPage page)
    {
        await page.SetContentAsync("<label>Output1<output>output</output></label>");
        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Output1" })).ToBeVisibleAsync();
    }

    [UpstreamTest("SE-065 should not match scope by default")]
    public async Task ShouldNotMatchScopeByDefault(IPage page)
    {
        await page.SetContentAsync(@"
    <ul>
      <li aria-label=""Parent list"">
        Parent list
        <ul>
          <li>child 1</li>
          <li>child 2</li>
        </ul>
      </li>
    </ul>
  ");
        var children = page.GetByRole(AriaRole.Listitem, new() { Name = "Parent list" }).GetByRole(AriaRole.Listitem);
        await Expect(children).ToHaveCountAsync(2);
        await Expect(children).ToHaveTextAsync(new[] { "child 1", "child 2" });
    }
}
