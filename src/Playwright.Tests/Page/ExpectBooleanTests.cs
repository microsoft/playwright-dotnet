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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/expect-boolean.spec.ts.</summary>
public class ExpectBooleanTests
{
    public class ToBeChecked
    {
        [UpstreamTest("EX-001 default")]
        public async Task Default(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox checked></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeCheckedAsync();
        }

        [UpstreamTest("EX-002 with checked:true")]
        public async Task WithCheckedTrue(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox checked></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeCheckedAsync(new() { Checked = true });
        }

        [UpstreamTest("EX-003 with checked:false")]
        public async Task WithCheckedFalse(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox checked></input>");
            var locator = page.Locator("input");
            await Expect(locator).Not.ToBeCheckedAsync(new() { Checked = false });
        }

        [UpstreamTest("EX-004 with indeterminate:true")]
        public async Task WithIndeterminateTrue(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            await page.Locator("input").EvaluateAsync("e => e.indeterminate = true");
            var locator = page.Locator("input");
            await Expect(locator).ToBeCheckedAsync(new() { Indeterminate = true });
        }

        [UpstreamTest("EX-005 with indeterminate:true and checked")]
        public async Task WithIndeterminateTrueAndChecked(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            await page.Locator("input").EvaluateAsync("e => e.indeterminate = true");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).ToBeCheckedAsync(new() { Indeterminate = true, Checked = false }));
            Assert.That(error.Message, Does.Contain("Can't assert indeterminate and checked at the same time"));
        }

        [UpstreamTest("EX-006 fail")]
        [Fixme("dotnet: the message of ToBeCheckedAsync is \"Locator expected to be checked \\nCall log: ...\"; upstream expects \"expect(locator).toBeChecked() failed\" with the Locator, Expected, Received and Timeout lines")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).ToBeCheckedAsync(new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toBeChecked() failed

Locator:  locator('input')
Expected: checked
Received: unchecked
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toBeChecked\" locator('input') with timeout 1000ms"));
        }

        [UpstreamTest("EX-007 with not")]
        public async Task WithNot(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            var locator = page.Locator("input");
            await Expect(locator).Not.ToBeCheckedAsync();
        }

        [UpstreamTest("EX-008 with not and checked:false")]
        public async Task WithNotAndCheckedFalse(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeCheckedAsync(new() { Checked = false });
        }

        [UpstreamTest("EX-009 fail with not")]
        [Fixme("dotnet: the message of Not.ToBeCheckedAsync is \"Locator expected not to be checked \\nCall log: ...\"; upstream expects \"expect(locator).not.toBeChecked() failed\" with the Locator, Expected, Received and Timeout lines")]
        public async Task FailWithNot(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox checked></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).Not.ToBeCheckedAsync(new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).not.toBeChecked() failed

Locator:  locator('input')
Expected: not checked
Received: checked
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"not toBeChecked\" locator('input') with timeout 1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("locator resolved to <input checked type=\"checkbox\"/>"));
        }

        [UpstreamTest("EX-010 fail with checked:false")]
        [Fixme("dotnet: the message of ToBeCheckedAsync with Checked = false is \"Locator expected not to be checked \\nCall log: ...\"; upstream expects \"expect(locator).toBeChecked({ checked: false }) failed\" with the Locator, Expected, Received and Timeout lines")]
        public async Task FailWithCheckedFalse(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox checked></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).ToBeCheckedAsync(new() { Checked = false, Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toBeChecked({ checked: false }) failed

Locator:  locator('input')
Expected: unchecked
Received: checked
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toBeChecked\" locator('input') with timeout 1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("locator resolved to <input checked type=\"checkbox\"/>"));
        }

        [UpstreamTest("EX-011 fail with indeterminate: true")]
        [Fixme("dotnet: the message of ToBeCheckedAsync with Indeterminate = true is \"Locator expected to be indeterminate \\nCall log: ...\"; upstream expects \"expect(locator).toBeChecked({ indeterminate: true }) failed\" with the Locator, Expected, Received and Timeout lines")]
        public async Task FailWithIndeterminateTrue(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).ToBeCheckedAsync(new() { Indeterminate = true, Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).toBeChecked({ indeterminate: true }) failed

Locator:  locator('input')
Expected: indeterminate
Received: unchecked
Timeout:  1000ms"));
            Assert.That(StripAnsi(error.Message), Does.Contain("- Expect \"toBeChecked\" locator('input') with timeout 1000ms"));
        }

        [UpstreamTest("EX-012 fail missing")]
        [Fixme("dotnet: the message of Not.ToBeCheckedAsync is \"Locator expected not to be checked\\nError: element(s) not found \\nCall log: ...\"; upstream expects \"expect(locator).not.toBeChecked() failed\" with the Locator, Expected and Timeout lines before the error")]
        public async Task FailMissing(IPage page)
        {
            await page.SetContentAsync("<div>no inputs here</div>");
            var locator2 = page.Locator("input2");
            var error = await Catch(() => Expect(locator2).Not.ToBeCheckedAsync(new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).not.toBeChecked() failed

Locator: locator('input2')
Expected: not checked
Timeout: 1000ms
Error: element(s) not found

Call log:
  - Expect ""not toBeChecked"" locator('input2') with timeout 1000ms
  - waiting for locator('input2')
"));
        }

        [UpstreamTest("EX-013 with role")]
        public async Task WithRole(IPage page)
        {
            foreach (var role in new[] { "checkbox", "menuitemcheckbox", "option", "radio", "switch", "menuitemradio", "treeitem" })
            {
                await It.Step($"role={role}", async () =>
                {
                    await page.SetContentAsync($"<div role={role} aria-checked=true>I am checked</div>");
                    var locator = page.Locator("div");
                    await Expect(locator).ToBeCheckedAsync();
                });
            }
        }

        [UpstreamTest("EX-014 friendly log")]
        public async Task FriendlyLog(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            var message1 = (await Catch(() => Expect(page.Locator("input")).ToBeCheckedAsync(new() { Timeout = 1000 }))).Message;
            Assert.That(message1, Does.Contain("unexpected value \"unchecked\""));

            await page.SetContentAsync("<input type=checkbox checked></input>");
            var message2 = (await Catch(() => Expect(page.Locator("input")).ToBeCheckedAsync(new() { Checked = false, Timeout = 1000 }))).Message;
            Assert.That(message2, Does.Contain("unexpected value \"checked\""));
        }

        [UpstreamTest("EX-015 with impossible timeout")]
        public async Task WithImpossibleTimeout(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox checked></input>");
            await Expect(page.Locator("input")).ToBeCheckedAsync(new() { Timeout = 1 });
        }

        [UpstreamTest("EX-016 with impossible timeout .not")]
        public async Task WithImpossibleTimeoutNot(IPage page)
        {
            await page.SetContentAsync("<input type=checkbox></input>");
            await Expect(page.Locator("input")).Not.ToBeCheckedAsync(new() { Timeout = 1 });
        }
    }

    public class ToBeEditable
    {
        [UpstreamTest("EX-017 default")]
        public async Task Default(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeEditableAsync();
        }

        [UpstreamTest("EX-018 with not")]
        public async Task WithNot(IPage page)
        {
            await page.SetContentAsync("<input readonly></input>");
            var locator = page.Locator("input");
            await Expect(locator).Not.ToBeEditableAsync();
        }

        [UpstreamTest("EX-019 with editable:true")]
        public async Task WithEditableTrue(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeEditableAsync(new() { Editable = true });
        }

        [UpstreamTest("EX-020 with editable:false")]
        public async Task WithEditableFalse(IPage page)
        {
            await page.SetContentAsync("<input readonly></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeEditableAsync(new() { Editable = false });
        }

        [UpstreamTest("EX-021 with not and editable:false")]
        public async Task WithNotAndEditableFalse(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            await Expect(locator).Not.ToBeEditableAsync(new() { Editable = false });
        }

        [UpstreamTest("EX-022 throws")]
        public async Task Throws(IPage page)
        {
            await page.SetContentAsync("<button>");
            var locator = page.Locator("button");
            var error = await Catch(() => Expect(locator).ToBeEditableAsync());
            Assert.That(error.Message, Does.Contain("Element is not an <input>, <textarea>, <select> or [contenteditable] and does not have a role allowing [aria-readonly]"));
        }
    }

    public class ToBeEnabled
    {
        [UpstreamTest("EX-023 default")]
        public async Task Default(IPage page)
        {
            await page.SetContentAsync("<button>Text</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeEnabledAsync();
        }

        [UpstreamTest("EX-024 with enabled:true")]
        public async Task WithEnabledTrue(IPage page)
        {
            await page.SetContentAsync("<button>Text</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeEnabledAsync(new() { Enabled = true });
        }

        [UpstreamTest("EX-025 with enabled:false")]
        public async Task WithEnabledFalse(IPage page)
        {
            await page.SetContentAsync("<button disabled>Text</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeEnabledAsync(new() { Enabled = false });
        }

        [UpstreamTest("EX-026 failed")]
        public async Task Failed(IPage page)
        {
            await page.SetContentAsync("<button disabled>Text</button>");
            var locator = page.Locator("button");
            var error = await Catch(() => Expect(locator).ToBeEnabledAsync(new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to <button disabled>Text</button>"));
        }

        [UpstreamTest("EX-027 eventually")]
        public async Task Eventually(IPage page)
        {
            await page.SetContentAsync("<button disabled>Text</button>");
            var locator = page.Locator("button");
            SetTimeout(() =>
            {
                IgnoreErrors(locator.EvaluateAsync("e => e.removeAttribute('disabled')"));
            }, 500);
            await Expect(locator).ToBeEnabledAsync();
        }

        [UpstreamTest("EX-028 eventually with not")]
        public async Task EventuallyWithNot(IPage page)
        {
            await page.SetContentAsync("<button>Text</button>");
            var locator = page.Locator("button");
            SetTimeout(() =>
            {
                IgnoreErrors(locator.EvaluateAsync("e => e.setAttribute('disabled', '')"));
            }, 500);
            await Expect(locator).Not.ToBeEnabledAsync();
        }

        [UpstreamTest("EX-029 with not and enabled:false")]
        public async Task WithNotAndEnabledFalse(IPage page)
        {
            await page.SetContentAsync("<button>Text</button>");
            var locator = page.Locator("button");
            await Expect(locator).Not.ToBeEnabledAsync(new() { Enabled = false });
        }

        [UpstreamTest("EX-030 toBeDisabled")]
        public async Task ToBeDisabled(IPage page)
        {
            await page.SetContentAsync("<button disabled>Text</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeDisabledAsync();
        }
    }

    [UpstreamTest("EX-031 toBeEmpty input")]
    public async Task ToBeEmptyInput(IPage page)
    {
        await page.SetContentAsync("<input></input>");
        var locator = page.Locator("input");
        await Expect(locator).ToBeEmptyAsync();
    }

    [UpstreamTest("EX-032 not.toBeEmpty")]
    public async Task NotToBeEmpty(IPage page)
    {
        await page.SetContentAsync("<input value=text></input>");
        var locator = page.Locator("input");
        await Expect(locator).Not.ToBeEmptyAsync();
    }

    [UpstreamTest("EX-033 toBeEmpty div")]
    public async Task ToBeEmptyDiv(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 50; height: 50px\"></div>");
        var locator = page.Locator("div");
        await Expect(locator).ToBeEmptyAsync();
    }

    [UpstreamTest("EX-034 toBeDisabled with value")]
    public async Task ToBeDisabledWithValue(IPage page)
    {
        await page.SetContentAsync("<button disabled=\"yes\">Text</button>");
        var locator = page.Locator("button");
        await Expect(locator).ToBeDisabledAsync();
    }

    [UpstreamTest("EX-035 toBeChecked with value")]
    public async Task ToBeCheckedWithValue(IPage page)
    {
        await page.SetContentAsync("<input type=checkbox checked=\"yes\"></input>");
        var locator = page.Locator("input");
        await Expect(locator).ToBeCheckedAsync();
    }

    [UpstreamTest("EX-036 toBeHidden with value")]
    public async Task ToBeHiddenWithValue(IPage page)
    {
        await page.SetContentAsync("<input type=checkbox hidden=\"of course\"></input>");
        var locator = page.Locator("input");
        await Expect(locator).ToBeHiddenAsync();
    }

    [UpstreamTest("EX-037 not.toBeDisabled div")]
    public async Task NotToBeDisabledDiv(IPage page)
    {
        await page.SetContentAsync("<div disabled=\"yes\"></div>");
        var locator = page.Locator("div");
        await Expect(locator).Not.ToBeDisabledAsync();
    }

    public class ToBeVisible
    {
        [UpstreamTest("EX-038 default")]
        public async Task Default(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeVisibleAsync();
        }

        [UpstreamTest("EX-039 with not")]
        public async Task WithNot(IPage page)
        {
            await page.SetContentAsync("<button style=\"display: none\">hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).Not.ToBeVisibleAsync();
        }

        [UpstreamTest("EX-040 with visible:true")]
        public async Task WithVisibleTrue(IPage page)
        {
            await page.SetContentAsync("<button>hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeVisibleAsync(new() { Visible = true });
        }

        [UpstreamTest("EX-041 with visible:false")]
        public async Task WithVisibleFalse(IPage page)
        {
            await page.SetContentAsync("<button hidden>hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeVisibleAsync(new() { Visible = false });
        }

        [UpstreamTest("EX-042 with not and visible:false")]
        public async Task WithNotAndVisibleFalse(IPage page)
        {
            await page.SetContentAsync("<button>hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).Not.ToBeVisibleAsync(new() { Visible = false });
        }

        [UpstreamTest("EX-043 eventually")]
        public async Task Eventually(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("span");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvalOnSelectorAsync("div", "div => div.innerHTML = '<span>Hello</span>'"));
            }, 0);
            await Expect(locator).ToBeVisibleAsync();
        }

        [UpstreamTest("EX-044 eventually with not")]
        public async Task EventuallyWithNot(IPage page)
        {
            await page.SetContentAsync("<div><span>Hello</span></div>");
            var locator = page.Locator("span");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvalOnSelectorAsync("span", "span => span.textContent = ''"));
            }, 0);
            await Expect(locator).Not.ToBeVisibleAsync();
        }

        [UpstreamTest("EX-045 fail")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<button style=\"display: none\"></button>");
            var locator = page.Locator("button");
            var error = await Catch(() => Expect(locator).ToBeVisibleAsync(new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to <button></button>"));
        }

        [UpstreamTest("EX-046 fail with not")]
        public async Task FailWithNot(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).Not.ToBeVisibleAsync(new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to <input/>"));
        }

        [UpstreamTest("EX-047 with impossible timeout")]
        public async Task WithImpossibleTimeout(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            await Expect(page.Locator("#node")).ToBeVisibleAsync(new() { Timeout = 1 });
        }

        [UpstreamTest("EX-048 with impossible timeout .not")]
        public async Task WithImpossibleTimeoutNot(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            await Expect(page.Locator("no-such-thing")).Not.ToBeVisibleAsync(new() { Timeout = 1 });
        }

        [UpstreamTest("EX-049 with frameLocator")]
        public async Task WithFrameLocator(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.FrameLocator("iframe").Locator("input");
            var done = false;
            async Task Then()
            {
                await Expect(locator).ToBeVisibleAsync();
                done = true;
            }
            var promise = Then();
            await page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await page.SetContentAsync("<iframe srcdoc=\"<input>\"></iframe>");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }

        [UpstreamTest("EX-050 with frameLocator 2")]
        public async Task WithFrameLocator2(IPage page)
        {
            await page.SetContentAsync("<iframe></iframe>");
            var locator = page.FrameLocator("iframe").Locator("input");
            var done = false;
            async Task Then()
            {
                await Expect(locator).ToBeVisibleAsync();
                done = true;
            }
            var promise = Then();
            await page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await page.SetContentAsync("<iframe srcdoc=\"<input>\"></iframe>");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }

        [UpstreamTest("EX-051 over navigation")]
        public async Task OverNavigation(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            var done = false;
            async Task Then()
            {
                await Expect(page.Locator("input")).ToBeVisibleAsync();
                done = true;
            }
            var promise = Then();
            await page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await page.GotoAsync(server.Prefix + "/input/checkbox.html");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }
    }

    public class ToBeHidden
    {
        [UpstreamTest("EX-052 default")]
        public async Task Default(IPage page)
        {
            await page.SetContentAsync("<button style=\"display: none\"></button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeHiddenAsync();
        }

        [UpstreamTest("EX-053 when nothing matches")]
        public async Task WhenNothingMatches(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeHiddenAsync();
        }

        [UpstreamTest("EX-054 with not")]
        public async Task WithNot(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            await Expect(locator).Not.ToBeHiddenAsync();
        }

        [UpstreamTest("EX-055 eventually with not")]
        public async Task EventuallyWithNot(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("span");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvalOnSelectorAsync("div", "div => div.innerHTML = '<span>Hello</span>'"));
            }, 0);
            await Expect(locator).Not.ToBeHiddenAsync();
        }

        [UpstreamTest("EX-056 eventually")]
        public async Task Eventually(IPage page)
        {
            await page.SetContentAsync("<div><span>Hello</span></div>");
            var locator = page.Locator("span");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvalOnSelectorAsync("span", "span => span.textContent = ''"));
            }, 0);
            await Expect(locator).ToBeHiddenAsync();
        }

        [UpstreamTest("EX-057 fail")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).ToBeHiddenAsync(new() { Timeout = 3000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to <input/>"));
        }

        [UpstreamTest("EX-058 fail with not")]
        public async Task FailWithNot(IPage page)
        {
            await page.SetContentAsync("<button style=\"display: none\"></button>");
            var locator = page.Locator("button");
            var error = await Catch(() => Expect(locator).Not.ToBeHiddenAsync(new() { Timeout = 3000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to <button></button>"));
        }

        [UpstreamTest("EX-059 fail with not when nothing matching")]
        [Fixme("dotnet: the message of Not.ToBeHiddenAsync is \"Locator expected not to be hidden\\nError: element(s) not found \\nCall log: ...\"; upstream expects \"expect(locator).not.toBeHidden() failed\" with the Locator, Expected and Timeout lines before the error")]
        public async Task FailWithNotWhenNothingMatching(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("button");
            var error = await Catch(() => Expect(locator).Not.ToBeHiddenAsync(new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(locator).not.toBeHidden() failed

Locator: locator('button')
Expected: not hidden
Timeout: 1000ms
Error: element(s) not found

Call log:
  - Expect ""not toBeHidden"" locator('button') with timeout 1000ms
"));
        }

        [UpstreamTest("EX-060 with impossible timeout .not")]
        public async Task WithImpossibleTimeoutNot(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            await Expect(page.Locator("#node")).Not.ToBeHiddenAsync(new() { Timeout = 1 });
        }

        [UpstreamTest("EX-061 with impossible timeout")]
        public async Task WithImpossibleTimeout(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            await Expect(page.Locator("no-such-thing")).ToBeHiddenAsync(new() { Timeout = 1 });
        }
    }

    [UpstreamTest("EX-062 toBeFocused")]
    public async Task ToBeFocused(IPage page)
    {
        await page.SetContentAsync("<input></input>");
        var locator = page.Locator("input");
        await locator.FocusAsync();
        await Expect(locator).ToBeFocusedAsync();
    }

    [UpstreamTest("EX-063 toBeFocused with shadow elements")]
    public async Task ToBeFocusedWithShadowElements(IPage page)
    {
        await page.SetContentAsync(@"
    <div id=""app"">
    </div>
    <script>
      const root = document.querySelector('div');
      const shadowRoot = root.attachShadow({ mode: 'open' });
      const input = document.createElement('input');
      input.id = ""my-input""
      shadowRoot.appendChild(input);
    </script>
  ");

        await page.Locator("input").FocusAsync();
        Assert.That(await page.EvaluateAsync<string>("() => document.activeElement.shadowRoot.activeElement.id"), Is.EqualTo("my-input"));
        await Expect(page.Locator("#app")).ToBeFocusedAsync();
        await Expect(page.Locator("input")).ToBeFocusedAsync();
    }

    [UpstreamTest("EX-064 should print unknown engine error")]
    public async Task ShouldPrintUnknownEngineError(IPage page)
    {
        var error = await Catch(() => Expect(page.Locator("row=\"row\"")).ToBeVisibleAsync());
        Assert.That(error.Message, Does.Contain("Unknown engine \"row\" while parsing selector row=\"row\""));
    }

    [UpstreamTest("EX-065 should print selector syntax error")]
    public async Task ShouldPrintSelectorSyntaxError(IPage page)
    {
        var error = await Catch(() => Expect(page.Locator("row]")).ToBeVisibleAsync());
        Assert.That(error.Message, Does.Contain("Unexpected token \"]\" while parsing css selector \"row]\""));
    }

    public class Describe
    {
        [BeforeEach]
        public static void Skip(bool isAndroid) => It.Skip(isAndroid, "server.EMPTY_PAGE is the emulator address 10.0.2.2");

        [BeforeEach]
        public static void Skip2(bool isElectron, int electronMajorVersion) => It.Skip(isElectron && electronMajorVersion < 30, "Protocol error (Storage.getCookies): Browser context management is not supported.");

        [UpstreamTest("EX-066 toBeOK")]
        public async Task ToBeOK(IPage page, Server server)
        {
            var res = await page.APIRequest.GetAsync(server.EmptyPage);
            await Expect(res).ToBeOKAsync();
        }

        [UpstreamTest("EX-067 not.toBeOK")]
        public async Task NotToBeOK(IPage page, Server server)
        {
            var res = await page.APIRequest.GetAsync($"{server.Prefix}/unknown");
            await Expect(res).Not.ToBeOKAsync();
        }

        public class ToBeOKShouldPrintResponseWithTextContentTypeWhenFails
        {
            [BeforeEach]
            public void BeforeEach(Server server)
            {
                server.SetRoute("/text-content-type", (req, res) =>
                {
                    res.StatusCode = 404;
                    res.SetHeader("Content-type", "text/plain");
                    res.End("Text error");
                });
                server.SetRoute("/no-content-type", (req, res) =>
                {
                    res.StatusCode = 404;
                    res.End("No content type error");
                });
                server.SetRoute("/binary-content-type", (req, res) =>
                {
                    res.StatusCode = 404;
                    res.SetHeader("Content-type", "image/bmp");
                    res.End("Image content type error");
                });
            }

            [UpstreamTest("EX-070 text content type")]
            [Fixme("dotnet: the message of ToBeOKAsync is \"Response status expected to be within [200..299] range, was 404\\nCall log: ...\"; upstream expects \"expect(response).toBeOK() failed\"")]
            public async Task TextContentType(IPage page, Server server)
            {
                var res = await page.APIRequest.GetAsync($"{server.Prefix}/text-content-type");
                var error = await Catch(() => Expect(res).ToBeOKAsync());
                Assert.That(StripAnsi(error.Message), Does.Contain("expect(response).toBeOK() failed"));
                Assert.That(error.Message, Does.Contain($"→ GET {server.Prefix}/text-content-type"));
                Assert.That(error.Message, Does.Contain("← 404 Not Found"));
                Assert.That(StripAnsi(error.Message), Does.Contain("Response text:\nText error"));
            }

            [UpstreamTest("EX-071 no content type")]
            public async Task NoContentType(IPage page, Server server)
            {
                var res = await page.APIRequest.GetAsync($"{server.Prefix}/no-content-type");
                var error = await Catch(() => Expect(res).ToBeOKAsync());
                Assert.That(error.Message, Does.Contain($"→ GET {server.Prefix}/no-content-type"));
                Assert.That(error.Message, Does.Contain("← 404 Not Found"));
                Assert.That(error.Message, Does.Not.Contain("No content type error"));
            }

            [UpstreamTest("EX-072 image content type")]
            public async Task ImageContentType(IPage page, Server server)
            {
                var res = await page.APIRequest.GetAsync($"{server.Prefix}/image-content-type");
                var error = await Catch(() => Expect(res).ToBeOKAsync());
                Assert.That(error.Message, Does.Contain($"→ GET {server.Prefix}/image-content-type"));
                Assert.That(error.Message, Does.Contain("← 404 Not Found"));
                Assert.That(error.Message, Does.Not.Contain("Image content type error"));
            }
        }
    }

    public class ToBeAttached
    {
        [UpstreamTest("EX-073 default")]
        public async Task Default(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeAttachedAsync();
        }

        [UpstreamTest("EX-074 with hidden element")]
        public async Task WithHiddenElement(IPage page)
        {
            await page.SetContentAsync("<button style=\"display:none\">hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeAttachedAsync();
        }

        [UpstreamTest("EX-075 with not")]
        public async Task WithNot(IPage page)
        {
            await page.SetContentAsync("<button>hello</button>");
            var locator = page.Locator("input");
            await Expect(locator).Not.ToBeAttachedAsync();
        }

        [UpstreamTest("EX-076 with attached:true")]
        public async Task WithAttachedTrue(IPage page)
        {
            await page.SetContentAsync("<button>hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).ToBeAttachedAsync(new() { Attached = true });
        }

        [UpstreamTest("EX-077 with attached:false")]
        public async Task WithAttachedFalse(IPage page)
        {
            await page.SetContentAsync("<button>hello</button>");
            var locator = page.Locator("input");
            await Expect(locator).ToBeAttachedAsync(new() { Attached = false });
        }

        [UpstreamTest("EX-078 with not and attached:false")]
        public async Task WithNotAndAttachedFalse(IPage page)
        {
            await page.SetContentAsync("<button>hello</button>");
            var locator = page.Locator("button");
            await Expect(locator).Not.ToBeAttachedAsync(new() { Attached = false });
        }

        [UpstreamTest("EX-079 eventually")]
        public async Task Eventually(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.Locator("span");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvalOnSelectorAsync("div", "div => div.innerHTML = '<span>Hello</span>'"));
            }, 0);
            await Expect(locator).ToBeAttachedAsync();
        }

        [UpstreamTest("EX-080 eventually with not")]
        public async Task EventuallyWithNot(IPage page)
        {
            await page.SetContentAsync("<div><span>Hello</span></div>");
            var locator = page.Locator("span");
            SetTimeout(() =>
            {
                IgnoreErrors(page.EvalOnSelectorAsync("div", "div => div.textContent = ''"));
            }, 0);
            await Expect(locator).Not.ToBeAttachedAsync();
        }

        [UpstreamTest("EX-081 fail")]
        public async Task Fail(IPage page)
        {
            await page.SetContentAsync("<button>Hello</button>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).ToBeAttachedAsync(new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Not.Contain("locator resolved to"));
        }

        [UpstreamTest("EX-082 fail with not")]
        public async Task FailWithNot(IPage page)
        {
            await page.SetContentAsync("<input></input>");
            var locator = page.Locator("input");
            var error = await Catch(() => Expect(locator).Not.ToBeAttachedAsync(new() { Timeout = 3000 }));
            Assert.That(error.Message, Does.Contain("locator resolved to <input/>"));
        }

        [UpstreamTest("EX-083 with impossible timeout")]
        public async Task WithImpossibleTimeout(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            await Expect(page.Locator("#node")).ToBeAttachedAsync(new() { Timeout = 1 });
        }

        [UpstreamTest("EX-084 with impossible timeout .not")]
        public async Task WithImpossibleTimeoutNot(IPage page)
        {
            await page.SetContentAsync("<div id=node>Text content</div>");
            await Expect(page.Locator("no-such-thing")).Not.ToBeAttachedAsync(new() { Timeout = 1 });
        }

        [UpstreamTest("EX-085 with frameLocator")]
        public async Task WithFrameLocator(IPage page)
        {
            await page.SetContentAsync("<div></div>");
            var locator = page.FrameLocator("iframe").Locator("input");
            var done = false;
            async Task Then()
            {
                await Expect(locator).ToBeAttachedAsync();
                done = true;
            }
            var promise = Then();
            await page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await page.SetContentAsync("<iframe srcdoc=\"<input>\"></iframe>");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }

        [UpstreamTest("EX-086 over navigation")]
        public async Task OverNavigation(IPage page, Server server)
        {
            await page.GotoAsync(server.EmptyPage);
            var done = false;
            async Task Then()
            {
                await Expect(page.Locator("input")).ToBeAttachedAsync();
                done = true;
            }
            var promise = Then();
            await page.WaitForTimeoutAsync(1000);
            Assert.That(done, Is.EqualTo(false));
            await page.GotoAsync(server.Prefix + "/input/checkbox.html");
            await promise;
            Assert.That(done, Is.EqualTo(true));
        }
    }
}
