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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-click-scroll.spec.ts.</summary>
public class PageClickScrollTests : UpstreamPageTest
{
    [UpstreamTest("CL-094 should not hit scroll bar")]
    public async Task ShouldNotHitScrollBar()
    {
        await Page.SetContentAsync(@"
    <style>
      .categories { width: 180px; display: flex; overflow-x: scroll; }
      button { flex: none; height: 28px; }
    </style>
    <div class=""categories"">
      <button>One</button>
      <button>Two</button>
      <button>Three</button>
      <button>Story</button>
      <button>More</button>
      <button>Items</button>
      <button>Here</button>
    </div>
    ");
        await Page.ClickAsync("text=Story", new() { Timeout = 2000 });
    }

    [UpstreamTest("CL-095 should scroll into view display:contents")]
    public async Task ShouldScrollIntoViewDisplayContents()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 105, "Needs https://chromium-review.googlesource.com/c/chromium/src/+/3758670");

        await Page.SetContentAsync(@"
    <div style=""background:red;height:2000px"">filler</div>
    <div>
      Example text, and button here:
      <button style=""display: contents"" onclick=""window._clicked=true;"">click me</button>
    </div>
  ");
        await Page.ClickAsync("text=click me", new() { Timeout = 5000 });
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-096 should scroll into view display:contents with a child")]
    public async Task ShouldScrollIntoViewDisplayContentsWithAChild()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 105, "Needs https://chromium-review.googlesource.com/c/chromium/src/+/3758670");

        await Page.SetContentAsync(@"
    <div style=""background:red;height:2000px"">filler</div>
    Example text, and button here:
    <button style=""display: contents"" onclick=""window._clicked=true;""><div>click me</div></button>
  ");
        await Page.ClickAsync("text=click me", new() { Timeout = 5000 });
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-097 should scroll into view display:contents with position")]
    public async Task ShouldScrollIntoViewDisplayContentsWithPosition()
    {
        Info.Fixme(BrowserName == "chromium", "DOM.getBoxModel does not work for display:contents");

        await Page.SetContentAsync(@"
    <div style=""background:red;height:2000px"">filler</div>
    <div>
      Example text, and button here:
      <button style=""display: contents"" onclick=""window._clicked=true;"">click me</button>
    </div>
  ");
        await Page.ClickAsync("text=click me", new() { Position = new() { X = 5, Y = 5 }, Timeout = 5000 });
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-098 should not crash when force-clicking hidden input")]
    public async Task ShouldNotCrashWhenForceClickingHiddenInput()
    {
        await Page.SetContentAsync("<input type=hidden>");
        var error = await Catch(() => Page.Locator("input").ClickAsync(new() { Force = true, Timeout = 2000 }));
        Assert.That(error.Message, Does.Contain("Element is not visible"));
    }

    [UpstreamTest("CL-099 should scroll into view span element")]
    public async Task ShouldScrollIntoViewSpanElement()
    {
        Info.Fixme(IsAndroid);
        await Page.SetContentAsync(@"
    <div id=big style=""height: 10000px;""></div>
    <span id=small>foo</span>
  ");
        await Page.Locator("#small").ScrollIntoViewIfNeededAsync();
        Assert.That(await Page.EvaluateAsync<int>("() => window.scrollY"), Is.GreaterThan(9000));
    }

    [UpstreamTest("CL-100 should scroll into view element in iframe")]
    public async Task ShouldScrollIntoViewElementInIframe()
    {
        Info.Fixme(IsAndroid);
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($@"
    <div id=big style=""height: 10000px;""></div>
    <iframe src='{Server.CrossProcessPrefix}/input/button.html'></iframe>
  ");
        await Page.FrameLocator("iframe").GetByRole(AriaRole.Button).ClickAsync(new() { Timeout = 5000 });
    }

    [UpstreamTest("CL-101 should not scroll the page when scroll is \"none\"")]
    public async Task ShouldNotScrollThePageWhenScrollIsNone()
    {
        await Page.SetContentAsync(@"
    <div style=""height: 2000px;""></div>
    <button onclick=""window._clicked=true"">click me</button>
  ");
        var error = await Catch(() => Page.Locator("button").ClickAsync(new() { Scroll = ScrollMode.None, Timeout = 2000 }));
        Assert.That(error.Message, Does.Contain("element is outside of the viewport"));
        Assert.That(await Page.EvaluateAsync<object>("window._clicked"), Is.Null);
        Assert.That(await Page.EvaluateAsync<int>("() => window.scrollY"), Is.EqualTo(0));
    }

    [UpstreamTest("CL-102 should click in-viewport element when scroll is \"none\"")]
    public async Task ShouldClickInViewportElementWhenScrollIsNone()
    {
        await Page.SetContentAsync(@"
    <button onclick=""window._clicked=true"">click me</button>
    <div style=""height: 2000px;""></div>
  ");
        await Page.Locator("button").ClickAsync(new() { Scroll = ScrollMode.None, Timeout = 2000 });
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<int>("() => window.scrollY"), Is.EqualTo(0));
    }

    [UpstreamTest("CL-103 should not scroll nested container when scroll is \"none\"")]
    public async Task ShouldNotScrollNestedContainerWhenScrollIsNone()
    {
        await Page.SetContentAsync(@"
    <div style=""height: 100px; width: 100px; overflow-y: scroll;"">
      <div style=""height: 50px;"">A</div>
      <div style=""height: 50px;"">B</div>
      <button style=""height: 50px; width: 100px;"" onclick=""window._clicked=true"">C</button>
    </div>
  ");
        var button = Page.Locator("button");
        // The button is scrolled out of the nested overflow container.
        var error = await Catch(() => button.ClickAsync(new() { Scroll = ScrollMode.None, Timeout = 2000 }));
        Assert.That(error, Is.Not.Null);
        Assert.That(await Page.EvaluateAsync<object>("window._clicked"), Is.Null);
        // Default behavior scrolls the nested container into view and succeeds.
        await button.ClickAsync(new() { Timeout = 2000 });
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-104 should not scroll on hover when scroll is \"none\"")]
    public async Task ShouldNotScrollOnHoverWhenScrollIsNone()
    {
        await Page.SetContentAsync(@"
    <div style=""height: 2000px;""></div>
    <div onmouseover=""window._hovered=true"" style=""width: 50px; height: 50px;"">hover me</div>
  ");
        var error = await Catch(() => Page.Locator("div >> text=hover me").HoverAsync(new() { Scroll = ScrollMode.None, Timeout = 2000 }));
        Assert.That(error.Message, Does.Contain("element is outside of the viewport"));
        Assert.That(await Page.EvaluateAsync<object>("window._hovered"), Is.Null);
        Assert.That(await Page.EvaluateAsync<int>("() => window.scrollY"), Is.EqualTo(0));
    }

    [UpstreamTest("CL-105 should scroll instantly on retry when scroll-behavior is smooth")]
    public async Task ShouldScrollInstantlyOnRetryWhenScrollBehaviorIsSmooth()
    {
        // The fixed header covers everything but the bottom strip of the viewport.
        // The first attempt centers the button under the header, so the click is
        // retried with a different scroll alignment using the native scrollIntoView().
        await Page.SetContentAsync(@"
    <style>
      html { scroll-behavior: smooth; }
      body { margin: 0; }
      .spacer { height: 2000px; }
      #header { position: fixed; top: 0; left: 0; right: 0; bottom: 50px; background: rgba(0, 0, 0, 0.1); }
      button { height: 30px; }
    </style>
    <div id=""header""></div>
    <div class=""spacer""></div>
    <button onclick=""window.clicked = true"">Target</button>
    <div class=""spacer""></div>
    <script>
      window.scrolls = [];
      addEventListener('scroll', () => window.scrolls.push(window.scrollY));
    </script>
  ");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<bool>("window.clicked"), Is.EqualTo(true));
        // Every scroll performed by Playwright should be a single instant jump,
        // and never a smooth animation that fires a scroll event per frame.
        var scrolls = await Page.EvaluateAsync<object[]>("() => window['scrolls']");
        Assert.That(scrolls.Length, Is.LessThanOrEqualTo(2));
    }
}
