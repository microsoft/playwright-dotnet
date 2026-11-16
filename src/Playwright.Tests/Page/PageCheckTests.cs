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
public class PageCheckTests
{
    [UpstreamTest("CH-001 should check the box @smoke")]
    public async Task ShouldCheckTheBox(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await page.CheckAsync("input");
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-002 should not check the checked box")]
    public async Task ShouldNotCheckTheCheckedBox(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await page.CheckAsync("input");
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-003 should uncheck the box")]
    public async Task ShouldUncheckTheBox(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await page.UncheckAsync("input");
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-004 should not uncheck the unchecked box")]
    public async Task ShouldNotUncheckTheUncheckedBox(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await page.UncheckAsync("input");
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-005 should check radio")]
    public async Task ShouldCheckRadio(IPage page)
    {
        await page.SetContentAsync(@"
    <input type='radio'>one</input>
    <input id='two' type='radio'>two</input>
    <input type='radio'>three</input>");
        await page.CheckAsync("#two");
        Assert.That(await page.EvaluateAsync<bool>("() => window['two'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-006 should check radio by aria role")]
    public async Task ShouldCheckRadioByAriaRole(IPage page)
    {
        await page.SetContentAsync(@"<div role='radio' id='checkbox'>CHECKBOX</div>
    <script>
      checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
    </script>");
        await page.CheckAsync("div");
        Assert.That(await page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("true"));
    }

    [UpstreamTest("CH-007 should uncheck radio by aria role")]
    public async Task ShouldUncheckRadioByAriaRole(IPage page)
    {
        await page.SetContentAsync(@"<div role='radio' id='checkbox' aria-checked=""true"">CHECKBOX</div>
    <script>
      checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'false'));
    </script>");
        await page.UncheckAsync("div");
        Assert.That(await page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("false"));
    }

    [UpstreamTest("CH-008 should check the box by aria role")]
    public async Task ShouldCheckTheBoxByAriaRole(IPage page)
    {
        foreach (var role in new[] { "checkbox", "menuitemcheckbox", "option", "radio", "switch", "menuitemradio", "treeitem" })
        {
            await It.Step($"role={role}", async () =>
            {
                await page.SetContentAsync($@"<div role='{role}' id='checkbox'>CHECKBOX</div>
        <script>
          checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
        </script>");
                await page.CheckAsync("div");
                Assert.That(await page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("true"));
            });
        }
    }

    [UpstreamTest("CH-009 should uncheck the box by aria role")]
    public async Task ShouldUncheckTheBoxByAriaRole(IPage page)
    {
        foreach (var role in new[] { "checkbox", "menuitemcheckbox", "option", "radio", "switch", "menuitemradio", "treeitem" })
        {
            await It.Step($"role={role}", async () =>
            {
                await page.SetContentAsync($@"<div role='{role}' id='checkbox' aria-checked=""true"">CHECKBOX</div>
        <script>
          checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'false'));
        </script>");
                await page.UncheckAsync("div");
                Assert.That(await page.EvaluateAsync<string>("() => window['checkbox'].getAttribute('aria-checked')"), Is.EqualTo("false"));
            });
        }
    }

    [UpstreamTest("CH-010 should throw when not a checkbox")]
    public async Task ShouldThrowWhenNotACheckbox(IPage page)
    {
        await page.SetContentAsync("<div>Check me</div>");
        var error = await Catch(() => page.CheckAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }

    [UpstreamTest("CH-011 should throw when not a checkbox 2")]
    public async Task ShouldThrowWhenNotACheckbox2(IPage page)
    {
        await page.SetContentAsync("<div role=button>Check me</div>");
        var error = await Catch(() => page.CheckAsync("div"));
        Assert.That(error.Message, Does.Contain("Not a checkbox or radio button"));
    }

    [UpstreamTest("CH-012 should check the box inside a button")]
    public async Task ShouldCheckTheBoxInsideAButton(IPage page)
    {
        await page.SetContentAsync("<div role='button'><input type='checkbox'></div>");
        await page.CheckAsync("input");
        Assert.That(await page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(true));
        Assert.That(await page.IsCheckedAsync("input"), Is.EqualTo(true));
        Assert.That(await (await page.QuerySelectorAsync("input")).IsCheckedAsync(), Is.EqualTo(true));
    }

    [UpstreamTest("CH-013 should check the label with position")]
    public async Task ShouldCheckTheLabelWithPosition(IPage page, Server server)
    {
        await page.SetContentAsync($@"
    <input id='checkbox' type='checkbox' style='width: 5px; height: 5px;'>
    <label for='checkbox'>
      <a href={JsonSerializer.Serialize(server.EmptyPage)}>I am a long link that goes away so that nothing good will happen if you click on me</a>
      Click me
    </label>");
        var box = await (await page.QuerySelectorAsync("text=Click me")).BoundingBoxAsync();
        await page.CheckAsync("text=Click me", new() { Position = new() { X = box.Width - 10, Y = 2 } });
        Assert.That(await page.EvalOnSelectorAsync<bool>("input", "input => input.checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-014 trial run should not check")]
    public async Task TrialRunShouldNotCheck(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await page.CheckAsync("input", new() { Trial = true });
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-015 trial run should not uncheck")]
    public async Task TrialRunShouldNotUncheck(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await page.UncheckAsync("input", new() { Trial = true });
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
    }

    [UpstreamTest("CH-016 should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked(IPage page)
    {
        await page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await page.SetCheckedAsync("input", true);
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(true));
        await page.SetCheckedAsync("input", false);
        Assert.That(await page.EvaluateAsync<bool>("() => window['checkbox'].checked"), Is.EqualTo(false));
    }

    [UpstreamTest("CH-017 should throw when trying to uncheck radio button")]
    public async Task ShouldThrowWhenTryingToUncheckRadioButton(IPage page)
    {
        await page.SetContentAsync("<input type='radio' name='test' checked id='radio'>");
        var error = await Catch(() => page.UncheckAsync("#radio"));
        Assert.That(error.Message, Does.Contain("Cannot uncheck radio button"));
    }
}
