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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-is-visible.spec.ts.</summary>
public class LocatorIsVisibleTests
{
    [UpstreamTest("LO-115 isVisible and isHidden should work")]
    public async Task IsVisibleAndIsHiddenShouldWork(IPage page)
    {
        await page.SetContentAsync("<div>Hi</div><span></span>");

        var div = page.Locator("div");
        Assert.That(await div.IsVisibleAsync(), Is.EqualTo(true));
        Assert.That(await div.IsHiddenAsync(), Is.EqualTo(false));
        Assert.That(await page.IsVisibleAsync("div"), Is.EqualTo(true));
        Assert.That(await page.IsHiddenAsync("div"), Is.EqualTo(false));

        var span = page.Locator("span");
        Assert.That(await span.IsVisibleAsync(), Is.EqualTo(false));
        Assert.That(await span.IsHiddenAsync(), Is.EqualTo(true));
        Assert.That(await page.IsVisibleAsync("span"), Is.EqualTo(false));
        Assert.That(await page.IsHiddenAsync("span"), Is.EqualTo(true));

        Assert.That(await page.IsVisibleAsync("no-such-element"), Is.EqualTo(false));
        Assert.That(await page.IsHiddenAsync("no-such-element"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-116 isVisible should be true for opacity:0")]
    public async Task IsVisibleShouldBeTrueForOpacity0(IPage page)
    {
        await page.SetContentAsync("<div style=\"opacity:0\">Hi</div>");
        await Expect(page.Locator("div")).ToBeVisibleAsync();
    }

    [UpstreamTest("LO-117 isVisible should be true for element outside view")]
    public async Task IsVisibleShouldBeTrueForElementOutsideView(IPage page)
    {
        await page.SetContentAsync("<div style=\"position: absolute; left: -1000px\">Hi</div>");
        await Expect(page.Locator("div")).ToBeVisibleAsync();
    }

    [UpstreamTest("LO-118 isVisible and isHidden should work with details")]
    public async Task IsVisibleAndIsHiddenShouldWorkWithDetails(IPage page)
    {
        await page.SetContentAsync(@"<details>
    <summary>click to open</summary>
      <ul>
        <li>hidden item 1</li>
        <li>hidden item 2</li>
        <li>hidden item 3</li>
      </ul
  </details>");

        await Expect(page.Locator("ul")).ToBeHiddenAsync();
    }

    [UpstreamTest("LO-119 isVisible and isHidden should work with nested details")]
    public async Task IsVisibleAndIsHiddenShouldWorkWithNestedDetails(IPage page)
    {
        await page.SetContentAsync(@"<details>
    <summary>outer</summary>
    <details open>
      <summary>inner</summary>
      <button>hidden button</button>
    </details>
  </details>");

        await Expect(page.Locator("summary", new() { HasText = "outer" })).ToBeVisibleAsync();
        await Expect(page.Locator("summary", new() { HasText = "inner" })).ToBeHiddenAsync();
        await Expect(page.Locator("button")).ToBeHiddenAsync();
        await Expect(page.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [UpstreamTest("LO-120 isVisible and isHidden should work with unslotted content")]
    public async Task IsVisibleAndIsHiddenShouldWorkWithUnslottedContent(IPage page)
    {
        await page.SetContentAsync(@"
    <div id=unslotted><span>hidden 1</span><div><span>hidden 2</span></div></div>
    <div id=slotted><span>visible</span></div>
    <script>
      unslotted.attachShadow({ mode: 'open' }).innerHTML = 'nothing to see here';
      slotted.attachShadow({ mode: 'open' }).innerHTML = '<slot></slot>';
    </script>
  ");

        await Expect(page.Locator("span", new() { HasText = "hidden 1" })).ToBeHiddenAsync();
        await Expect(page.Locator("span", new() { HasText = "hidden 2" })).ToBeHiddenAsync();
        await Expect(page.Locator("span", new() { HasText = "visible" })).ToBeVisibleAsync();
    }

    [UpstreamTest("LO-121 isVisible and isHidden should work with list box options")]
    public async Task IsVisibleAndIsHiddenShouldWorkWithListBoxOptions(IPage page, bool isAndroid)
    {
        It.Skip(isAndroid, "Android Chrome renders list boxes as popups, so options are not visible in the page");
        await page.SetContentAsync(@"
    <select multiple>
      <option>visible 1</option>
      <optgroup label=""group""><option>visible 2</option></optgroup>
      <option hidden>hidden 1</option>
      <optgroup label=""hidden group"" style=""display:none""><option>hidden 2</option></optgroup>
    </select>
    <select size=3><option>visible 3</option></select>
    <select multiple style=""display:none""><option>hidden 3</option></select>
  ");

        foreach (var name in new[] { "visible 1", "visible 2", "visible 3" })
        {
            await Expect(page.GetByRole(AriaRole.Option, new() { Name = name, Exact = true })).ToBeVisibleAsync();
        }
        foreach (var text in new[] { "hidden 1", "hidden 2", "hidden 3" })
        {
            await Expect(page.Locator("option", new() { HasText = text })).ToBeHiddenAsync();
        }
    }

    [UpstreamTest("LO-122 isVisible inside a button")]
    public async Task IsVisibleInsideAButton(IPage page)
    {
        await page.SetContentAsync("<button><span></span>a button</button>");
        var span = page.Locator("span");
        Assert.That(await span.IsVisibleAsync(), Is.EqualTo(false));
        Assert.That(await span.IsHiddenAsync(), Is.EqualTo(true));
        Assert.That(await page.IsVisibleAsync("span"), Is.EqualTo(false));
        Assert.That(await page.IsHiddenAsync("span"), Is.EqualTo(true));
        await Expect(span).Not.ToBeVisibleAsync();
        await Expect(span).ToBeHiddenAsync();
        await span.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await page.Locator("button").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    [UpstreamTest("LO-123 isVisible inside a role=button")]
    public async Task IsVisibleInsideARoleButton(IPage page)
    {
        await page.SetContentAsync("<div role=button><span></span>a button</div>");
        var span = page.Locator("span");
        Assert.That(await span.IsVisibleAsync(), Is.EqualTo(false));
        Assert.That(await span.IsHiddenAsync(), Is.EqualTo(true));
        Assert.That(await page.IsVisibleAsync("span"), Is.EqualTo(false));
        Assert.That(await page.IsHiddenAsync("span"), Is.EqualTo(true));
        await Expect(span).Not.ToBeVisibleAsync();
        await Expect(span).ToBeHiddenAsync();
        await span.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await page.Locator("[role=button]").WaitForAsync(new() { State = WaitForSelectorState.Visible });
    }

    [UpstreamTest("LO-124 isVisible during navigation should not throw")]
    public async Task IsVisibleDuringNavigationShouldNotThrow(IPage page, Server server)
    {
        for (var i = 0; i < 20; i++)
        {
            await Catch(() => page.SetContentAsync($@"
      <script>
        setTimeout(() => {{
          window.location.href = {JsonSerializer.Serialize(server.EmptyPage)};
        }}, Math.random(50));
      </script>
    "));  // Avoid page.setContent throwing because of scheduled navigation.
            Assert.That(await page.Locator("div").IsVisibleAsync(), Is.EqualTo(false));
        }
    }

    [UpstreamTest("LO-125 isVisible with invalid selector should throw")]
    public async Task IsVisibleWithInvalidSelectorShouldThrow(IPage page)
    {
        var error = await Catch(() => page.Locator("hey=what").IsVisibleAsync());
        Assert.That(error.Message, Does.Contain("Unknown engine \"hey\" while parsing selector hey=what"));
    }
}
