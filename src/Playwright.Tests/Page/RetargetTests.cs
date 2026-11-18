/**
 * Copyright 2017 Google Inc. All rights reserved.
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

/// <summary>Port of tests/page/retarget.spec.ts.</summary>
public class RetargetTests : UpstreamPageTest
{
    private static Task GiveItAChanceToResolve(IPage page) => Rafraf(page, 5);

    [UpstreamTest("RE-001 element state checks should work as expected for label with zero-sized input")]
    public async Task ElementStateChecksShouldWorkAsExpectedForLabelWithZeroSizedInput()
    {
        await Page.SetContentAsync(@"
    <label>
      Click me
      <input disabled style=""width:0;height:0;padding:0;margin:0;border:0;"">
    </label>
  ");
        // Visible checks the label.
        Assert.That(await Page.IsVisibleAsync("text=Click me"), Is.EqualTo(true));
        Assert.That(await Page.IsHiddenAsync("text=Click me"), Is.EqualTo(false));

        // Enabled checks the input.
        Assert.That(await Page.IsEnabledAsync("text=Click me"), Is.EqualTo(false));
        Assert.That(await Page.IsDisabledAsync("text=Click me"), Is.EqualTo(true));
    }

    [UpstreamTest("RE-002 should wait for enclosing disabled button")]
    public async Task ShouldWaitForEnclosingDisabledButton()
    {
        await Page.SetContentAsync("<button><span>Target</span></button>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var done = false;
        // .NET: const promise = span.waitForElementState('disabled').then(() => done = true);
        async Task Promise()
        {
            await span.WaitForElementStateAsync(ElementState.Disabled);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await span.EvaluateAsync("span => span.parentElement.disabled = true");
        await promise;
    }

    [UpstreamTest("RE-003 should wait for enclosing button with a disabled fieldset")]
    public async Task ShouldWaitForEnclosingButtonWithADisabledFieldset()
    {
        await Page.SetContentAsync("<fieldset disabled=true><button><span>Target</span></button></div>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var done = false;
        // .NET: const promise = span.waitForElementState('enabled').then(() => done = true);
        async Task Promise()
        {
            await span.WaitForElementStateAsync(ElementState.Enabled);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await span.EvaluateAsync("span => span.parentElement.parentElement.disabled = false");
        await promise;
    }

    [UpstreamTest("RE-004 should wait for enclosing enabled button")]
    public async Task ShouldWaitForEnclosingEnabledButton()
    {
        await Page.SetContentAsync("<button disabled><span>Target</span></button>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var done = false;
        // .NET: const promise = span.waitForElementState('enabled').then(() => done = true);
        async Task Promise()
        {
            await span.WaitForElementStateAsync(ElementState.Enabled);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await span.EvaluateAsync("span => span.parentElement.disabled = false");
        await promise;
    }

    [UpstreamTest("RE-005 should check the box outside shadow dom label")]
    public async Task ShouldCheckTheBoxOutsideShadowDomLabel()
    {
        await Page.SetContentAsync("<div></div>");
        await Page.EvalOnSelectorAsync("div", @"div => {
    const root = div.attachShadow({ mode: 'open' });
    const label = document.createElement('label');
    label.setAttribute('for', 'target');
    label.textContent = 'Click me';
    root.appendChild(label);
    const input = document.createElement('input');
    input.setAttribute('type', 'checkbox');
    input.setAttribute('id', 'target');
    root.appendChild(input);
  }");
        await Page.CheckAsync("label");
        Assert.That(await Page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(true));
    }

    [UpstreamTest("RE-006 setInputFiles should work with label")]
    public async Task SetInputFilesShouldWorkWithLabel()
    {
        await Page.SetContentAsync("<label for=target>Choose a file</label><input id=target type=file>");
        await Page.SetInputFilesAsync("text=Choose a file", Asset("file-to-upload.txt"));
        Assert.That(await Page.EvalOnSelectorAsync<int>("input", "input => input.files.length"), Is.EqualTo(1));
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.files?.[0].name"), Is.EqualTo("file-to-upload.txt"));
    }

    private sealed class Options
    {
        public bool? Disabled { get; set; }

        public bool? Hidden { get; set; }

        public bool? Readonly { get; set; }
    }

    private static string OptionsToAttributes(Options options) => $" {(options?.Disabled == true ? "disabled" : "")} {(options?.Hidden == true ? "hidden" : "")} {(options?.Readonly == true ? "readonly" : "")} ";

    private static string DomInLabel(string dom, Options options = null) => $"<label {OptionsToAttributes(options)}>Text {dom}</label>";

    private static string DomLabelFor(string dom, Options options = null) => $"<label {OptionsToAttributes(options)} for=\"target\"><h1>Text</h1></label>{dom}";

    private static string DomStandalone(string dom) => dom;

    private static string DomInButton(string dom, Options options = null) => $"<button {OptionsToAttributes(options)}>Button {dom}</button>";

    private static string DomInLink(string dom, Options options = null) => $"<button {OptionsToAttributes(options)}>Button {dom}</button>";

    [UpstreamTest("RE-007 enabled/disabled retargeting")]
    public async Task EnabledDisabledRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<input id=target>"), enabled: true, locator: "label"),
            (dom: DomLabelFor("<input id=target>"), enabled: true, locator: "label"),
            (dom: DomStandalone("<input id=target>"), enabled: true, locator: "input"),
            (dom: DomInButton("<input id=target>"), enabled: true, locator: "input"),
            (dom: DomInLink("<input id=target>"), enabled: true, locator: "input"),
            (dom: DomInButton("<input id=target>", new() { Disabled = true }), enabled: true, locator: "input"),
            (dom: DomInLabel("<li role=menuitem id=target aria-disabled=false></li>"), enabled: true, locator: "li"),

            (dom: DomInLabel("<input id=target disabled>"), enabled: false, locator: "label"),
            (dom: DomLabelFor("<input id=target disabled>"), enabled: false, locator: "label"),
            (dom: DomStandalone("<input id=target disabled>"), enabled: false, locator: "input"),
            (dom: DomInButton("<input id=target disabled>"), enabled: false, locator: "input"),
            (dom: DomInLink("<input id=target disabled>"), enabled: false, locator: "input"),
            (dom: DomInButton("<input id=target disabled>", new() { Disabled = true }), enabled: false, locator: "input"),
            (dom: DomInLabel("<li role=menuitem id=target aria-disabled=true></li>"), enabled: false, locator: "li"),
            (dom: DomInLabel("<select><option disabled>Other employees</option><option>Alex</option></select>"), enabled: false, locator: "option >> nth=0"),
            (dom: DomInLabel("<select><optgroup disabled label=\"Other employees\"><option>Alex</option></optgroup></select>"), enabled: false, locator: "optgroup"),
        };
        foreach (var (dom, enabled, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" should be enabled={(enabled ? "true" : "false")}", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                var handle = await Page.QuerySelectorAsync(locator);
                Assert.That(await target.IsEnabledAsync(), Is.EqualTo(enabled));
                Assert.That(await target.IsDisabledAsync(), Is.EqualTo(!enabled));
                if (enabled)
                {
                    await Expect(target).ToBeEnabledAsync();
                    await Expect(target).Not.ToBeDisabledAsync();
                    await handle.WaitForElementStateAsync(ElementState.Enabled);
                }
                else
                {
                    await Expect(target).Not.ToBeEnabledAsync();
                    await Expect(target).ToBeDisabledAsync();
                    await handle.WaitForElementStateAsync(ElementState.Disabled);
                }
            });
        }
    }

    [UpstreamTest("RE-008 visible/hidden retargeting")]
    public async Task VisibleHiddenRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<span id=target>content</span>"), visible: true, locator: "label"),
            (dom: DomInLabel("<span id=target hidden>content</span>"), visible: true, locator: "label"),
            (dom: DomLabelFor("<span id=target>content</span>"), visible: true, locator: "label"),
            (dom: DomLabelFor("<span id=target hidden>content</span>"), visible: true, locator: "label"),
            (dom: DomStandalone("<span id=target>content</span>"), visible: true, locator: "span"),
            (dom: DomInButton("<span id=target>content</span>"), visible: true, locator: "span"),
            (dom: DomInLink("<span id=target>content</span>"), visible: true, locator: "span"),

            (dom: DomInLabel("<span id=target>content</span>", new() { Hidden = true }), visible: false, locator: "label"),
            (dom: DomLabelFor("<span id=target>content</span>", new() { Hidden = true }), visible: false, locator: "label"),
            (dom: DomStandalone("<span id=target hidden>content</span>"), visible: false, locator: "span"),
            (dom: DomInButton("<span id=target hidden>content</span>"), visible: false, locator: "span"),
            (dom: DomInButton("<span id=target>content</span>", new() { Hidden = true }), visible: false, locator: "span"),
            (dom: DomInLink("<span id=target hidden>content</span>"), visible: false, locator: "span"),
            (dom: DomInLink("<span id=target>content</span>", new() { Hidden = true }), visible: false, locator: "span"),
        };
        foreach (var (dom, visible, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" should be visible={(visible ? "true" : "false")}", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                var handle = await Page.QuerySelectorAsync(locator);
                Assert.That(await target.IsVisibleAsync(), Is.EqualTo(visible));
                Assert.That(await target.IsHiddenAsync(), Is.EqualTo(!visible));
                if (visible)
                {
                    await Expect(target).ToBeVisibleAsync();
                    await Expect(target).Not.ToBeHiddenAsync();
                    await handle.WaitForElementStateAsync(ElementState.Visible);
                }
                else
                {
                    await Expect(target).Not.ToBeVisibleAsync();
                    await Expect(target).ToBeHiddenAsync();
                    await handle.WaitForElementStateAsync(ElementState.Hidden);
                }
            });
        }
    }

    [UpstreamTest("RE-009 editable retargeting")]
    public async Task EditableRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<input id=target>"), editable: true, locator: "label"),
            (dom: DomLabelFor("<input id=target>"), editable: true, locator: "label"),
            (dom: DomStandalone("<input id=target>"), editable: true, locator: "input"),
            (dom: DomInButton("<input id=target>"), editable: true, locator: "input"),
            (dom: DomInLink("<input id=target>"), editable: true, locator: "input"),
            (dom: DomInButton("<input id=target>", new() { Readonly = true }), editable: true, locator: "input"),

            (dom: DomInLabel("<input id=target readonly>"), editable: false, locator: "label"),
            (dom: DomLabelFor("<input id=target readonly>"), editable: false, locator: "label"),
            (dom: DomStandalone("<input id=target readonly>"), editable: false, locator: "input"),
            (dom: DomInButton("<input id=target readonly>"), editable: false, locator: "input"),
            (dom: DomInLink("<input id=target readonly>"), editable: false, locator: "input"),
            (dom: DomInButton("<input id=target readonly>", new() { Readonly = true }), editable: false, locator: "input"),
        };
        foreach (var (dom, editable, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" should be editable={(editable ? "true" : "false")}", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                var handle = await Page.QuerySelectorAsync(locator);
                Assert.That(await target.IsEditableAsync(), Is.EqualTo(editable));
                if (editable)
                {
                    await Expect(target).ToBeEditableAsync();
                    await handle.WaitForElementStateAsync(ElementState.Editable);
                }
                else
                {
                    await Expect(target).Not.ToBeEditableAsync();
                }
            });
        }
    }

    [UpstreamTest("RE-010 input value retargeting")]
    [LocalFixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
    public async Task InputValueRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<input id=target>"), locator: "label"),
            (dom: DomLabelFor("<input id=target>"), locator: "label"),
            (dom: DomStandalone("<input id=target>"), locator: "input"),
            (dom: DomInButton("<input id=target>"), locator: "input"),
            (dom: DomInLink("<input id=target>"), locator: "input"),
            (dom: DomInButton("<input id=target>"), locator: "input"),
        };
        foreach (var (dom, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" input value", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                var handle = await Page.QuerySelectorAsync(locator);

                Assert.That(await target.InputValueAsync(), Is.EqualTo(""));
                Assert.That(await handle.InputValueAsync(), Is.EqualTo(""));
                await Expect(target).ToHaveValueAsync("");

                await target.FillAsync("foo");
                Assert.That(await target.InputValueAsync(), Is.EqualTo("foo"));
                Assert.That(await handle.InputValueAsync(), Is.EqualTo("foo"));
                await Expect(target).ToHaveValueAsync("foo");

                await Page.EvalOnSelectorAsync("#target", "input => input.value = 'bar'");
                Assert.That(await target.InputValueAsync(), Is.EqualTo("bar"));
                Assert.That(await handle.InputValueAsync(), Is.EqualTo("bar"));
                await Expect(target).ToHaveValueAsync("bar");

                await target.SelectTextAsync();
                Assert.That(await Page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("bar"));
            });
        }
    }

    [UpstreamTest("RE-011 selection retargeting")]
    [LocalFixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
    public async Task SelectionRetargeting()
    {
        var cases = new[]
        {
            (dom: DomStandalone("<div contenteditable id=target>content</div>"), locator: "div"),
            (dom: DomInButton("<div contenteditable id=target>content</div>"), locator: "div"),
            (dom: DomInLink("<div contenteditable id=target>content</div>"), locator: "div"),
            (dom: DomInButton("<div contenteditable id=target>content</div>"), locator: "div"),
        };
        foreach (var (dom, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" text selection", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                var handle = await Page.QuerySelectorAsync(locator);

                Assert.That(await target.IsEditableAsync(), Is.EqualTo(true));
                Assert.That(await handle.IsEditableAsync(), Is.EqualTo(true));
                await Expect(Page.Locator("#target")).ToHaveTextAsync("content");

                await target.FillAsync("foo");
                await Expect(Page.Locator("#target")).ToHaveTextAsync("foo");

                await target.SelectTextAsync();
                Assert.That(await Page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("foo"));
            });
        }
    }

    [UpstreamTest("RE-012 select options retargeting")]
    public async Task SelectOptionsRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<select id=target multiple><option value=dog selected>Dog</option><option value=cat>Cat</option></select>"), locator: "label"),
            (dom: DomLabelFor("<select id=target multiple><option value=dog selected>Dog</option><option value=cat>Cat</option></select>"), locator: "label"),
            (dom: DomStandalone("<select id=target multiple><option value=dog selected>Dog</option><option value=cat>Cat</option></select>"), locator: "select"),
            (dom: DomInButton("<select id=target multiple><option value=dog selected>Dog</option><option value=cat>Cat</option></select>"), locator: "select"),
            (dom: DomInLink("<select id=target multiple><option value=dog selected>Dog</option><option value=cat>Cat</option></select>"), locator: "select"),
            (dom: DomInButton("<select id=target multiple><option value=dog selected>Dog</option><option value=cat>Cat</option></select>"), locator: "select"),
        };
        foreach (var (dom, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" select option", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                var handle = await Page.QuerySelectorAsync(locator);

                Assert.That(await target.InputValueAsync(), Is.EqualTo("dog"));
                Assert.That(await handle.InputValueAsync(), Is.EqualTo("dog"));
                await Expect(target).ToHaveValueAsync("dog");
                await Expect(target).ToHaveValuesAsync(new[] { "dog" });

                await target.SelectOptionAsync("cat");
                Assert.That(await target.InputValueAsync(), Is.EqualTo("cat"));
                Assert.That(await handle.InputValueAsync(), Is.EqualTo("cat"));
                await Expect(target).ToHaveValueAsync("cat");
                await Expect(target).ToHaveValuesAsync(new[] { "cat" });
            });
        }
    }

    [UpstreamTest("RE-013 direct actions retargeting")]
    public async Task DirectActionsRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<div>content</div><input id=target value=oh>"), locator: "div"),
            (dom: DomLabelFor("<div>content</div><input id=target value=oh>"), locator: "div"),
            (dom: DomStandalone("<div>content</div>"), locator: "div"),
            (dom: DomInButton("<div>content</div>"), locator: "div"),
            (dom: DomInLink("<div>content</div>"), locator: "div"),
            (dom: DomInButton("<div>content</div>"), locator: "div"),
        };
        foreach (var (dom, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" direct actions", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);

                Assert.That(await target.InnerTextAsync(), Is.EqualTo("content"));
                Assert.That(await target.TextContentAsync(), Is.EqualTo("content"));
                await Expect(target).ToHaveTextAsync("content");
                await Expect(target).ToContainTextAsync("content");
                await Expect(target).Not.ToBeFocusedAsync();
                await Expect(target).ToHaveCountAsync(1);

                await Page.EvalOnSelectorAsync("div", "div => div.foo = 'bar'");
                await Expect(target).ToHaveJSPropertyAsync("foo", "bar");

                await Page.EvalOnSelectorAsync("div", "div => div.classList.add('cls')");
                await Expect(target).ToHaveClassAsync("cls");

                await Page.EvalOnSelectorAsync("div", "div => div.id = 'myid'");
                await Expect(target).ToHaveIdAsync("myid");
                await Expect(target).ToHaveAttributeAsync("id", "myid");
                Assert.That(await target.GetAttributeAsync("id"), Is.EqualTo("myid"));
            });
        }
    }

    [UpstreamTest("RE-014 check retargeting")]
    public async Task CheckRetargeting()
    {
        var cases = new[]
        {
            (dom: DomInLabel("<input type=checkbox id=target>"), locator: "label"),
            (dom: DomLabelFor("<input type=checkbox id=target>"), locator: "label"),
            (dom: DomStandalone("<input type=checkbox id=target>"), locator: "input"),
            (dom: DomInButton("<input type=checkbox id=target>"), locator: "input"),
            (dom: DomInLink("<input type=checkbox id=target>"), locator: "input"),
            (dom: DomInButton("<input type=checkbox id=target>"), locator: "input"),
        };
        foreach (var (dom, locator) in cases)
        {
            await Info.Step($"\"{locator}\" in \"{dom}\" check", async () =>
            {
                await Page.SetContentAsync(dom);
                var target = Page.Locator(locator);
                Assert.That(await target.IsCheckedAsync(), Is.EqualTo(false));
                await Expect(target).Not.ToBeCheckedAsync();
                await Expect(target).ToBeCheckedAsync(new() { Checked = false });

                await Page.EvalOnSelectorAsync("input", "input => input.checked = true");
                Assert.That(await target.IsCheckedAsync(), Is.EqualTo(true));
                await Expect(target).ToBeCheckedAsync();
                await Expect(target).ToBeCheckedAsync(new() { Checked = true });

                await target.UncheckAsync();
                Assert.That(await Page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(false));

                await target.CheckAsync();
                Assert.That(await Page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(true));

                await target.SetCheckedAsync(false);
                Assert.That(await Page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(false));
            });
        }
    }

    [UpstreamTest("RE-015 should not retarget anchor into parent label")]
    public async Task ShouldNotRetargetAnchorIntoParentLabel()
    {
        await Page.SetContentAsync(@"
    <label disabled>Text<a href='#' onclick='window.__clicked=1'>Target</a></label>
  ");
        await Page.Locator("a").ClickAsync();
        Assert.That(await Page.EvaluateAsync<int>("window.__clicked"), Is.EqualTo(1));

        await Page.SetContentAsync(@"
    <input type=""radio"" id=""input-id"" checked disabled />
    <label for=""input-id"">Text<a href='#' onclick='window.__clicked=2'>Target</a></label>
  ");
        await Page.Locator("a").ClickAsync();
        Assert.That(await Page.EvaluateAsync<int>("window.__clicked"), Is.EqualTo(2));
    }
}
