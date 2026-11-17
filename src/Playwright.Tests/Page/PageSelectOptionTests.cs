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
public class PageSelectOptionTests
{
    private static Task GiveItAChanceToResolve(IPage page) => Rafraf(page, 5);

    [UpstreamTest("SO-001 should select single option @smoke")]
    public async Task ShouldSelectSingleOption(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", "blue");
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-002 should select single option by value")]
    public async Task ShouldSelectSingleOptionByValue(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new SelectOptionValue { Value = "blue" });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-003 should fall back to selecting by label")]
    public async Task ShouldFallBackToSelectingByLabel(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", "Blue");
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-004 should select single option by label")]
    public async Task ShouldSelectSingleOptionByLabel(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new SelectOptionValue { Label = "Indigo" });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "indigo" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "indigo" }));
    }

    [UpstreamTest("SO-005 should select single option by label with html whitespace")]
    public async Task ShouldSelectSingleOptionByLabelWithHtmlWhitespace(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new SelectOptionValue { Label = "HTML" });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "html" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "html" }));
    }

    [UpstreamTest("SO-006 should select single option by handle")]
    public async Task ShouldSelectSingleOptionByHandle(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", await page.QuerySelectorAsync("[id=whiteOption]"));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "white" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "white" }));
    }

    [UpstreamTest("SO-007 should select single option by index")]
    public async Task ShouldSelectSingleOptionByIndex(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new SelectOptionValue { Index = 2 });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "brown" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "brown" }));
    }

    [UpstreamTest("SO-008 should select single option by multiple attributes")]
    public async Task ShouldSelectSingleOptionByMultipleAttributes(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new SelectOptionValue { Value = "green", Label = "Green" });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "green" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "green" }));
    }

    [UpstreamTest("SO-009 should not select single option when some attributes do not match")]
    public async Task ShouldNotSelectSingleOptionWhenSomeAttributesDoNotMatch(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvalOnSelectorAsync("select", "s => s.value = undefined");
        try
        {
            await page.SelectOptionAsync("select", new SelectOptionValue { Value = "green", Label = "Brown" }, new() { Timeout = 1000 });
        }
        catch (Exception e)
        {
            Assert.That(e.Message, Does.Contain("Timeout"));
        }
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('select').value"), Is.EqualTo(""));
    }

    [UpstreamTest("SO-010 should select only first option")]
    public async Task ShouldSelectOnlyFirstOption(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new[] { "blue", "green", "red" });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-011 should not throw when select causes navigation")]
    public async Task ShouldNotThrowWhenSelectCausesNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvalOnSelectorAsync("select", "select => select.addEventListener('input', () => window.location.href = '/empty.html')");
        await PromiseAll(
            page.SelectOptionAsync("select", "blue"),
            page.WaitForNavigationAsync());
        Assert.That(page.Url, Does.Contain("empty.html"));
    }

    [UpstreamTest("SO-012 should select multiple options")]
    public async Task ShouldSelectMultipleOptions(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        await page.SelectOptionAsync("select", new[] { "blue", "green", "red" });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue", "green", "red" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue", "green", "red" }));
    }

    [UpstreamTest("SO-013 should select multiple options with attributes")]
    public async Task ShouldSelectMultipleOptionsWithAttributes(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        await page.SelectOptionAsync("select", new[] { new SelectOptionValue { Value = "blue" }, new SelectOptionValue { Label = "Green" }, new SelectOptionValue { Index = 4 } });
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue", "gray", "green" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue", "gray", "green" }));
    }

    [UpstreamTest("SO-014 should respect event bubbling")]
    public async Task ShouldRespectEventBubbling(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", "blue");
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onBubblingInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onBubblingChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-015 should throw when element is not a <select>")]
    public async Task ShouldThrowWhenElementIsNotASelect(IPage page, Server server)
    {
        Exception error = null;
        await page.GotoAsync(server.Prefix + "/input/select.html");
        error = await Catch(() => page.SelectOptionAsync("body", ""));
        Assert.That(error.Message, Does.Contain("Element is not a <select> element"));
    }

    [UpstreamTest("SO-016 should return [] on no matched values")]
    public async Task ShouldReturnOnNoMatchedValues(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        var result = await page.SelectOptionAsync("select", new string[] { });
        Assert.That(result, Is.EqualTo(new string[] { }));
    }

    [UpstreamTest("SO-017 should return an array of matched values")]
    public async Task ShouldReturnAnArrayOfMatchedValues(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        var result = await page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        Assert.That(result.Aggregate(true, (accumulator, current) => new[] { "blue", "black", "magenta" }.Contains(current) && accumulator), Is.EqualTo(true));
    }

    [UpstreamTest("SO-018 should return an array of one element when multiple is not set")]
    public async Task ShouldReturnAnArrayOfOneElementWhenMultipleIsNotSet(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        var result = await page.SelectOptionAsync("select", new[] { "42", "blue", "black", "magenta" });
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [UpstreamTest("SO-019 should return [] on no values")]
    public async Task ShouldReturnOnNoValues(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        var result = await page.SelectOptionAsync("select", new string[] { });
        Assert.That(result, Is.EqualTo(new string[] { }));
    }

    [UpstreamTest("SO-020 should not allow null items")]
    [LocalFixme("dotnet: SelectOptionAsync does not check the values for null and does not throw, upstream throws \"options[1]: expected object, got null\" in the client")]
    public async Task ShouldNotAllowNullItems(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        Exception error = null;
        error = await Catch(() => page.SelectOptionAsync("select", new[] { "blue", null, "black", "magenta" }));
        Assert.That(error.Message, Does.Contain("options[1]: expected object, got null"));
    }

    [UpstreamTest("SO-021 should unselect with null")]
    [LocalFixme("dotnet: SelectOptionAsync(selector, (string)null) sends one option without a value and options stay selected, and a null IEnumerable<string> throws ArgumentNullException; upstream sends no options for null, which deselects all")]
    public async Task ShouldUnselectWithNull(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        var result = await page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        Assert.That(result.Aggregate(true, (accumulator, current) => new[] { "blue", "black", "magenta" }.Contains(current) && accumulator), Is.EqualTo(true));
        // .NET: null has the type of one of the overloads.
        await page.SelectOptionAsync("select", (string)null);
        Assert.That(await page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"), Is.EqualTo(true));
    }

    [UpstreamTest("SO-022 should deselect all options when passed no values for a multiple select")]
    public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForAMultipleSelect(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        await page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        await page.SelectOptionAsync("select", new string[] { });
        Assert.That(await page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"), Is.EqualTo(true));
    }

    [UpstreamTest("SO-023 should deselect all options when passed no values for a select without multiple")]
    public async Task ShouldDeselectAllOptionsWhenPassedNoValuesForASelectWithoutMultiple(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.SelectOptionAsync("select", new[] { "blue", "black", "magenta" });
        await page.SelectOptionAsync("select", new string[] { });
        Assert.That(await page.EvalOnSelectorAsync<bool>("select", "select => Array.from(select.options).every(option => !option.selected)"), Is.EqualTo(true));
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/3327
    [UpstreamTest("SO-025 should work when re-defining top-level Event class")]
    public async Task ShouldWorkWhenReDefiningTopLevelEventClass(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window.Event = null");
        await page.SelectOptionAsync("select", "blue");
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("SO-026 should wait for option to be present")]
    public async Task ShouldWaitForOptionToBePresent(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        var selectPromise = page.SelectOptionAsync("select", "scarlet");
        var didSelect = false;
        async Task SelectThenDidSelect()
        {
            await selectPromise;
            didSelect = true;
        }
        IgnoreErrors(SelectThenDidSelect());
        await GiveItAChanceToResolve(page);
        Assert.That(didSelect, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("select", @"select => {
    const option = document.createElement('option');
    option.value = 'scarlet';
    option.textContent = 'Scarlet';
    select.appendChild(option);
  }");
        var items = await selectPromise;
        Assert.That(items, Is.EqualTo(new[] { "scarlet" }));
    }

    [UpstreamTest("SO-027 should wait for option index to be present")]
    public async Task ShouldWaitForOptionIndexToBePresent(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        var len = await page.EvalOnSelectorAsync<int>("select", "select => select.options.length");
        var selectPromise = page.SelectOptionAsync("select", new SelectOptionValue { Index = len });
        var didSelect = false;
        async Task SelectThenDidSelect()
        {
            await selectPromise;
            didSelect = true;
        }
        IgnoreErrors(SelectThenDidSelect());
        await GiveItAChanceToResolve(page);
        Assert.That(didSelect, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("select", @"select => {
    const option = document.createElement('option');
    option.value = 'scarlet';
    option.textContent = 'Scarlet';
    select.appendChild(option);
  }");
        var items = await selectPromise;
        Assert.That(items, Is.EqualTo(new[] { "scarlet" }));
    }

    [UpstreamTest("SO-028 should wait for multiple options to be present")]
    public async Task ShouldWaitForMultipleOptionsToBePresent(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/select.html");
        await page.EvaluateAsync("() => window['makeMultiple']()");
        var selectPromise = page.SelectOptionAsync("select", new[] { "green", "scarlet" });
        var didSelect = false;
        async Task SelectThenDidSelect()
        {
            await selectPromise;
            didSelect = true;
        }
        IgnoreErrors(SelectThenDidSelect());
        await GiveItAChanceToResolve(page);
        Assert.That(didSelect, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("select", @"select => {
    const option = document.createElement('option');
    option.value = 'scarlet';
    option.textContent = 'Scarlet';
    select.appendChild(option);
  }");
        var items = await selectPromise;
        Assert.That(items, Is.EqualTo(new[] { "green", "scarlet" }));
    }

    [UpstreamTest("SO-029 input event.composed should be true and cross shadow dom boundary")]
    public async Task InputEventComposedShouldBeTrueAndCrossShadowDomBoundary(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"<body><script>
  const div = document.createElement('div');
  const shadowRoot = div.attachShadow({mode: 'open'});
  shadowRoot.innerHTML = `<select>
    <option value=""black"">Black</option>
    <option value=""blue"">Blue</option>
  </select>`;
  document.body.appendChild(div);
</script></body>");
        await page.Locator("body").EvaluateAsync(@"select => {
    window.firedBodyEvents = [];
    for (const event of ['input', 'change']) {
      select.addEventListener(event, e => {
        window.firedBodyEvents.push(e.type + ':' + e.composed);
      }, false);
    }
  }");

        await page.Locator("select").EvaluateAsync(@"select => {
    window.firedEvents = [];
    for (const event of ['input', 'change']) {
      select.addEventListener(event, e => {
        window.firedEvents.push(e.type + ':' + e.composed);
      }, false);
    }
  }");
        await page.SelectOptionAsync("select", "blue");
        Assert.That(await page.EvaluateAsync<string[]>("() => window['firedEvents']"), Is.EqualTo(new[] { "input:true", "change:false" }));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['firedBodyEvents']"), Is.EqualTo(new[] { "input:true" }));
    }

    [UpstreamTest("SO-030 should wait for select to be enabled")]
    public async Task ShouldWaitForSelectToBeEnabled(IPage page)
    {
        await page.SetContentAsync(@"
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

        var selectPromise = page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
        await Expect(page.Locator("select")).ToHaveValueAsync("two");
    }

    [UpstreamTest("SO-031 should wait for option to be enabled")]
    public async Task ShouldWaitForOptionToBeEnabled(IPage page)
    {
        await page.SetContentAsync(@"
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

        var error = await Catch(() => page.Locator("select").SelectOptionAsync("two", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("option being selected is not enabled"));

        var selectPromise = page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
        await Expect(page.Locator("select")).ToHaveValueAsync("two");
    }

    [UpstreamTest("SO-032 should wait for optgroup to be enabled")]
    public async Task ShouldWaitForOptgroupToBeEnabled(IPage page)
    {
        await page.SetContentAsync(@"
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

        var error = await Catch(() => page.Locator("select").SelectOptionAsync("two", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("option being selected is not enabled"));

        var selectPromise = page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
        await Expect(page.Locator("select")).ToHaveValueAsync("two");
    }

    [UpstreamTest("SO-033 should wait for select to be swapped")]
    public async Task ShouldWaitForSelectToBeSwapped(IPage page)
    {
        await page.SetContentAsync(@"
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

        var selectPromise = page.Locator("select").SelectOptionAsync("two");
        await Task.Delay(1000);
        await page.EvaluateAsync("() => window.hydrate()");
        await selectPromise;
        await Expect(page.Locator("select")).ToHaveValueAsync("two");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("two"));
    }

    [UpstreamTest("SO-034 should wait for select to become non-inert")]
    public async Task ShouldWaitForSelectToBecomeNonInert(IPage page)
    {
        await page.SetContentAsync(@"
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
            await page.Locator("select").SelectOptionAsync("two");
            done = true;
        }
        var selectPromise = SelectThenDone();
        await GiveItAChanceToResolve(page);
        Assert.That(done, Is.EqualTo(false));
        await Expect(page.Locator("select")).ToHaveValueAsync("one");

        await page.EvaluateAsync("() => document.getElementById('container').removeAttribute('inert')");
        await selectPromise;
        await Expect(page.Locator("select")).ToHaveValueAsync("two");

        await page.EvaluateAsync("() => document.getElementById('container').setAttribute('inert', '')");
        var error = await Catch(() => page.Locator("select").SelectOptionAsync("one", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("element is inert"));
    }
}
