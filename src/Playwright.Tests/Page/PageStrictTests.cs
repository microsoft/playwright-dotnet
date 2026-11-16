/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

/// <summary>Port of tests/page/page-strict.spec.ts.</summary>
public class PageStrictTests
{
    [UpstreamTest("SM-001 should fail page.textContent in strict mode")]
    [Fixme("dotnet: the message has the locators in C# form, 'aka GetByText(\"span1\")', upstream expects \"aka getByText('span1')\"")]
    public async Task ShouldFailPageTextContentInStrictMode(IPage page)
    {
        await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
        var error = await Catch(() => page.TextContentAsync("span", new() { Strict = true }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("1) <span>span1</span> aka getByText('span1')"));
        Assert.That(error.Message, Does.Contain("2) <span>target</span> aka getByText('target')"));
    }

    [UpstreamTest("SM-002 should fail page.getAttribute in strict mode")]
    public async Task ShouldFailPageGetAttributeInStrictMode(IPage page)
    {
        await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
        var error = await Catch(() => page.GetAttributeAsync("span", "id", new() { Strict = true }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
    }

    [UpstreamTest("SM-003 should fail page.fill in strict mode")]

    [Fixme("dotnet: the message has the locators in C# form, 'aka GetByRole(AriaRole.Textbox).First', upstream expects \"aka getByRole('textbox').first()\"")]
    public async Task ShouldFailPageFillInStrictMode(IPage page)
    {
        await page.SetContentAsync("<input></input><div><input></input></div>");
        var error = await Catch(() => page.FillAsync("input", "text", new() { Strict = true }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("1) <input/> aka getByRole('textbox').first()"));
        Assert.That(error.Message, Does.Contain("2) <input/> aka locator('div').getByRole('textbox')"));
    }

    [UpstreamTest("SM-004 should fail page.$ in strict mode")]
    public async Task ShouldFailPageInStrictMode(IPage page)
    {
        await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
        var error = await Catch(() => page.QuerySelectorAsync("span", new() { Strict = true }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
    }

    [UpstreamTest("SM-005 should fail page.waitForSelector in strict mode")]
    public async Task ShouldFailPageWaitForSelectorInStrictMode(IPage page)
    {
        await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
        var error = await Catch(() => page.WaitForSelectorAsync("span", new() { Strict = true }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
    }

    [UpstreamTest("SM-006 should fail page.dispatchEvent in strict mode")]

    [Fixme("dotnet: the message has the locators in C# form, 'aka Locator(\"span\").First', upstream expects \"aka locator('span').first()\"")]
    public async Task ShouldFailPageDispatchEventInStrictMode(IPage page)
    {
        await page.SetContentAsync("<span></span><div><span></span></div>");
        var error = await Catch(() => page.DispatchEventAsync("span", "click", new { }, new() { Strict = true }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("1) <span></span> aka locator('span').first()"));
        Assert.That(error.Message, Does.Contain("2) <span></span> aka locator('div span')"));
    }

    [UpstreamTest("SM-007 should properly format :nth-child() in strict mode message")]
    public async Task ShouldProperlyFormatNthChildInStrictModeMessage(IPage page)
    {
        await page.SetContentAsync(@"
  <div>
    <div>
    </div>
    <div>
      <div>
      </div>
      <div>
      </div>
    </div>
  </div>
  <div>
    <div class='foo'>
    </div>
    <div class='foo'>
    </div>
  </div>
  ");
        var error = await Catch(() => page.Locator(".foo").HoverAsync());
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        // Second div has body > div:nth-child(2) > div:nth-child(2) selector which would be ambiguous
        // if '>' were ' '.
        Assert.That(error.Message, Does.Contain("body > div:nth-child(2) > div:nth-child(2)"));
    }

    [UpstreamTest("SM-008 should escape class names")]
    public async Task ShouldEscapeClassNames(IPage page)
    {
        await page.SetContentAsync(@"
  <div>
    <div></div>
    <div>
      <div></div>
      <div></div>
    </div>
  </div>
  <div>
    <div class='foo bar:0'>
    </div>
    <div class='foo bar:1'>
    </div>
  </div>
  ");
        var error = await Catch(() => page.Locator(".foo").HoverAsync());
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("<div class=\"foo bar:0"));
        Assert.That(error.Message, Does.Contain("<div class=\"foo bar:1"));
    }

    [UpstreamTest("SM-009 should escape tag names")]

    [Fixme("dotnet: the message has the locators in C# form, 'GetByText(\"special test description\").First', upstream expects \"getByText('special test description').first()\"")]
    public async Task ShouldEscapeTagNames(IPage page)
    {
        await page.SetContentAsync(@"
    <q:template> </q:template>
    <span>special test description</span>
    <q:template hidden="""" aria-hidden=""true"">
      <span>special test description</span>
    </q:template>
  ");
        var error = await Catch(() => Expect(page.GetByText("special test description")).ToBeVisibleAsync());
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("getByText('special test description').first()"));
        Assert.That(error.Message, Does.Contain("locator('q\\\\:template').filter({ hasText: 'special test description' })"));
    }

    [UpstreamTest("SM-010 should keep the engine name for a \"text=..\" selector in strict mode")]

    [Fixme("dotnet: the message has the locator in C# form, 'Locator(\"text=..loading\")', upstream expects \"locator('text=..loading')\"")]
    public async Task ShouldKeepTheEngineNameForATextSelectorInStrictMode(IPage page)
    {
        await page.SetContentAsync("<div>..loading</div><div>..loading</div>");
        var error = await Catch(() => page.Locator("text=..loading").HoverAsync());
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("locator('text=..loading')"));
    }
}
