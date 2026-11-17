/**
 * Copyright Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/locator-highlight.spec.ts.</summary>
public class LocatorHighlightTests : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Mode != "default", "Highlight overlay uses an open shadow root only in default mode");

    [UpstreamTest("LO-193 highlight should accept a CSS string style")]
    public async Task HighlightShouldAcceptACSSStringStyle()
    {
        Info.Skip(IsFrozenWebkit);

        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        await page.GetByRole(AriaRole.Button).HighlightAsync(new() { Style = "outline: 3px solid rgb(255, 0, 0); background-color: rgba(0, 255, 0, 0.25)" });

        var highlight = page.Locator("x-pw-highlight");
        await Expect(highlight).ToBeVisibleAsync();
        var style = await highlight.EvaluateAsync<JsonElement>(@"el => ({
    // Browsers serialize the 'outline' shorthand in different orders, check the longhands.
    outlineWidth: el.style.outlineWidth,
    outlineStyle: el.style.outlineStyle,
    outlineColor: el.style.outlineColor,
    backgroundColor: el.style.backgroundColor,
  })");
        Assert.That(style.GetProperty("outlineWidth").GetString(), Is.EqualTo("3px"));
        Assert.That(style.GetProperty("outlineStyle").GetString(), Is.EqualTo("solid"));
        Assert.That(style.GetProperty("outlineColor").GetString(), Is.EqualTo("rgb(255, 0, 0)"));
        Assert.That(style.GetProperty("backgroundColor").GetString(), Is.EqualTo("rgba(0, 255, 0, 0.25)"));

        await context.CloseAsync();
    }

    [UpstreamTest("LO-195 hideHighlight removes a styled highlight")]
    public async Task HideHighlightRemovesAStyledHighlight()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");

        var button = page.GetByRole(AriaRole.Button);
        await button.HighlightAsync(new() { Style = "outline: 2px solid red" });
        await Expect(page.Locator("x-pw-highlight")).ToBeVisibleAsync();

        await button.HideHighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("LO-196 highlight should survive navigation")]
    public async Task HighlightShouldSurviveNavigation()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<button onclick=\"console.log(1)\">Clicker</button>");

        await page.GetByRole(AriaRole.Button).HighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(1);

        // Highlights are resolved again after the navigation.
        await page.GotoAsync(Server.Prefix + "/input/button.html");
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(1);

        await page.HideHighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("LO-197 Page.hideHighlight clears all locator highlights")]
    public async Task PageHideHighlightClearsAllLocatorHighlights()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<button>One</button><button>Two</button>");

        await page.GetByRole(AriaRole.Button, new() { Name = "One" }).HighlightAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Two" }).HighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(2);

        await page.HideHighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("LO-198 highlight should work with a custom selector engine that runs in the main world")]
    public async Task HighlightShouldWorkWithACustomSelectorEngineThatRunsInTheMainWorld()
    {
        // Engines registered without "contentScript" run in the main world and can see page globals.
        const string createEngine = @"() => ({
    query(root, selector) {
      return window['__engineEnabled'] ? root.querySelector(selector) : null;
    },
    queryAll(root, selector) {
      return window['__engineEnabled'] ? Array.from(root.querySelectorAll(selector)) : [];
    },
  })";
        await Playwright.Selectors.RegisterAsync("highlight-tag", new() { Script = "(" + createEngine + ")()" });

        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<button>Button</button>");
        await page.EvaluateAsync("() => window['__engineEnabled'] = true");

        var button = page.Locator("highlight-tag=button");
        await button.HighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(1);

        await button.HideHighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(0);

        await button.HighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(1);

        await page.HideHighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }

    [UpstreamTest("LO-199 highlight should resolve relative to the frame of the locator")]
    public async Task HighlightShouldResolveRelativeToTheFrameOfTheLocator()
    {
        var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<iframe name=\"frame\" srcdoc=\"<button>foo</button>\"></iframe><button>bar</button>");
        var frame = page.Frame("frame");

        await page.Locator("button").HighlightAsync();
        await frame.Locator("button").HighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(1);
        await Expect(frame.Locator("x-pw-highlight")).ToHaveCountAsync(1);

        await page.Locator("button").HideHighlightAsync();
        await Expect(page.Locator("x-pw-highlight")).ToHaveCountAsync(0);
        await Expect(frame.Locator("x-pw-highlight")).ToHaveCountAsync(1);

        await frame.Locator("button").HideHighlightAsync();
        await Expect(frame.Locator("x-pw-highlight")).ToHaveCountAsync(0);

        await context.CloseAsync();
    }
}
