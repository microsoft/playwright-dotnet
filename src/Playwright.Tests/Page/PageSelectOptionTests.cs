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

/// <summary>Port of tests/page/page-select-option.spec.ts.</summary>
public class PageSelectOptionTests : UpstreamPageTest
{
    private static Task GiveItAChanceToResolve(IPage page) => Rafraf(page, 5);

    [UpstreamTest("SO-001 should select single option @smoke")]
    public async Task ShouldSelectSingleOption()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", "blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-002 should select single option by value")]
    public async Task ShouldSelectSingleOptionByValue()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new SelectOptionValue { Value = "blue" });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-003 should fall back to selecting by label")]
    public async Task ShouldFallBackToSelectingByLabel()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", "Blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-004 should select single option by label")]
    public async Task ShouldSelectSingleOptionByLabel()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new SelectOptionValue { Label = "Indigo" });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "indigo" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "indigo" }));
    }

    [UpstreamTest("SO-005 should select single option by label with html whitespace")]
    public async Task ShouldSelectSingleOptionByLabelWithHtmlWhitespace()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new SelectOptionValue { Label = "HTML" });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "html" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "html" }));
    }

    [UpstreamTest("SO-006 should select single option by handle")]
    public async Task ShouldSelectSingleOptionByHandle()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", await Page.QuerySelectorAsync("[id=whiteOption]"));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "white" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "white" }));
    }

    [UpstreamTest("SO-007 should select single option by index")]
    public async Task ShouldSelectSingleOptionByIndex()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new SelectOptionValue { Index = 2 });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "brown" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "brown" }));
    }

    [UpstreamTest("SO-008 should select single option by multiple attributes")]
    public async Task ShouldSelectSingleOptionByMultipleAttributes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new SelectOptionValue { Value = "green", Label = "Green" });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "green" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "green" }));
    }

    [UpstreamTest("SO-009 should not select single option when some attributes do not match")]
    public async Task ShouldNotSelectSingleOptionWhenSomeAttributesDoNotMatch()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvalOnSelectorAsync("select", "s => s.value = undefined");
        try
        {
            await Page.SelectOptionAsync("select", new SelectOptionValue { Value = "green", Label = "Brown" }, new() { Timeout = 1000 });
        }
        catch (Exception e)
        {
            Assert.That(e.Message, Does.Contain("Timeout"));
        }
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('select').value"), Is.EqualTo(""));
    }

    [UpstreamTest("SO-010 should select only first option")]
    public async Task ShouldSelectOnlyFirstOption()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new[] { "blue", "green", "red" });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-011 should not throw when select causes navigation")]
    public async Task ShouldNotThrowWhenSelectCausesNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvalOnSelectorAsync("select", "select => select.addEventListener('input', () => window.location.href = '/empty.html')");
        await PromiseAll(
            Page.SelectOptionAsync("select", "blue"),
            Page.WaitForNavigationAsync());
        Assert.That(Page.Url, Does.Contain("empty.html"));
    }

    [UpstreamTest("SO-012 should select multiple options")]
    public async Task ShouldSelectMultipleOptions()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        await Page.SelectOptionAsync("select", new[] { "blue", "green", "red" });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue", "green", "red" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue", "green", "red" }));
    }

    [UpstreamTest("SO-013 should select multiple options with attributes")]
    public async Task ShouldSelectMultipleOptionsWithAttributes()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        await Page.SelectOptionAsync("select", new[] { new SelectOptionValue { Value = "blue" }, new SelectOptionValue { Label = "Green" }, new SelectOptionValue { Index = 4 } });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue", "gray", "green" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue", "gray", "green" }));
    }

    [UpstreamTest("SO-014 should respect event bubbling")]
    public async Task ShouldRespectEventBubbling()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", "blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onBubblingInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onBubblingChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-015 should throw when element is not a <select>")]
    public async Task ShouldThrowWhenElementIsNotASelect()
    {
        Exception error = null;
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        error = await Catch(() => Page.SelectOptionAsync("body", ""));
        Assert.That(error.Message, Does.Contain("Element is not a <select> element"));
    }

    [UpstreamTest("SO-016 should return [] on no matched values")]
    public async Task ShouldReturnOnNoMatchedValues()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var result = await Page.SelectOptionAsync("select", new string[] { });
        Assert.That(result, Is.EqualTo(new string[] { }));
    }

    [UpstreamTest("SO-017 should return an array of matched values")]
    public async Task ShouldReturnAnArrayOfMatchedValues()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        var result = await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        Assert.That(result.Aggregate(true, (accumulator, current) => new[] { "blue", "black", "magenta" }.Contains(current) && accumulator), Is.EqualTo(true));
    }

    [UpstreamTest("SO-018 should return an array of one element when multiple is not set")]
    public async Task ShouldReturnAnArrayOfOneElementWhenMultipleIsNotSet()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var result = await Page.SelectOptionAsync("select", new[] { "42", "blue", "black", "magenta" });
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [UpstreamTest("SO-019 should return [] on no values")]
    public async Task ShouldReturnOnNoValues()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var result = await Page.SelectOptionAsync("select", new string[] { });
        Assert.That(result, Is.EqualTo(new string[] { }));
    }

    [UpstreamTest("SO-020 should not allow null items")]
    public async Task ShouldNotAllowNullItems()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        Exception error = null;
        error = await Catch(() => Page.SelectOptionAsync("select", new[] { "blue", null, "black", "magenta" }));
        Assert.That(error.Message, Does.Contain("options[1]: expected object, got null"));
    }

    [UpstreamTest("SO-021 should unselect with null")]
    public async Task ShouldUnselectWithNull()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        var result = await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        Assert.That(result.Aggregate(true, (accumulator, current) => new[] { "blue", "black", "magenta" }.Contains(current) && accumulator), Is.EqualTo(true));
        // .NET: null has the type of one of the overloads.
        await Page.SelectOptionAsync("select", (string)null);
        Assert.That(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"), Is.EqualTo(true));
    }

    [UpstreamTest("SO-022 should deselect all options when passed no values for a multiple select")]
    public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForAMultipleSelect()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        await Page.SelectOptionAsync("select", new string[] { });
        Assert.That(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"), Is.EqualTo(true));
    }

    [UpstreamTest("SO-023 should deselect all options when passed no values for a select without multiple")]
    public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForASelectWithoutMultiple()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        await Page.SelectOptionAsync("select", new string[] { });
        Assert.That(await Page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"), Is.EqualTo(true));
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/3327
    [UpstreamTest("SO-025 should work when re-defining top-level Event class")]
    public async Task ShouldWorkWhenReDefiningTopLevelEventClass()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window.Event = null");
        await Page.SelectOptionAsync("select", "blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-026 should wait for option to be present")]
    public async Task ShouldWaitForOptionToBePresent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var selectPromise = Page.SelectOptionAsync("select", "scarlet");
        var didSelect = false;
        async Task SelectThenDidSelect()
        {
            await selectPromise;
            didSelect = true;
        }
        IgnoreErrors(SelectThenDidSelect());
        await GiveItAChanceToResolve(Page);
        Assert.That(didSelect, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("select", @"select => {
    const option = document.createElement('option');
    option.value = 'scarlet';
    option.textContent = 'Scarlet';
    select.appendChild(option);
  }");
        var items = await selectPromise;
        Assert.That(items, Is.EqualTo(new[] { "scarlet" }));
    }

    [UpstreamTest("SO-027 should wait for option index to be present")]
    public async Task ShouldWaitForOptionIndexToBePresent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var len = await Page.EvalOnSelectorAsync<int>("select", "select => select.options.length");
        var selectPromise = Page.SelectOptionAsync("select", new SelectOptionValue { Index = len });
        var didSelect = false;
        async Task SelectThenDidSelect()
        {
            await selectPromise;
            didSelect = true;
        }
        IgnoreErrors(SelectThenDidSelect());
        await GiveItAChanceToResolve(Page);
        Assert.That(didSelect, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("select", @"select => {
    const option = document.createElement('option');
    option.value = 'scarlet';
    option.textContent = 'Scarlet';
    select.appendChild(option);
  }");
        var items = await selectPromise;
        Assert.That(items, Is.EqualTo(new[] { "scarlet" }));
    }

    [UpstreamTest("SO-028 should wait for multiple options to be present")]
    public async Task ShouldWaitForMultipleOptionsToBePresent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        await Page.EvaluateAsync("() => window['makeMultiple']()");
        var selectPromise = Page.SelectOptionAsync("select", new[] { "green", "scarlet" });
        var didSelect = false;
        async Task SelectThenDidSelect()
        {
            await selectPromise;
            didSelect = true;
        }
        IgnoreErrors(SelectThenDidSelect());
        await GiveItAChanceToResolve(Page);
        Assert.That(didSelect, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("select", @"select => {
    const option = document.createElement('option');
    option.value = 'scarlet';
    option.textContent = 'Scarlet';
    select.appendChild(option);
  }");
        var items = await selectPromise;
        Assert.That(items, Is.EqualTo(new[] { "green", "scarlet" }));
    }

    [UpstreamTest("SO-029 input event.composed should be true and cross shadow dom boundary")]
    public async Task InputEventComposedShouldBeTrueAndCrossShadowDomBoundary()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"<body><script>
  const div = document.createElement('div');
  const shadowRoot = div.attachShadow({mode: 'open'});
  shadowRoot.innerHTML = `<select>
    <option value=""black"">Black</option>
    <option value=""blue"">Blue</option>
  </select>`;
  document.body.appendChild(div);
</script></body>");
        await Page.Locator("body").EvaluateAsync(@"select => {
    window.firedBodyEvents = [];
    for (const event of ['input', 'change']) {
      select.addEventListener(event, e => {
        window.firedBodyEvents.push(e.type + ':' + e.composed);
      }, false);
    }
  }");

        await Page.Locator("select").EvaluateAsync(@"select => {
    window.firedEvents = [];
    for (const event of ['input', 'change']) {
      select.addEventListener(event, e => {
        window.firedEvents.push(e.type + ':' + e.composed);
      }, false);
    }
  }");
        await Page.SelectOptionAsync("select", "blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['firedEvents']"), Is.EqualTo(new[] { "input:true", "change:false" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['firedBodyEvents']"), Is.EqualTo(new[] { "input:true" }));
    }

    [UpstreamTest("SO-030 should wait for select to be enabled")]
    public async Task ShouldWaitForSelectToBeEnabled()
    {
        await Page.SetContentAsync(@"
    <select disabled>
      <option>one</option>
      <option>two</option>
    </select>

    <script>
    function hydrate() {
      const select = document.querySelector('select');
      select.removeAttribute('disabled');
      select.addEventListener('change', () => {
        window['result'] = select.value;
      });
    }
    </script>
  ");

        var selectPromise = Page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await Page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
        await Expect(Page.Locator("select")).ToHaveValueAsync("two");
    }

    [UpstreamTest("SO-031 should wait for option to be enabled")]
    public async Task ShouldWaitForOptionToBeEnabled()
    {
        await Page.SetContentAsync(@"
    <select>
      <option>one</option>
      <option disabled id=myoption>two</option>
    </select>

    <script>
    function hydrate() {
      const option = document.querySelector('#myoption');
      option.removeAttribute('disabled');
      const select = document.querySelector('select');
      select.addEventListener('change', () => {
        window['result'] = select.value;
      });
    }
    </script>
  ");

        var error = await Catch(() => Page.Locator("select").SelectOptionAsync("two", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("option being selected is not enabled"));

        var selectPromise = Page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await Page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
        await Expect(Page.Locator("select")).ToHaveValueAsync("two");
    }

    [UpstreamTest("SO-032 should wait for optgroup to be enabled")]
    public async Task ShouldWaitForOptgroupToBeEnabled()
    {
        await Page.SetContentAsync(@"
    <select>
      <option>one</option>
      <optgroup label=""Group"" disabled id=mygroup>
        <option>two</option>
      </optgroup>
    </select>

    <script>
    function hydrate() {
      const group = document.querySelector('#mygroup');
      group.removeAttribute('disabled');
      const select = document.querySelector('select');
      select.addEventListener('change', () => {
        window['result'] = select.value;
      });
    }
    </script>
  ");

        var error = await Catch(() => Page.Locator("select").SelectOptionAsync("two", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("option being selected is not enabled"));

        var selectPromise = Page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await Page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
        await Expect(Page.Locator("select")).ToHaveValueAsync("two");
    }

    [UpstreamTest("SO-033 should wait for select to be swapped")]
    public async Task ShouldWaitForSelectToBeSwapped()
    {
        await Page.SetContentAsync(@"
    <select disabled>
      <option>one</option>
      <option>two</option>
    </select>

    <script>
    function hydrate() {
      const select = document.querySelector('select');
      select.remove();

      const newSelect = document.createElement('select');
      const option1 = document.createElement('option');
      option1.textContent = 'one';
      newSelect.appendChild(option1);
      const option2 = document.createElement('option');
      option2.textContent = 'two';
      newSelect.appendChild(option2);

      document.body.appendChild(newSelect);

      newSelect.addEventListener('change', () => {
        window['result'] = newSelect.value;
      });
    }
    </script>
  ");

        var selectPromise = Page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await Page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        await Expect(Page.Locator("select")).ToHaveValueAsync("two");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
    }

    [UpstreamTest("SO-034 should wait for select to become non-inert")]
    public async Task ShouldWaitForSelectToBecomeNonInert()
    {
        await Page.SetContentAsync(@"
    <div inert id=container>
      <select>
        <option>one</option>
        <option>two</option>
      </select>
    </div>
  ");

        var done = false;
        async Task SelectThenDone()
        {
            await Page.Locator("select").SelectOptionAsync("two");
            done = true;
        }
        var selectPromise = SelectThenDone();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await Expect(Page.Locator("select")).ToHaveValueAsync("one");

        await Page.EvaluateAsync("() => document.getElementById('container').removeAttribute('inert')");
        await selectPromise;
        await Expect(Page.Locator("select")).ToHaveValueAsync("two");

        await Page.EvaluateAsync("() => document.getElementById('container').setAttribute('inert', '')");
        var error = await Catch(() => Page.Locator("select").SelectOptionAsync("one", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("element is inert"));
    }
}
