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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-check.spec.ts.</summary>
public class PageCheckTests : UpstreamPageTest
{
    [UpstreamTest("CH-001 should check the box @smoke")]
    public async Task ShouldCheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.CheckAsync("input");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-002 should not check the checked box")]
    public async Task ShouldNotCheckTheCheckedBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await Page.CheckAsync("input");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-003 should uncheck the box")]
    public async Task ShouldUncheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await Page.UncheckAsync("input");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-004 should not uncheck the unchecked box")]
    public async Task ShouldNotUncheckTheUncheckedBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.UncheckAsync("input");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-005 should check radio")]
    public async Task ShouldCheckRadio()
    {
        await Page.SetContentAsync(@"
    <input type='radio'>one</input>
    <input id='two' type='radio'>two</input>
    <input type='radio'>three</input>");
        await Page.CheckAsync("#two");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['two'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-006 should check radio by aria role")]
    public async Task ShouldCheckRadioByAriaRole()
    {
        await Page.SetContentAsync(@"<div role='radio' id='checkbox'>CHECKBOX</div>
    <script>
      checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
    </script>");
        await Page.CheckAsync("div");
        Assert.That(await Page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("true"));
    }

    [UpstreamTest("CH-007 should uncheck radio by aria role")]
    public async Task ShouldUncheckRadioByAriaRole()
    {
        await Page.SetContentAsync(@"<div role='radio' id='checkbox' aria-checked=""true"">CHECKBOX</div>
    <script>
      checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'false'));
    </script>");
        await Page.UncheckAsync("div");
        Assert.That(await Page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("false"));
    }

    [UpstreamTest("CH-008 should check the box by aria role")]
    public async Task ShouldCheckTheBoxByAriaRole()
    {
        foreach (var role in new[] { "checkbox", "menuitemcheckbox", "option", "radio", "switch", "menuitemradio", "treeitem" })
        {
            await Info.Step($"role={role}", async () =>
            {
                await Page.SetContentAsync($@"<div role='{role}' id='checkbox'>CHECKBOX</div>
        <script>
          checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
        </script>");
                await Page.CheckAsync("div");
                Assert.That(await Page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("true"));
            });
        }
    }

    [UpstreamTest("CH-009 should uncheck the box by aria role")]
    public async Task ShouldUncheckTheBoxByAriaRole()
    {
        foreach (var role in new[] { "checkbox", "menuitemcheckbox", "option", "radio", "switch", "menuitemradio", "treeitem" })
        {
            await Info.Step($"role={role}", async () =>
            {
                await Page.SetContentAsync($@"<div role='{role}' id='checkbox' aria-checked=""true"">CHECKBOX</div>
        <script>
          checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'false'));
        </script>");
                await Page.UncheckAsync("div");
                Assert.That(await Page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("false"));
            });
        }
    }

    [UpstreamTest("CH-010 should throw when not a checkbox")]
    public async Task ShouldThrowWhenNotACheckbox()
    {
        await Page.SetContentAsync("<div>Check me</div>");
        var error = await Catch(() => Page.CheckAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }

    [UpstreamTest("CH-011 should throw when not a checkbox 2")]
    public async Task ShouldThrowWhenNotACheckbox2()
    {
        await Page.SetContentAsync("<div role=button>Check me</div>");
        var error = await Catch(() => Page.CheckAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }

    [UpstreamTest("CH-012 should check the box inside a button")]
    public async Task ShouldCheckTheBoxInsideAButton()
    {
        await Page.SetContentAsync("<div role='button'><input type='checkbox'></div>");
        await Page.CheckAsync("input");
        Assert.That(await Page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(true));
        Assert.That(await Page.IsCheckedAsync("input"), Is.EqualTo(true));
        Assert.That(await (await Page.QuerySelectorAsync("input")).IsCheckedAsync(), Is.EqualTo(true));
    }

    [UpstreamTest("CH-013 should check the label with position")]
    public async Task ShouldCheckTheLabelWithPosition()
    {
        await Page.SetContentAsync($@"
    <input id='checkbox' type='checkbox' style='width: 5px; height: 5px;'>
    <label for='checkbox'>
      <a href={JsonSerializer.Serialize(Server.EmptyPage)}>I am a long link that goes away so that nothing good will happen if you click on me</a>
      Click me
    </label>");
        var box = await (await Page.QuerySelectorAsync("text=Click me")).BoundingBoxAsync();
        await Page.CheckAsync("text=Click me", new() { Position = new() { X = box.Width - 10, Y = 2 } });
        Assert.That(await Page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-014 trial run should not check")]
    public async Task TrialRunShouldNotCheck()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.CheckAsync("input", new() { Trial = true });
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-015 trial run should not uncheck")]
    public async Task TrialRunShouldNotUncheck()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await Page.UncheckAsync("input", new() { Trial = true });
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-016 should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.SetCheckedAsync("input", true);
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
        await Page.SetCheckedAsync("input", false);
        Assert.That(await Page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-017 should throw when trying to uncheck radio button")]
    public async Task ShouldThrowWhenTryingToUncheckRadioButton()
    {
        await Page.SetContentAsync("<input type='radio' name='test' checked id='radio'>");
        var error = await Catch(() => Page.UncheckAsync("#radio"));
        Assert.That(error.Message, Does.Contain("Cannot uncheck radio button"));
    }
}
