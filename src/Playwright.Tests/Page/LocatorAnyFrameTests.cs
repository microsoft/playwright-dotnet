/**
 * Copyright (c) Microsoft Corporation.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-any-frame.spec.ts.</summary>
public class LocatorAnyFrameTests : UpstreamPageTest
{
    private static Task RoutePage(IPage page, string url, string body)
    {
        return page.RouteAsync("**/" + url, route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = body, ContentType = "text/html" }));
        });
    }

    private static async Task WaitForAllFrames(IPage page, int frameCount, string selector)
    {
        // Wait for all child frames to load their content, so that the search
        // deterministically sees elements in all of them.
        await Expect(async () => Assert.That(page.Frames.Count, Is.EqualTo(frameCount))).ToPassAsync();
        foreach (var frame in page.Frames)
        {
            if (frame != page.MainFrame)
            {
                await frame.WaitForSelectorAsync(selector, new() { State = WaitForSelectorState.Attached });
            }
        }
    }

    [UpstreamTest("LO-001 should click a button inside an iframe")]
    public async Task ShouldClickAButtonInsideAnIframe()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button onclick=\"window.__clicked = true\">Click me</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.FrameLocator().GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        Assert.That(await Page.Frames[1].EvaluateAsync<bool>("() => window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-002 should click a button in the main frame")]
    public async Task ShouldClickAButtonInTheMainFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><button onclick=\"window.__clicked = true\">Click me</button>");
        await RoutePage(Page, "a.html", "<div>No buttons here</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.FrameLocator().Locator("button").ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("() => window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-003 should fail click when elements match in multiple frames")]
    [LocalFixme("dotnet: the call log has FrameLocator().Locator(\"button\"), upstream expects frameLocator().locator('button')")]
    public async Task ShouldFailClickWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button>one</button>");
        await RoutePage(Page, "b.html", "<button>two</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 3, "button");
        var error = await Catch(() => Page.FrameLocator().Locator("button").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
        Assert.That(error.Message, Does.Contain("waiting for frameLocator().locator('button')"));
    }

    [UpstreamTest("LO-004 should fail click upon strict mode violation inside a single frame")]
    [LocalFixme("dotnet: the call log has FrameLocator().Locator(\"button\"), upstream expects frameLocator().locator('button')")]
    public async Task ShouldFailClickUponStrictModeViolationInsideASingleFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button>one</button><button>two</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "button");
        var error = await Catch(() => Page.FrameLocator().Locator("button").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("waiting for frameLocator().locator('button')"));
    }

    [UpstreamTest("LO-005 should time out on click when there are no matches")]
    [LocalFixme("dotnet: the call log has FrameLocator().Locator(\"button\"), upstream expects frameLocator().locator('button')")]
    public async Task ShouldTimeOutOnClickWhenThereAreNoMatches()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>Nothing here</div>");
        await Page.GotoAsync(Server.EmptyPage);
        var error = await Catch(() => Page.FrameLocator().Locator("button").ClickAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for frameLocator().locator('button')"));
    }

    [UpstreamTest("LO-006 should count elements in a single frame")]
    public async Task ShouldCountElementsInASingleFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>1</div><div>2</div><div>3</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "div");
        Assert.That(await Page.FrameLocator().Locator("div").CountAsync(), Is.EqualTo(3));
        Assert.That(await Page.FrameLocator().Locator("button").CountAsync(), Is.EqualTo(0));
    }

    [UpstreamTest("LO-007 should fail count when elements match in multiple frames")]
    public async Task ShouldFailCountWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<div>main</div><iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>child</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "div");
        var error = await Catch(() => Page.FrameLocator().Locator("div").CountAsync());
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("LO-008 should support toHaveCount")]
    public async Task ShouldSupportToHaveCount()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("span")).ToHaveCountAsync(2);
        await Expect(Page.FrameLocator().Locator("button")).ToHaveCountAsync(0);
    }

    [UpstreamTest("LO-009 should wait for a frame to appear with toHaveCount")]
    public async Task ShouldWaitForAFrameToAppearWithToHaveCount()
    {
        await RoutePage(Page, "empty.html", "<div>No frames yet</div>");
        await RoutePage(Page, "a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => {
      const iframe = document.createElement('iframe');
      iframe.src = 'a.html';
      document.body.appendChild(iframe);
    }, 500);
  }");
        await Expect(Page.FrameLocator().Locator("span")).ToHaveCountAsync(2);
    }

    [UpstreamTest("LO-010 should fail toHaveCount when elements match in multiple frames")]
    [LocalFixme("dotnet: the message of ToHaveCountAsync is \"Locator expected to have count\\nError: frameLocator() matched elements in multiple frames. '2'\\nBut was: 'null'\", with FrameLocator().Locator(\"span\") in the call log; upstream expects the line \"Locator: frameLocator().locator('span')\"")]
    public async Task ShouldFailToHaveCountWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span>");
        await RoutePage(Page, "b.html", "<span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 3, "span");
        var error = await Catch(() => Expect(Page.FrameLocator().Locator("span")).ToHaveCountAsync(2, new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
        Assert.That(error.Message, Does.Contain("Locator: frameLocator().locator('span')"));
    }

    [UpstreamTest("LO-011 should support toHaveText")]
    public async Task ShouldSupportToHaveText()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>Hello iframe</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div")).ToHaveTextAsync("Hello iframe");
    }

    [UpstreamTest("LO-012 should support toHaveText with an array")]
    public async Task ShouldSupportToHaveTextWithAnArray()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("span")).ToHaveTextAsync(new[] { "one", "two" });
    }

    [UpstreamTest("LO-013 should fail toHaveText when elements match in multiple frames")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: frameLocator() matched elements in multiple frames. 'one'\\nBut was: 'null'\", with FrameLocator().Locator(\"div\") in the call log; upstream expects the line \"Locator: frameLocator().locator('div')\"")]
    public async Task ShouldFailToHaveTextWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>one</div>");
        await RoutePage(Page, "b.html", "<div>two</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 3, "div");
        var error = await Catch(() => Expect(Page.FrameLocator().Locator("div")).ToHaveTextAsync("one", new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
        Assert.That(error.Message, Does.Contain("Locator: frameLocator().locator('div')"));
    }

    [UpstreamTest("LO-014 should fail toHaveText with an array when elements match in multiple frames")]
    public async Task ShouldFailToHaveTextWithAnArrayWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span>");
        await RoutePage(Page, "b.html", "<span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 3, "span");
        var error = await Catch(() => Expect(Page.FrameLocator().Locator("span")).ToHaveTextAsync(new[] { "one", "two" }, new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("LO-015 should fail toHaveText upon strict mode violation inside a single frame")]
    [LocalFixme("dotnet: the message of ToHaveTextAsync is \"Locator expected to have text\\nError: strict mode violation: Locator(\\\"div\\\") resolved to 2 elements: ... 'one'\\nBut was: 'null'\", with FrameLocator().Locator(\"div\") in the call log; upstream expects the line \"Locator: frameLocator().locator('div')\"")]
    public async Task ShouldFailToHaveTextUponStrictModeViolationInsideASingleFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>one</div><div>two</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "div");
        var error = await Catch(() => Expect(Page.FrameLocator().Locator("div")).ToHaveTextAsync("one", new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
        Assert.That(error.Message, Does.Contain("Locator: frameLocator().locator('div')"));
    }

    [UpstreamTest("LO-016 should support evaluate")]
    public async Task ShouldSupportEvaluate()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div data-foo=\"bar\">Hello</div>");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await Page.FrameLocator().Locator("div").EvaluateAsync<string>("e => e.getAttribute('data-foo')"), Is.EqualTo("bar"));
    }

    [UpstreamTest("LO-017 should fail evaluate when elements match in multiple frames")]
    public async Task ShouldFailEvaluateWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>one</div>");
        await RoutePage(Page, "b.html", "<div>two</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 3, "div");
        var error = await Catch(() => Page.FrameLocator().Locator("div").EvaluateAsync<string>("e => e.textContent", null, new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("LO-018 should time out on evaluate when there are no matches")]
    [LocalFixme("dotnet: the call log has FrameLocator().Locator(\"button\"), upstream expects frameLocator().locator('button')")]
    public async Task ShouldTimeOutOnEvaluateWhenThereAreNoMatches()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>Nothing here</div>");
        await Page.GotoAsync(Server.EmptyPage);
        var error = await Catch(() => Page.FrameLocator().Locator("button").EvaluateAsync<string>("e => e.textContent", null, new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for frameLocator().locator('button')"));
    }

    [UpstreamTest("LO-019 should support evaluateAll")]
    public async Task ShouldSupportEvaluateAll()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "span");
        Assert.That(await Page.FrameLocator().Locator("span").EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"), Is.EqualTo(new[] { "one", "two" }));
        Assert.That(await Page.FrameLocator().Locator("button").EvaluateAllAsync<int>("els => els.length"), Is.EqualTo(0));
    }

    [UpstreamTest("LO-020 should fail evaluateAll when elements match in multiple frames")]
    public async Task ShouldFailEvaluateAllWhenElementsMatchInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span>");
        await RoutePage(Page, "b.html", "<span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 3, "span");
        var error = await Catch(() => Page.FrameLocator().Locator("span").EvaluateAllAsync<int>("els => els.length"));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("LO-021 should support hasText filter")]
    public async Task ShouldSupportHasTextFilter()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>foo</div><div>bar</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div", new() { HasText = "bar" })).ToHaveTextAsync("bar");
    }

    [UpstreamTest("LO-022 should support first/last/nth")]
    public async Task ShouldSupportFirstLastNth()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<span>one</span><span>two</span><span>three</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "span");
        await Expect(Page.FrameLocator().Locator("span").First).ToHaveTextAsync("one");
        await Expect(Page.FrameLocator().Locator("span").Last).ToHaveTextAsync("three");
        await Expect(Page.FrameLocator().Locator("span").Nth(1)).ToHaveTextAsync("two");
    }

    [UpstreamTest("LO-023 should support nth in the middle of the chain")]
    public async Task ShouldSupportNthInTheMiddleOfTheChain()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div><span>one</span></div><div><span>two</span></div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div").Nth(1).Locator("span")).ToHaveTextAsync("two");
    }

    [UpstreamTest("LO-024 should support composite locators")]
    public async Task ShouldSupportCompositeLocators()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div><span>foo</span></div><div><i>bar</i></div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div", new() { Has = Page.Locator("span") })).ToHaveTextAsync("foo");
    }

    [UpstreamTest("LO-025 should support capture")]
    public async Task ShouldSupportCapture()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div id=\"target\"><span>hello</span></div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("*css=div >> span")).ToHaveAttributeAsync("id", "target");
    }

    [UpstreamTest("LO-026 should find a frame inside the scope")]
    public async Task ShouldFindAFrameInsideTheScope()
    {
        await RoutePage(Page, "empty.html", "<section><iframe src=\"a.html\"></iframe></section><button>outside</button>");
        await RoutePage(Page, "a.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "button");
        var scope = await Page.QuerySelectorAsync("section");
        var buttons = await scope.QuerySelectorAllAsync("internal:control=any-frame >> button");
        Assert.That(buttons.Count, Is.EqualTo(1));
        Assert.That(await buttons[0].TextContentAsync(), Is.EqualTo("inside"));
    }

    [UpstreamTest("LO-027 should find a nested frame inside the scope")]
    public async Task ShouldFindANestedFrameInsideTheScope()
    {
        await RoutePage(Page, "empty.html", "<section><iframe src=\"a.html\"></iframe></section><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe src=\"c.html\"></iframe>");
        await RoutePage(Page, "b.html", "<button>outside</button>");
        await RoutePage(Page, "c.html", "<button>deep</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(4))).ToPassAsync();
        foreach (var frame in Page.Frames)
        {
            if (frame.Url.Contains("b.html", StringComparison.Ordinal) || frame.Url.Contains("c.html", StringComparison.Ordinal))
            {
                await frame.WaitForSelectorAsync("button", new() { State = WaitForSelectorState.Attached });
            }
        }
        var scope = await Page.QuerySelectorAsync("section");
        var buttons = await scope.QuerySelectorAllAsync("internal:control=any-frame >> button");
        Assert.That(buttons.Count, Is.EqualTo(1));
        Assert.That(await buttons[0].TextContentAsync(), Is.EqualTo("deep"));
    }

    [UpstreamTest("LO-028 should find a frame inside the scope while another iframe is stalled")]
    public async Task ShouldFindAFrameInsideTheScopeWhileAnotherIframeIsStalled()
    {
        await RoutePage(Page, "empty.html", "<section><iframe src=\"a.html\"></iframe><iframe src=\"stall.html\"></iframe></section>");
        await RoutePage(Page, "a.html", "<button>inside</button>");
        await Page.RouteAsync("**/stall.html", route => { });
        await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        var scope = await Page.QuerySelectorAsync("section");
        var buttons = await scope.QuerySelectorAllAsync("internal:control=any-frame >> button");
        Assert.That(buttons.Count, Is.EqualTo(1));
        Assert.That(await buttons[0].TextContentAsync(), Is.EqualTo("inside"));
    }

    [UpstreamTest("LO-029 should respect the scope without a frame inside the scope")]
    public async Task ShouldRespectTheScopeWithoutAFrameInsideTheScope()
    {
        await RoutePage(Page, "empty.html", "<section><button>target</button></section><iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button>in-frame</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "button");
        var scope = await Page.QuerySelectorAsync("section");
        var buttons = await scope.QuerySelectorAllAsync("internal:control=any-frame >> button");
        Assert.That(buttons.Count, Is.EqualTo(1));
        Assert.That(await buttons[0].TextContentAsync(), Is.EqualTo("target"));
    }

    [UpstreamTest("LO-030 should not match a chain across a frame boundary")]
    public async Task ShouldNotMatchAChainAcrossAFrameBoundary()
    {
        await RoutePage(Page, "empty.html", "<section><iframe src=\"a.html\"></iframe></section>");
        await RoutePage(Page, "a.html", "<iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "b.html", "<button>deep</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        var deepFrame = Page.Frames.FirstOrDefault(f => f.Url.Contains("b.html", StringComparison.Ordinal));
        await deepFrame.WaitForSelectorAsync("button", new() { State = WaitForSelectorState.Attached });
        // "section" lives in the main frame, while the button is two frames below it.
        await Expect(Page.FrameLocator().Locator("section").Locator("button")).ToHaveCountAsync(0);
        await Expect(Page.FrameLocator().Locator("button")).ToHaveTextAsync("deep");
    }

    [UpstreamTest("LO-031 should only search frames inside the starting frame")]
    public async Task ShouldOnlySearchFramesInsideTheStartingFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><button>main</button>");
        await RoutePage(Page, "a.html", "<iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "b.html", "<button>deep</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        var deepFrame = Page.Frames.FirstOrDefault(f => f.Url.Contains("b.html", StringComparison.Ordinal));
        await deepFrame.WaitForSelectorAsync("button", new() { State = WaitForSelectorState.Attached });
        var middleFrame = Page.Frames.FirstOrDefault(f => f.Url.Contains("a.html", StringComparison.Ordinal));
        await Expect(middleFrame.FrameLocator().Locator("button")).ToHaveTextAsync("deep");
    }

    [UpstreamTest("LO-032 should enter a frame found in a nested frame")]
    public async Task ShouldEnterAFrameFoundInANestedFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><button>main</button>");
        await RoutePage(Page, "a.html", "<iframe id=\"target\" src=\"b.html\"></iframe><button>decoy</button>");
        await RoutePage(Page, "b.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().FrameLocator("#target").Locator("button")).ToHaveTextAsync("inside");
    }

    [UpstreamTest("LO-033 should click inside an entered frame")]
    public async Task ShouldClickInsideAnEnteredFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe id=\"target\" src=\"b.html\"></iframe><button>Click me</button>");
        await RoutePage(Page, "b.html", "<button onclick=\"window.__clicked = true\">Click me</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.FrameLocator().FrameLocator("#target").GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        var frame = Page.Frames.FirstOrDefault(f => f.Url.Contains("b.html", StringComparison.Ordinal));
        Assert.That(await frame.EvaluateAsync<bool>("() => window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-034 should not search nested frames after entering a frame")]
    public async Task ShouldNotSearchNestedFramesAfterEnteringAFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe id=\"target\" src=\"a.html\"></iframe><button>main</button>");
        await RoutePage(Page, "a.html", "<iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "b.html", "<button>deep</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        // The entered frame itself has no button, and we do not look inside its nested frames.
        await Expect(Page.FrameLocator().FrameLocator("#target").Locator("button")).ToHaveCountAsync(0);
    }

    [UpstreamTest("LO-035 should support two frameLocators")]
    public async Task ShouldSupportTwoFrameLocators()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe id=\"x\" src=\"b.html\"></iframe>");
        await RoutePage(Page, "b.html", "<iframe id=\"y\" src=\"c.html\"></iframe><button>decoy</button>");
        await RoutePage(Page, "c.html", "<button>bottom</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().FrameLocator("#x").FrameLocator("#y").Locator("button")).ToHaveTextAsync("bottom");
    }

    [UpstreamTest("LO-036 should support locator before frameLocator")]
    public async Task ShouldSupportLocatorBeforeFrameLocator()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<section><iframe src=\"b.html\"></iframe></section><iframe src=\"c.html\"></iframe>");
        await RoutePage(Page, "b.html", "<button>in-section</button>");
        await RoutePage(Page, "c.html", "<button>outside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("section").FrameLocator("iframe").Locator("button")).ToHaveTextAsync("in-section");
    }

    [UpstreamTest("LO-037 should support owner of a frameLocator")]
    public async Task ShouldSupportOwnerOfAFrameLocator()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe id=\"target\" src=\"b.html\"></iframe>");
        await RoutePage(Page, "b.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await Page.FrameLocator().FrameLocator("#target").Owner.GetAttributeAsync("id"), Is.EqualTo("target"));
    }

    [UpstreamTest("LO-038 should wait for the frame to enter to appear")]
    public async Task ShouldWaitForTheFrameToEnterToAppear()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div>Nothing yet</div>");
        await RoutePage(Page, "b.html", "<button>late</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(2))).ToPassAsync();
        await Page.Frames[1].EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => {
      const iframe = document.createElement('iframe');
      iframe.id = 'late';
      iframe.src = 'b.html';
      document.body.appendChild(iframe);
    }, 3000);
  }");
        await Expect(Page.FrameLocator().FrameLocator("#late").Locator("button")).ToHaveTextAsync("late");
    }

    [UpstreamTest("LO-039 should fail when the frame to enter matches in multiple frames")]
    public async Task ShouldFailWhenTheFrameToEnterMatchesInMultipleFrames()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe class=\"inner\" src=\"c.html\"></iframe>");
        await RoutePage(Page, "b.html", "<iframe class=\"inner\" src=\"c.html\"></iframe>");
        await RoutePage(Page, "c.html", "<button>Click me</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(5))).ToPassAsync();
        foreach (var frame in Page.Frames)
        {
            if (frame.Url.Contains("c.html", StringComparison.Ordinal))
            {
                await frame.WaitForSelectorAsync("button", new() { State = WaitForSelectorState.Attached });
            }
        }
        var error = await Catch(() => Page.FrameLocator().FrameLocator(".inner").Locator("button").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("LO-040 should support contentFrame")]
    public async Task ShouldSupportContentFrame()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe id=\"target\" src=\"b.html\"></iframe><button>decoy</button>");
        await RoutePage(Page, "b.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("#target").ContentFrame.Locator("button")).ToHaveTextAsync("inside");
    }

    [UpstreamTest("LO-041 should render frameLocator() in the locator description")]
    [LocalFixme("dotnet: Locator.ToString() is the C# form, 'FrameLocator().Locator(\"#x\").ContentFrame.Locator(\"button\")'; upstream expects \"frameLocator().locator('#x').contentFrame().locator('button')\"")]
    public async Task ShouldRenderFrameLocatorInTheLocatorDescription()
    {
        Assert.That(Page.FrameLocator().FrameLocator("#x").Locator("button").ToString(), Is.EqualTo("frameLocator().locator('#x').contentFrame().locator('button')"));
        Assert.That(Page.FrameLocator().Locator("section").FrameLocator("iframe").GetByText("foo").ToString(), Is.EqualTo("frameLocator().locator('section').locator('iframe').contentFrame().getByText('foo')"));
    }

    [UpstreamTest("LO-042 should not allow frameLocator() inside a composite locator")]
    [LocalFixme("dotnet: the message names the locator in the C# form, 'while querying \"Locator(\"button\").Or(FrameLocator().Locator(\"a\"))\"'; upstream expects 'while querying \"locator('button').or(frameLocator().locator('a'))\"'")]
    public async Task ShouldNotAllowFrameLocatorInsideACompositeLocator()
    {
        await RoutePage(Page, "empty.html", "<button>main</button><iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<a href=\"#\">link</a>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "a");

        var error = await Catch(() => Page.Locator("button").Or(Page.FrameLocator().Locator("a")).CountAsync());
        Assert.That(error.Message, Does.Contain("frameLocator() is not allowed inside composite locators, while querying \"locator('button').or(frameLocator().locator('a'))\""));

        var error2 = await Catch(() => Page.Locator("button").Filter(new() { Has = Page.FrameLocator().Locator("a") }).CountAsync());
        Assert.That(error2.Message, Does.Contain("frameLocator() is not allowed inside composite locators"));

        // Repeating frameLocator() in the operand is not allowed either, even though the outer locator has it.
        var error3 = await Catch(() => Page.FrameLocator().Locator("button").Or(Page.FrameLocator().Locator("a")).CountAsync());
        Assert.That(error3.Message, Does.Contain("frameLocator() is not allowed inside composite locators, while querying \"frameLocator().locator('button').or(frameLocator().locator('a'))\""));

        // Repeating frameLocator() is not allowed even when the rest of the frame chain matches.
        var error4 = await Catch(() => Page.FrameLocator().FrameLocator("#f").Locator("a").Or(Page.FrameLocator().FrameLocator("#f").Locator("button")).CountAsync());
        Assert.That(error4.Message, Does.Contain("frameLocator() is not allowed inside composite locators"));

        // With frameLocator() first, the token applies to the whole locator, so both operands are searched in every frame.
        var error5 = await Catch(() => Page.FrameLocator().Locator("a").Or(Page.Locator("button")).CountAsync());
        Assert.That(error5.Message, Does.Contain("frameLocator() matched elements in multiple frames"));
    }

    [UpstreamTest("LO-043 should support a composite locator under frameLocator()")]
    public async Task ShouldSupportACompositeLocatorUnderFrameLocator()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<div class=\"classname\">first</div><button>second</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "button");
        await Expect(Page.FrameLocator().Locator(".classname").Or(Page.GetByRole(AriaRole.Button))).ToHaveTextAsync(new[] { "first", "second" });
    }

    [UpstreamTest("LO-044 should support a composite locator under frameLocator() and a frame locator")]
    public async Task ShouldSupportACompositeLocatorUnderFrameLocatorAndAFrameLocator()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<iframe id=\"f\" src=\"b.html\"></iframe>");
        await RoutePage(Page, "b.html", "<div class=\"classname\">first</div><button>second</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        await Expect(Page.FrameLocator().FrameLocator("#f").Locator(".classname").Or(Page.FrameLocator("#f").GetByRole(AriaRole.Button))).ToHaveTextAsync(new[] { "first", "second" });
    }

    [UpstreamTest("LO-045 should not allow first/last/nth on frameLocator()")]
    public async Task ShouldNotAllowFirstLastNthOnFrameLocator()
    {
        Assert.That(Assert.Throws<PlaywrightException>(() => _ = Page.FrameLocator().First).Message, Does.Contain("Selecting the nth frame is not allowed on frameLocator()"));
        Assert.That(Assert.Throws<PlaywrightException>(() => _ = Page.FrameLocator().Last).Message, Does.Contain("Selecting the nth frame is not allowed on frameLocator()"));
        Assert.That(Assert.Throws<PlaywrightException>(() => Page.FrameLocator().Nth(1)).Message, Does.Contain("Selecting the nth frame is not allowed on frameLocator()"));
    }

    [UpstreamTest("LO-046 should not allow owner on frameLocator()")]
    public async Task ShouldNotAllowOwnerOnFrameLocator()
    {
        var error = await Catch(() => Page.FrameLocator().Owner.CountAsync());
        Assert.That(error.Message, Does.Contain("Selector cannot be empty after frameLocator()"));
    }

    [UpstreamTest("LO-047 should resolve aria-ref selectors")]
    public async Task ShouldResolveAriaRefSelectors()
    {
        await RoutePage(Page, "empty.html", "<button>main</button><iframe src=\"a.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(Page, 2, "button");
        var snapshot = await Page.AriaSnapshotAsync(new() { Mode = AriaSnapshotMode.Ai });
        // Refs are unique across frames, so every starting frame resolves them to the same element.
        var insideMatch = Regex.Match(snapshot, "button \"inside\" \\[ref=(.*?)\\]");
        Assert.That(insideMatch.Groups[1].Value, Does.Match(@"^f\d+e\d+$"));
        await Expect(Page.FrameLocator().Locator($"aria-ref={insideMatch.Groups[1].Value}")).ToHaveTextAsync("inside");
    }

    [UpstreamTest("LO-048 should click while another iframe is stalled")]
    public async Task ShouldClickWhileAnotherIframeIsStalled()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"stall.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button onclick=\"window.__clicked = true\">Click me</button>");
        await Page.RouteAsync("**/stall.html", route => { });
        await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        await Page.FrameLocator().Locator("button").ClickAsync();
        var frame = Page.Frames.FirstOrDefault(f => f.Url.Contains("a.html", StringComparison.Ordinal));
        Assert.That(await frame.EvaluateAsync<bool>("() => window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-049 should support toBeVisible while another iframe is stalled")]
    public async Task ShouldSupportToBeVisibleWhileAnotherIframeIsStalled()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"stall.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button>Click me</button>");
        await Page.RouteAsync("**/stall.html", route => { });
        await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        await Expect(Page.FrameLocator().Locator("button")).ToBeVisibleAsync();
    }

    [UpstreamTest("LO-050 should support toHaveCount while another iframe is stalled")]
    public async Task ShouldSupportToHaveCountWhileAnotherIframeIsStalled()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"stall.html\"></iframe>");
        await RoutePage(Page, "a.html", "<button>Click me</button>");
        await Page.RouteAsync("**/stall.html", route => { });
        await Page.GotoAsync(Server.EmptyPage, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Expect(async () => Assert.That(Page.Frames.Count, Is.EqualTo(3))).ToPassAsync();
        await Expect(Page.FrameLocator().Locator("button")).ToHaveCountAsync(1);
    }

    [UpstreamTest("LO-051 should treat elements inside hidden iframe as hidden")]
    public async Task ShouldTreatElementsInsideHiddenIframeAsHidden()
    {
        await RoutePage(Page, "empty.html", "<iframe src=\"a.html\" style=\"visibility: hidden\"></iframe>");
        await RoutePage(Page, "a.html", "<button onclick=\"window.__clicked = true\">Click me</button>");
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator().Locator("button");
        await Expect(button).ToBeAttachedAsync();
        Assert.That(await button.IsVisibleAsync(), Is.EqualTo(false));
        await Expect(button).ToBeHiddenAsync();

        var error = await Catch(() => button.ClickAsync(new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("element is inside a hidden frame, retrying"));
        Assert.That(await Page.Frames[1].EvaluateAsync<object>("() => window.__clicked"), Is.Null);

        await Page.EvaluateAsync("() => document.querySelector('iframe').style.visibility = 'visible'");
        await Expect(button).ToBeVisibleAsync();
        await button.ClickAsync();
        Assert.That(await Page.Frames[1].EvaluateAsync<bool>("() => window.__clicked"), Is.EqualTo(true));
    }
}
