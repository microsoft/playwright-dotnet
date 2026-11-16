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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-click.spec.ts.</summary>
public class PageClickTests
{
    private static Task GiveItAChanceToClick(IPage page) => Rafraf(page, 5);

    [UpstreamTest("CL-001 should click the button @smoke")]
    public async Task ShouldClickTheButton(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-002 should click button inside frameset")]
    public async Task ShouldClickButtonInsideFrameset(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/frameset.html");
        var frameElement = await page.QuerySelectorAsync("frame");
        await frameElement.EvaluateAsync("frame => frame.src = '/input/button.html'");
        var frame = await frameElement.ContentFrameAsync();
        await frame.ClickAsync("button");
        Assert.That(await frame.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-003 should issue clicks in parallel in page and popup")]
    public async Task ShouldIssueClicksInParallelInPageAndPopup(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/counter.html");
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open('/counter.html')"));
        var clickPromises = new List<Task>();
        for (var i = 0; i < 21; ++i)
        {
            if (i % 3 == 0)
            {
                clickPromises.Add(popup.Locator("button").ClickAsync());
            }
            else
            {
                clickPromises.Add(page.Locator("button").ClickAsync());
            }
        }
        await PromiseAll(clickPromises);
        Assert.That(await page.EvaluateAsync<int>("() => window['count']"), Is.EqualTo(14));
        Assert.That(await popup.EvaluateAsync<int>("() => window['count']"), Is.EqualTo(7));
    }

    [UpstreamTest("CL-004 should click svg")]
    public async Task ShouldClickSvg(IPage page)
    {
        await page.SetContentAsync(@"
    <svg height=""100"" width=""100"">
      <circle onclick=""window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red"" />
    </svg>
  ");
        await page.ClickAsync("circle");
        Assert.That(await page.EvaluateAsync<int>("__CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-005 should click the button if window.Node is removed")]
    public async Task ShouldClickTheButtonIfWindowNodeIsRemoved(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync("() => delete window.Node");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/4281
    [UpstreamTest("CL-006 should click on a span with an inline element inside")]
    public async Task ShouldClickOnASpanWithAnInlineElementInside(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
    span::before {
      content: 'q';
    }
    </style>
    <span onclick='window.CLICKED=42'></span>
  ");
        await page.ClickAsync("span");
        Assert.That(await page.EvaluateAsync<int>("CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-007 should click the aligned 1x1 div")]
    public async Task ShouldClickTheAligned1x1Div(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-008 should click the half-aligned 1x1 div")]
    public async Task ShouldClickTheHalfAligned1x1Div(IPage page)
    {
        await page.SetContentAsync("<div style=\"margin-left: 20.5px; margin-top: 11.5px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-009 should click the unaligned 1x1 div v1")]
    public async Task ShouldClickTheUnaligned1x1DivV1(IPage page)
    {
        await page.SetContentAsync("<div style=\"margin-left: 20.23px; margin-top: 11.65px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-010 should click the unaligned 1x1 div v2")]
    public async Task ShouldClickTheUnaligned1x1DivV2(IPage page)
    {
        await page.SetContentAsync("<div style=\"margin-left: 20.68px; margin-top: 11.13px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-011 should click the unaligned 1x1 div v3")]
    public async Task ShouldClickTheUnaligned1x1DivV3(IPage page)
    {
        await page.SetContentAsync("<div style=\"margin-left: 20.68px; margin-top: 11.52px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-012 should click the unaligned 1x1 div v4")]
    public async Task ShouldClickTheUnaligned1x1DivV4(IPage page)
    {
        await page.SetContentAsync("<div style=\"margin-left: 20.15px; margin-top: 11.24px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await page.ClickAsync("div");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-013 should click the button after navigation ")]
    public async Task ShouldClickTheButtonAfterNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-014 should click the button after a cross origin navigation ")]
    public async Task ShouldClickTheButtonAfterACrossOriginNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        await page.GotoAsync(server.CrossProcessPrefix + "/input/button.html");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-015 should click when one of inline box children is outside of viewport")]
    public async Task ShouldClickWhenOneOfInlineBoxChildrenIsOutsideOfViewport(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
    i {
      position: absolute;
      top: -1000px;
    }
    </style>
    <span onclick='window.CLICKED = 42;'><i>woof</i><b>doggo</b></span>
  ");
        await page.ClickAsync("span");
        Assert.That(await page.EvaluateAsync<int>("CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-016 should select the text by triple clicking")]
    public async Task ShouldSelectTheTextByTripleClicking(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        const string text = "This is the text that we are going to try to select. Let's see how it goes.";
        await page.FillAsync("textarea", text);
        await page.ClickAsync("textarea", new() { ClickCount = 3 });
        Assert.That(
            await page.EvaluateAsync<string>(@"() => {
    const textarea = document.querySelector('textarea');
    return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
  }"),
            Is.EqualTo(text));
    }

    [UpstreamTest("CL-017 should click offscreen buttons")]
    public async Task ShouldClickOffscreenButtons(IPage page, Server server, string browserName, bool headless)
    {
        await page.GotoAsync(server.Prefix + "/offscreenbuttons.html");
        var messages = new List<string>();
        page.Console += (_, msg) => messages.Add(msg.Text);
        for (var i = 0; i < 11; ++i)
        {
            // We might've scrolled to click a button - reset to (0, 0).
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await page.ClickAsync($"#btn{i}");
        }
        Assert.That(messages, Is.EqualTo(new[]
        {
            "button #0 clicked",
            "button #1 clicked",
            "button #2 clicked",
            "button #3 clicked",
            "button #4 clicked",
            "button #5 clicked",
            "button #6 clicked",
            "button #7 clicked",
            "button #8 clicked",
            "button #9 clicked",
            "button #10 clicked",
        }));
    }

    [UpstreamTest("CL-018 should waitFor visible when already visible")]
    public async Task ShouldWaitForVisibleWhenAlreadyVisible(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-019 should not wait with force")]
    public async Task ShouldNotWaitWithForce(IPage page, Server server)
    {
        Exception error = null;
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        error = await Catch(() => page.ClickAsync("button", new() { Force = true }));
        Assert.That(error.Message, Does.Contain("Element is not visible"));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-020 should waitFor display:none to be gone")]
    public async Task ShouldWaitForDisplayNoneToBeGone(IPage page, Server server)
    {
        var done = false;
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        async Task Clicked()
        {
            await page.ClickAsync("button", new() { Timeout = 0 });
            done = true;
        }
        var clicked = Clicked();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
        Assert.That(done, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("button", "b => b.style.display = 'block'");
        await clicked;
        Assert.That(done, Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-021 should waitFor visibility:hidden to be gone")]
    public async Task ShouldWaitForVisibilityHiddenToBeGone(IPage page, Server server)
    {
        var done = false;
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
        async Task Clicked()
        {
            await page.ClickAsync("button", new() { Timeout = 0 });
            done = true;
        }
        var clicked = Clicked();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
        Assert.That(done, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'visible'");
        await clicked;
        Assert.That(done, Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-022 should waitFor visible when parent is hidden")]
    public async Task ShouldWaitForVisibleWhenParentIsHidden(IPage page, Server server)
    {
        var done = false;
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'none'");
        async Task Clicked()
        {
            await page.ClickAsync("button", new() { Timeout = 0 });
            done = true;
        }
        var clicked = Clicked();
        await GiveItAChanceToClick(page);
        Assert.That(done, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'block'");
        await clicked;
        Assert.That(done, Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-023 should click wrapped links")]
    public async Task ShouldClickWrappedLinks(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/wrappedlink.html");
        await page.ClickAsync("a");
        Assert.That(await page.EvaluateAsync<bool>("__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-024 should click on checkbox input and toggle")]
    public async Task ShouldClickOnCheckboxInputAndToggle(IPage page, Server server, bool headless)
    {
        await page.GotoAsync(server.Prefix + "/input/checkbox.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['result'].check"), Is.Null);
        await page.ClickAsync("input#agree");
        Assert.That(await page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(true));
        var events = await page.EvaluateAsync<string[]>("() => window['result'].events");
        if (!headless)
        {
            events = events.Where(e => e != "mouseout" && e != "mouseleave").ToArray();
        }
        Assert.That(events, Is.EqualTo(new[]
        {
            "mouseover",
            "mouseenter",
            "mousemove",
            "mousedown",
            "mouseup",
            "click",
            "input",
            "change",
        }));
        await page.ClickAsync("input#agree");
        Assert.That(await page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(false));
    }

    [UpstreamTest("CL-025 should click on checkbox label and toggle")]
    public async Task ShouldClickOnCheckboxLabelAndToggle(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/checkbox.html");
        Assert.That(await page.EvaluateAsync<object>("() => window['result'].check"), Is.Null);
        await page.ClickAsync("label[for=\"agree\"]");
        Assert.That(await page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string[]>("() => window['result'].events"), Is.EqualTo(new[]
        {
            "click",
            "input",
            "change",
        }));
        await page.ClickAsync("label[for=\"agree\"]");
        Assert.That(await page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(false));
    }

    [UpstreamTest("CL-026 should scroll and click the button")]
    public async Task ShouldScrollAndClickTheButton(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.ClickAsync("#button-5");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('#button-5').textContent"), Is.EqualTo("clicked"));
        await page.ClickAsync("#button-80");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('#button-80').textContent"), Is.EqualTo("clicked"));
    }

    [UpstreamTest("CL-027 should scroll and click the button with smooth scroll behavior")]
    public async Task ShouldScrollAndClickTheButtonWithSmoothScrollBehavior(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.AddStyleTagAsync(new() { Content = "html { scroll-behavior: smooth; }" });
        for (var i = 0; i < 5; i++)
        {
            await page.ClickAsync("#button-80");
            Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('#button-80').textContent"), Is.EqualTo("clicked"));
            await page.ClickAsync("#button-20");
            Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('#button-20').textContent"), Is.EqualTo("clicked"));
        }
    }

    [UpstreamTest("CL-028 should double click the button")]
    public async Task ShouldDoubleClickTheButton(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync(@"() => {
    window['double'] = false;
    const button = document.querySelector('button');
    button.addEventListener('dblclick', event => {
      window['double'] = true;
    });
  }");
        await page.DblClickAsync("button");
        Assert.That(await page.EvaluateAsync<bool>("double"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-029 should click a partially obscured button")]
    public async Task ShouldClickAPartiallyObscuredButton(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync(@"() => {
    const button = document.querySelector('button');
    button.textContent = 'Some really long text that will go offscreen';
    button.style.position = 'absolute';
    button.style.left = '368px';
  }");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-030 should click a rotated button")]
    public async Task ShouldClickARotatedButton(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/rotatedButton.html");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-031 should fire contextmenu event on right click")]
    public async Task ShouldFireContextmenuEventOnRightClick(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.ClickAsync("#button-8", new() { Button = MouseButton.Right });
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('#button-8').textContent"), Is.EqualTo("context menu"));
    }

    [UpstreamTest("CL-032 should click links which cause navigation")]
    public async Task ShouldClickLinksWhichCauseNavigation(IPage page, Server server)
    {
        // @see https://github.com/GoogleChrome/puppeteer/issues/206
        await page.SetContentAsync($"<a href=\"{server.EmptyPage}\">empty.html</a>");
        // This await should not hang.
        await page.ClickAsync("a");
    }

    [UpstreamTest("CL-033 should not wait for navigation after clicking a link answered with 204")]
    public async Task ShouldNotWaitForNavigationAfterClickingALinkAnsweredWith204(IPage page, Server server)
    {
        server.SetRoute("/204", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($"<a href=\"{server.Prefix}/204\">link</a>");
        await page.ClickAsync("a", new() { Timeout = 5000 });
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("CL-034 should click the button inside an iframe")]
    public async Task ShouldClickTheButtonInsideAnIframe(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
        await AttachFrame(page, "button-test", server.Prefix + "/input/button.html");
        var frame = page.Frames[1];
        var button = await frame.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.That(await frame.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-035 should click the button with fixed position inside an iframe")]
    public async Task ShouldClickTheButtonWithFixedPositionInsideAnIframe(IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "chromium");

        // @see https://github.com/GoogleChrome/puppeteer/issues/4110
        // @see https://bugs.chromium.org/p/chromium/issues/detail?id=986390
        // @see https://chromium-review.googlesource.com/c/chromium/src/+/1742784
        await page.GotoAsync(server.EmptyPage);
        await page.SetViewportSizeAsync(500, 500);
        await page.SetContentAsync("<div style=\"width:100px;height:2000px\">spacer</div>");
        await AttachFrame(page, "button-test", server.CrossProcessPrefix + "/input/button.html");
        var frame = page.Frames[1];
        await frame.EvalOnSelectorAsync("button", "button => button.style.setProperty('position', 'fixed')");
        await frame.ClickAsync("button");
        Assert.That(await frame.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-036 should click the button behind sticky header")]
    public async Task ShouldClickTheButtonBehindStickyHeader(IPage page)
    {
        await page.SetViewportSizeAsync(500, 240);
        await page.SetContentAsync(@"
    <style>
    * {
      padding: 0;
      margin: 0;
    }
    li {
      height: 80px;
      border: 1px solid black;
    }
    ol {
      padding-top: 160px;
    }
    div.fixed {
      position: fixed;
      z-index: 1001;
      width: 100%;
      background: red;
      height: 160px;
    }
    </style>

    <div class=fixed></div>

    <ol>
    <li>hi1</li><li>hi2</li><li>hi3</li><li>hi4</li><li>hi5</li><li>hi6</li><li>hi7</li><li>hi8</li>
    <li id=target onclick=""window.__clicked = true"">hi9</li>
    <li>hi10</li><li>hi11</li><li>hi12</li><li>hi13</li><li id=li14>hi14</li>
    </ol>
  ");
        await page.EvalOnSelectorAsync("#li14", "e => e.scrollIntoView()");
        await page.ClickAsync("#target");
        Assert.That(await page.EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-037 should click the button behind position:absolute header")]
    public async Task ShouldClickTheButtonBehindPositionAbsoluteHeader(IPage page)
    {
        await page.SetViewportSizeAsync(500, 240);
        await page.SetContentAsync(@"
    <style>
    * {
      padding: 0;
      margin: 0;
    }
    li {
      height: 80px;
      border: 1px solid black;
    }
    ol {
      height: 100vh;
      overflow: scroll;
      padding-top: 160px;
    }
    body {
      position: relative;
    }
    div.fixed {
      position: absolute;
      top: 0;
      z-index: 1001;
      width: 100%;
      background: red;
      height: 160px;
    }
    </style>

    <ol>
    <li>hi1</li><li>hi2</li><li>hi3</li><li>hi4</li><li>hi5</li><li>hi6</li><li>hi7</li><li>hi8</li>
    <li id=target onclick=""window.__clicked = true"">hi9</li>
    <li>hi10</li><li>hi11</li><li>hi12</li><li>hi13</li><li id=li14>hi14</li>
    </ol>

    <div class=fixed>Overlay</div>
  ");
        await page.EvalOnSelectorAsync("ol", @"e => {
    const target = document.querySelector('#target');
    e.scrollTo({ top: target.offsetTop, behavior: 'instant' });
  }");
        await page.ClickAsync("#target");
        Assert.That(await page.EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-038 should click the button with px border with offset")]
    public async Task ShouldClickTheButtonWithPxBorderWithOffset(IPage page, Server server, bool isFrozenWebkit)
    {
        It.Skip(isFrozenWebkit, "needs recent webkit");
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
        await page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await page.EvaluateAsync<int>("offsetX"), Is.EqualTo(20));
        Assert.That(await page.EvaluateAsync<int>("offsetY"), Is.EqualTo(10));
    }

    [UpstreamTest("CL-039 should click the button with em border with offset")]
    public async Task ShouldClickTheButtonWithEmBorderWithOffset(IPage page, Server server, bool isFrozenWebkit)
    {
        It.Skip(isFrozenWebkit, "needs recent webkit");
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '2em'");
        await page.EvalOnSelectorAsync("button", "button => button.style.fontSize = '12px'");
        await page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await page.EvaluateAsync<int>("offsetX"), Is.EqualTo(20));
        Assert.That(await page.EvaluateAsync<int>("offsetY"), Is.EqualTo(10));
    }

    [UpstreamTest("CL-040 should click a very large button with offset")]
    public async Task ShouldClickAVeryLargeButtonWithOffset(IPage page, Server server, bool isFrozenWebkit, bool isAndroid)
    {
        It.Fixme(isAndroid, "Failed to scroll to a particular point");
        It.Skip(isFrozenWebkit, "needs recent webkit");
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
        await page.EvalOnSelectorAsync("button", "button => button.style.height = button.style.width = '2000px'");
        await page.ClickAsync("button", new() { Position = new() { X = 1900, Y = 1910 } });
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await page.EvaluateAsync<int>("offsetX"), Is.EqualTo(1900));
        Assert.That(await page.EvaluateAsync<int>("offsetY"), Is.EqualTo(1910));
    }

    [UpstreamTest("CL-041 should click a button in scrolling container with offset")]
    public async Task ShouldClickAButtonInScrollingContainerWithOffset(IPage page, Server server, bool isFrozenWebkit, bool isAndroid)
    {
        It.Fixme(isAndroid, "Failed to scroll to a particular point");
        It.Skip(isFrozenWebkit, "needs recent webkit");
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", @"button => {
    const container = document.createElement('div');
    container.style.overflow = 'auto';
    container.style.width = '200px';
    container.style.height = '200px';
    button.parentElement.insertBefore(container, button);
    container.appendChild(button);
    button.style.height = '2000px';
    button.style.width = '2000px';
    button.style.borderWidth = '8px';
  }");
        await page.ClickAsync("button", new() { Position = new() { X = 1900, Y = 1910 } });
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await page.EvaluateAsync<int>("offsetX"), Is.EqualTo(1900));
        Assert.That(await page.EvaluateAsync<int>("offsetY"), Is.EqualTo(1910));
    }

    [UpstreamTest("CL-042 should wait for stable position")]
    public async Task ShouldWaitForStablePosition(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", @"button => {
    button.style.transition = 'margin 500ms linear 0s';
    button.style.marginLeft = '200px';
    button.style.borderWidth = '0';
    button.style.width = '200px';
    button.style.height = '20px';
    // Set display to ""block"" - otherwise Firefox layouts with non-even
    // values on Linux.
    button.style.display = 'block';
    document.body.style.margin = '0';
  }");
        // rafraf for Firefox to kick in the animation.
        await Rafraf(page);
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        Assert.That(await page.EvaluateAsync<int>("pageX"), Is.EqualTo(300));
        Assert.That(await page.EvaluateAsync<int>("pageY"), Is.EqualTo(10));
    }

    [UpstreamTest("CL-043 should wait for becoming hit target")]
    public async Task ShouldWaitForBecomingHitTarget(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", @"button => {
    button.style.borderWidth = '0';
    button.style.width = '200px';
    button.style.height = '20px';
    document.body.style.margin = '0';
    document.body.style.position = 'relative';
    const flyOver = document.createElement('div');
    flyOver.className = 'flyover';
    flyOver.style.position = 'absolute';
    flyOver.style.width = '400px';
    flyOver.style.height = '20px';
    flyOver.style.left = '-200px';
    flyOver.style.top = '0';
    flyOver.style.background = 'red';
    document.body.appendChild(flyOver);
  }");
        var clicked = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("button");
            clicked = true;
        }
        var clickPromise = ClickPromise();
        Assert.That(clicked, Is.EqualTo(false));

        await page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '0'");
        await GiveItAChanceToClick(page);
        Assert.That(clicked, Is.EqualTo(false));

        await page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '200px'");
        await clickPromise;
        Assert.That(clicked, Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-044 should wait for becoming hit target with trial run")]
    public async Task ShouldWaitForBecomingHitTargetWithTrialRun(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", @"button => {
    button.style.borderWidth = '0';
    button.style.width = '200px';
    button.style.height = '20px';
    document.body.style.margin = '0';
    document.body.style.position = 'relative';
    const flyOver = document.createElement('div');
    flyOver.className = 'flyover';
    flyOver.style.position = 'absolute';
    flyOver.style.width = '400px';
    flyOver.style.height = '20px';
    flyOver.style.left = '-200px';
    flyOver.style.top = '0';
    flyOver.style.background = 'red';
    document.body.appendChild(flyOver);
  }");
        var clicked = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("button", new() { Trial = true });
            clicked = true;
        }
        var clickPromise = ClickPromise();
        Assert.That(clicked, Is.EqualTo(false));

        await page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '0'");
        await GiveItAChanceToClick(page);
        Assert.That(clicked, Is.EqualTo(false));

        await page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '200px'");
        await clickPromise;
        Assert.That(clicked, Is.EqualTo(true));

        // Should not actually click.
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-045 trial run should work with short timeout")]
    public async Task TrialRunShouldWorkWithShortTimeout(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", "button => button.disabled = true");
        var error = await Catch(() => page.ClickAsync("button", new() { Trial = true, Timeout = 2000 }));
        Assert.That(error.Message, Does.Contain("click action (trial run)"));
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-046 trial run should not click")]
    public async Task TrialRunShouldNotClick(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button", new() { Trial = true });
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-047 trial run should not double click")]
    public async Task TrialRunShouldNotDoubleClick(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync(@"() => {
    window['double'] = false;
    const button = document.querySelector('button');
    button.addEventListener('dblclick', event => {
      window['double'] = true;
    });
  }");
        await page.DblClickAsync("button", new() { Trial = true });
        Assert.That(await page.EvaluateAsync<bool>("double"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-048 should fail when obscured and not waiting for hit target")]
    public async Task ShouldFailWhenObscuredAndNotWaitingForHitTarget(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        var button = await page.QuerySelectorAsync("button");
        await page.EvaluateAsync(@"() => {
    document.body.style.position = 'relative';
    const blocker = document.createElement('div');
    blocker.style.position = 'absolute';
    blocker.style.width = '400px';
    blocker.style.height = '20px';
    blocker.style.left = '0';
    blocker.style.top = '0';
    document.body.appendChild(blocker);
  }");
        await button.ClickAsync(new() { Force = true });
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-049 should wait for button to be enabled")]
    public async Task ShouldWaitForButtonToBeEnabled(IPage page)
    {
        await page.SetContentAsync("<button onclick=\"window.__CLICKED=true;\" disabled><span>Click target</span></button>");
        var done = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("text=Click target");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('button').removeAttribute('disabled')");
        await clickPromise;
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-050 should wait for element to become non-inert")]
    public async Task ShouldWaitForElementToBecomeNonInert(IPage page)
    {
        await page.SetContentAsync("<div inert id=container><button onclick=\"window.__CLICKED=true;\"><span>Click target</span></button></div>");
        var done = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("text=Click target");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.getElementById('container').removeAttribute('inert')");
        await clickPromise;
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-052 should wait for input to be enabled")]
    public async Task ShouldWaitForInputToBeEnabled(IPage page)
    {
        await page.SetContentAsync("<input onclick=\"window.__CLICKED=true;\" disabled>");
        var done = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("input");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('input').removeAttribute('disabled')");
        await clickPromise;
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-053 should wait for select to be enabled")]
    public async Task ShouldWaitForSelectToBeEnabled(IPage page)
    {
        await page.SetContentAsync(@"
    <select disabled><option selected>Hello</option></select>
    <script>
      document.querySelector('select').addEventListener('mousedown', event => {
        window.__CLICKED=true;
        event.preventDefault();
      });
    </script>
  ");
        var done = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("select");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('select').removeAttribute('disabled')");
        await clickPromise;
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-054 should click disabled div")]
    public async Task ShouldClickDisabledDiv(IPage page)
    {
        await page.SetContentAsync("<div onclick=\"window.__CLICKED=true\" disabled>Click target</div>");
        await page.ClickAsync("text=Click target");
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-055 should wait for BUTTON to be clickable when it has pointer-events:none")]
    public async Task ShouldWaitForBUTTONToBeClickableWhenItHasPointerEventsNone(IPage page)
    {
        await page.SetContentAsync("<button onclick=\"window.__CLICKED=true\" style=\"pointer-events:none\"><span>Click target</span></button>");
        var done = false;
        async Task ClickPromise()
        {
            await page.ClickAsync("text=Click target");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(page);
        Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('button').style.removeProperty('pointer-events')");
        await clickPromise;
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-056 should wait for LABEL to be clickable when it has pointer-events:none")]
    public async Task ShouldWaitForLABELToBeClickableWhenItHasPointerEventsNone(IPage page)
    {
        await page.SetContentAsync("<label onclick=\"window.__CLICKED=true\" style=\"pointer-events:none\"><span>Click target</span></label>");
        var clickPromise = page.ClickAsync("text=Click target");
        // Do a few roundtrips to the page.
        for (var i = 0; i < 5; ++i)
        {
            Assert.That(await page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        }
        // remove `pointer-events: none` css from button.
        await page.EvaluateAsync("() => document.querySelector('label').style.removeProperty('pointer-events')");
        await clickPromise;
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-057 should update modifiers correctly")]
    public async Task ShouldUpdateModifiersCorrectly(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.ClickAsync("button", new() { Modifiers = [KeyboardModifier.Shift] });
        Assert.That(await page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(true));
        await page.ClickAsync("button", new() { Modifiers = [] });
        Assert.That(await page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(false));

        await page.Keyboard.DownAsync("Shift");
        await page.ClickAsync("button", new() { Modifiers = [] });
        Assert.That(await page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(false));
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(true));
        await page.Keyboard.UpAsync("Shift");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(false));
    }

    [UpstreamTest("CL-058 should click an offscreen element when scroll-behavior is smooth")]
    public async Task ShouldClickAnOffscreenElementWhenScrollBehaviorIsSmooth(IPage page)
    {
        await page.SetContentAsync(@"
    <div style=""border: 1px solid black; height: 500px; overflow: auto; width: 500px; scroll-behavior: smooth"">
    <button style=""margin-top: 2000px"" onClick=""window.clicked = true"">hi</button>
    </div>
  ");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<bool>("window.clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-059 should report nice error when element is detached and force-clicked")]
    public async Task ShouldReportNiceErrorWhenElementIsDetachedAndForceClicked(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/animating-button.html");
        await page.EvaluateAsync("addButton()");
        var handle = await page.QuerySelectorAsync("button");
        await page.EvaluateAsync("stopButton(true)");
        var promise = Catch(() => handle.ClickAsync(new() { Force = true }));
        var error = await promise;
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("CL-060 should fail when element detaches after animation")]
    public async Task ShouldFailWhenElementDetachesAfterAnimation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/animating-button.html");
        await page.EvaluateAsync("addButton()");
        var handle = await page.QuerySelectorAsync("button");
        var promise = Catch(() => handle.ClickAsync());
        await page.EvaluateAsync("stopButton(true)");
        var error = await promise;
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("CL-061 should retry when element detaches after animation")]
    public async Task ShouldRetryWhenElementDetachesAfterAnimation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/animating-button.html");
        await page.EvaluateAsync("addButton()");
        var clicked = false;
        async Task Promise()
        {
            await page.ClickAsync("button");
            clicked = true;
        }
        var promise = Promise();
        Assert.That(clicked, Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
        await page.EvaluateAsync("stopButton(true)");
        await page.EvaluateAsync("addButton()");
        Assert.That(clicked, Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
        await page.EvaluateAsync("stopButton(true)");
        await page.EvaluateAsync("addButton()");
        Assert.That(clicked, Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
        await page.EvaluateAsync("stopButton(false)");
        await promise;
        Assert.That(clicked, Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-062 should retry when element is animating from outside the viewport")]
    public async Task ShouldRetryWhenElementIsAnimatingFromOutsideTheViewport(IPage page)
    {
        await page.SetContentAsync(@"<style>
    @keyframes move {
      from { left: -300px; }
      to { left: 0; }
    }
    button {
      position: absolute;
      left: -300px;
      top: 0;
      bottom: 0;
      width: 200px;
    }
    button.animated {
      animation: 1s linear 1s move forwards;
    }
    </style>
    <div style=""position: relative; width: 300px; height: 300px;"">
      <button onclick=""window.clicked=true""></button>
    </div>
  ");
        var handle = await page.QuerySelectorAsync("button");
        var promise = handle.ClickAsync();
        await handle.EvaluateAsync("button => button.className = 'animated'");
        await promise;
        Assert.That(await page.EvaluateAsync<bool>("clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-063 should fail when element is animating from outside the viewport with force")]
    public async Task ShouldFailWhenElementIsAnimatingFromOutsideTheViewportWithForce(IPage page)
    {
        await page.SetContentAsync(@"<style>
    @keyframes move {
      from { left: -300px; }
      to { left: 0; }
    }
    button {
      position: absolute;
      left: -300px;
      top: 0;
      bottom: 0;
      width: 200px;
    }
    button.animated {
      animation: 1s linear 1s move forwards;
    }
    </style>
    <div style=""position: relative; width: 300px; height: 300px;"">
      <button onclick=""window.clicked=true""></button>
    </div>
  ");
        var handle = await page.QuerySelectorAsync("button");
        var promise = Catch(() => handle.ClickAsync(new() { Force = true }));
        await handle.EvaluateAsync("button => button.className = 'animated'");
        var error = await promise;
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("Element is outside of the viewport"));
    }

    [UpstreamTest("CL-064 should dispatch microtasks in order")]
    public async Task ShouldDispatchMicrotasksInOrder(IPage page)
    {
        await page.SetContentAsync(@"
    <button id=button>Click me</button>
    <script>
      let mutationCount = 0;
      const observer = new MutationObserver((mutationsList, observer) => {
        for(let mutation of mutationsList)
          ++mutationCount;
      });
      observer.observe(document.body, { attributes: true, childList: true, subtree: true });
      button.addEventListener('mousedown', () => {
        mutationCount = 0;
        document.body.appendChild(document.createElement('div'));
      });
      button.addEventListener('mouseup', () => {
        window['result'] = mutationCount;
      });
    </script>
  ");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(1));
    }

    [UpstreamTest("CL-065 should click the button when window.innerWidth is corrupted")]
    public async Task ShouldClickTheButtonWhenWindowInnerWidthIsCorrupted(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvaluateAsync("() => Object.defineProperty(window, 'innerWidth', { value: 0 })");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-066 should click zero-sized input by label")]
    public async Task ShouldClickZeroSizedInputByLabel(IPage page)
    {
        await page.SetContentAsync(@"
    <label>
      Click me
      <input onclick=""window.__clicked=true"" style=""width:0;height:0;padding:0;margin:0;border:0;"">
    </label>
  ");
        await page.ClickAsync("text=Click me");
        Assert.That(await page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-070 should climb dom for inner label with pointer-events:none")]
    public async Task ShouldClimbDomForInnerLabelWithPointerEventsNone(IPage page)
    {
        await page.SetContentAsync("<button onclick=\"window.__CLICKED=true;\"><label style=\"pointer-events:none\">Click target</label></button>");
        await page.ClickAsync("text=Click target");
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-071 should climb up to [role=button]")]
    public async Task ShouldClimbUpToRoleButton(IPage page)
    {
        await page.SetContentAsync("<div role=button onclick=\"window.__CLICKED=true;\"><div style=\"pointer-events:none\"><span><div>Click target</div></span></div>");
        await page.ClickAsync("text=Click target");
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-072 should climb up to a anchor")]
    public async Task ShouldClimbUpToAAnchor(IPage page)
    {
        // For Firefox its not allowed to return anything: https://bugzilla.mozilla.org/show_bug.cgi?id=1392046
        // Note the intermediate div - it is necessary, otherwise <a><non-clickable/></a> is not recognized as a clickable link.
        await page.SetContentAsync("<a href=\"#\" onclick=\"window.__CLICKED=true\" id=\"outer\"><div id=\"intermediate\"><div id=\"inner\" style=\"pointer-events: none\">Inner</div></div></a>");
        await page.ClickAsync("#inner");
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-073 should climb up to a [role=link]")]
    public async Task ShouldClimbUpToARoleLink(IPage page)
    {
        await page.SetContentAsync("<div role=link onclick=\"window.__CLICKED=true\" id=\"outer\"><div id=\"inner\" style=\"pointer-events: none\">Inner</div></div>");
        await page.ClickAsync("#inner");
        Assert.That(await page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-074 should click in an iframe with border")]
    public async Task ShouldClickInAnIframeWithBorder(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; box-sizing: border-box; }
      iframe { border: 4px solid black; background: gray; margin-left: 33px; margin-top: 24px; width: 400px; height: 400px; }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { margin-left: 10px; margin-top: 20px; width: 2px; height: 2px; }
      </style>
      <div>Target</div>
      <script>
        document.querySelector('div').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync();
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-075 should click in an iframe with border 2")]
    public async Task ShouldClickInAnIframeWithBorder2(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; }
      iframe { border: 4px solid black; background: gray; margin-left: 33px; margin-top: 24px; width: 400px; height: 400px; }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { margin-left: 10px; margin-top: 20px; width: 2px; height: 2px; }
      </style>
      <div>Target</div>
      <script>
        document.querySelector('div').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync();
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-076 should not retain removed iframe after clicking inside it")]
    public async Task ShouldNotRetainRemovedIframeAfterClickingInsideIt(IPage page, bool isAndroid)
    {
        It.Skip(isAndroid, "requesting gc is not enough");

        await page.SetContentAsync("<iframe srcdoc=\"<button>Click</button>\"></iframe>");
        var button = page.FrameLocator("iframe").GetByRole(AriaRole.Button);
        await button.WaitForAsync();
        await page.EvaluateAsync(@"() => {
    window.iframeRef = new WeakRef(document.querySelector('iframe'));
  }");
        await button.ClickAsync();
        await page.EvaluateAsync("() => document.querySelector('iframe').remove()");
        // Move the mouse away to release Chromium's own last-hovered-node retention.
        await page.Mouse.MoveAsync(500, 500);
        await page.RequestGCAsync();
        var retained = await page.EvaluateAsync<bool>("() => Boolean(window.iframeRef.deref())");
        Assert.That(retained, Is.EqualTo(false));
    }

    [UpstreamTest("CL-077 should click in a transformed iframe")]
    public async Task ShouldClickInATransformedIframe(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; }
      iframe {
        border: 4px solid black;
        background: gray;
        margin-left: 33px;
        margin-top: 24px;
        width: 400px;
        height: 400px;
        transform: translate(100px, 100px) scale(1.2) rotate3d(1, 1, 1, 25deg);
      }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { margin-left: 10px; margin-top: 20px; width: 2px; height: 2px; }
      </style>
      <div>Target</div>
      <script>
        document.querySelector('div').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync();
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-078 should click a button that is overlaid by a permission popup")]
    public async Task ShouldClickAButtonThatIsOverlaidByAPermissionPopup(IPage page, Server server)
    {
        await page.SetViewportSizeAsync(500, 500);
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <style>body, html { padding: 0; margin: 0; }</style>
    <script type='text/javascript'>
      window.addEventListener('DOMContentLoaded', () => {
        // Viewport filled with buttons.
        for (let i = 0; i < 100; ++i) {
          const button = document.createElement('button');
          button.textContent = i;
          button.style.setProperty('width', '50px');
          button.style.setProperty('height', '50px');
          document.body.append(button);
        }
      }, false);
    </script>
  ");
        // Issue a geolocation request. This should show a browser popup.
        // NOTE: this is a bit racy since we can't wait for the geolocation
        // popup to be shown.
        await page.EvaluateAsync(@"() => {
    navigator.geolocation.getCurrentPosition(position => { });
  }");
        // If popup blocks the click, then some of the `page.click` calls below will hang.
        for (var i = 0; i < 30; ++i)
        {
            await page.ClickAsync($"text={i}");
        }
    }

    [UpstreamTest("CL-079 should click in a transformed iframe with force")]
    public async Task ShouldClickInATransformedIframeWithForce(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; }
      iframe { background: gray; margin-left: 33px; margin-top: 24px; width: 400px; height: 400px; transform: translate(-40px, -40px) scale(0.8); }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { margin-left: 10px; margin-top: 20px; width: 2px; height: 2px; }
      </style>
      <div>Target</div>
      <script>
        document.querySelector('div').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync(new() { Force = true });
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-080 should click in a nested transformed iframe")]
    public async Task ShouldClickInANestedTransformedIframe(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; box-sizing: border-box; }
      iframe { border: 1px solid black; background: gray; margin-left: 33px; margin-top: 24px; width: 400px; height: 400px; transform: scale(0.8); }
    </style>
    <iframe srcdoc=""
      <style>
        body, html, iframe { margin: 0; padding: 0; box-sizing: border-box; }
        iframe { border: 3px solid black; background: gray; margin-left: 18px; margin-top: 14px; width: 200px; height: 200px; transform: scale(0.7); }
      </style>
      <iframe srcdoc='
        <style>
          div { margin-left: 10px; margin-top: 20px; width: 2px; height: 2px; }
        </style>
        <div>Target</div>
      '></iframe>
    ""></iframe>
  ");
        var locator = page.FrameLocator("iframe").FrameLocator("iframe").Locator("div");
        await locator.EvaluateAsync(@"div => {
    div.addEventListener('click', () => window.top['_clicked'] = true);
  }");
        await locator.ClickAsync();
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-081 ensure events are dispatched in the individual tasks")]
    public async Task EnsureEventsAreDispatchedInTheIndividualTasks(IPage page, string browserName)
    {
        await page.SetContentAsync(@"
    <div id=""outer"" style=""background: #d4d4d4; width: 60px; height: 60px;"">
      <div id=""inner"" style=""background: #adadad; width: 46px; height: 46px;""></div>
    </div>
  ");

        await page.EvaluateAsync(@"() => {
    function onClick(name) {
      console.log(`click ${name}`);

      window.builtins.setTimeout(function() {
        console.log(`timeout ${name}`);
      }, 0);

      void Promise.resolve().then(function() {
        console.log(`promise ${name}`);
      });
    }

    document.getElementById('inner').addEventListener('click', () => onClick('inner'));
    document.getElementById('outer').addEventListener('click', () => onClick('outer'));
  }");

        // Capture console messages
        var messages = new List<string>();
        page.Console += (_, msg) => messages.Add(msg.Text);

        // Click on the inner div element
        await page.Locator("#inner").ClickAsync();

        await Expect(() => Assert.That(messages, Is.EqualTo(new[]
        {
            "click inner",
            "promise inner",
            "click outer",
            "promise outer",
            "timeout inner",
            "timeout outer",
        }))).ToPassAsync();
    }

    [UpstreamTest("CL-082 should click if opened select covers the button")]
    public async Task ShouldClickIfOpenedSelectCoversTheButton(IPage page)
    {
        await page.SetContentAsync(@"
    <div>
      <select>
        <option>very long text #1</option>
        <option>very long text #2</option>
        <option>very long text #3</option>
        <option>very long text #4</option>
        <option>very long text #5</option>
        <option>very long text #6</option>
      </select>
    </div>
    <div>
      <button onclick=""window.__CLICKED=42"">clickme</button>
    </div>
  ");
        await page.ClickAsync("select");
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<int>("window.__CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-083 should fire contextmenu event on right click in correct order")]
    public async Task ShouldFireContextmenuEventOnRightClickInCorrectOrder(IPage page, Server server, string browserName, bool isWindows)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <button id=""target"">Click me</button>
  ");
        await page.EvaluateAsync(@"() => {
    const logEvent = e => console.log(e.type);
    document.addEventListener('mousedown', logEvent);
    document.addEventListener('mouseup', logEvent);
    document.addEventListener('contextmenu', logEvent);
  }");
        var entries = new List<string>();
        page.Console += (_, message) => entries.Add(message.Text);
        await page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync(new() { Button = MouseButton.Right });
        if (browserName == "chromium" && isWindows)
        {
            await Expect(() => Assert.That(entries, Is.EqualTo(new[] { "mousedown", "mouseup", "contextmenu" }))).ToPassAsync();
        }
        else
        {
            await Expect(() => Assert.That(entries, Is.EqualTo(new[] { "mousedown", "contextmenu", "mouseup" }))).ToPassAsync();
        }
    }

    [UpstreamTest("CL-084 should click after a right click")]
    public async Task ShouldClickAfterARightClick(IPage page)
    {
        await page.SetContentAsync(@"
    <button>Click me</button>
    <script>
      const button = document.querySelector('button');
      button.addEventListener('click', () => button.textContent = 'Clicked!');
    </script>
  ");
        await page.GetByRole(AriaRole.Button).ClickAsync(new() { Button = MouseButton.Right });
        await page.GetByRole(AriaRole.Button).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Button)).ToHaveTextAsync("Clicked!");
    }

    [UpstreamTest("CL-085 should set PointerEvent.pressure on pointerdown")]
    public async Task ShouldSetPointerEventPressureOnPointerdown(IPage page, bool isLinux, bool headless)
    {
        await page.SetContentAsync(@"
    <button id=""target"">Click me</button>
    <script>
      window['pressures'] = [];
      document.addEventListener('pointerdown', e => window['pressures'].push(['pointerdown', e.pressure]));
      document.addEventListener('pointerup', e => window['pressures'].push(['pointerup', e.pressure]));
    </script>
  ");
        await page.ClickAsync("button");
        ExpectToEqual(await page.EvaluateAsync<object>("() => window['pressures']"), ArrayContaining(new object[]
        {
            new object[] { "pointerdown", 0.5 },
            new object[] { "pointerup", 0 },
        }));
    }

    [UpstreamTest("CL-086 should set PointerEvent.pressure on pointermove")]
    public async Task ShouldSetPointerEventPressureOnPointermove(IPage page, bool isLinux, bool headless, bool isWindows, string browserName, bool isAndroid)
    {
        It.Fixme(isAndroid, "Android coordinates seem to have rounding issues.");
        await page.SetContentAsync(@"
    <body style=""margin: 0; padding: 0;"">
      <div id=""target"" style=""width: 500px; height: 500px; background-color: red;""></div>
      <script>
        window['pressures'] = [];
        document.addEventListener('pointermove', e => window['pressures'].push([e.pressure, e.clientX, e.clientY]));
      </script>
    </body>
  ");
        await page.ClickAsync("div#target");
        await page.Mouse.MoveAsync(10, 10);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(250, 250);
        await page.Mouse.UpAsync();
        await page.Mouse.MoveAsync(50, 50);
        ExpectToEqual(await page.EvaluateAsync<object>("() => window['pressures']"), ArrayContaining(new object[]
        {
            new object[] { 0, 250, 250 },
            new object[] { 0, 10, 10 },
            new object[] { 0.5, 250, 250 },
            new object[] { 0, 50, 50 },
        }));
    }

    [UpstreamTest("CL-087 should click into shadow root with slotted div")]
    public async Task ShouldClickIntoShadowRootWithSlottedDiv(IPage page)
    {
        await page.SetContentAsync(@"
    <my-button>
      <template shadowrootmode=""open"">
        <button><slot></slot></button>
      </template>
      <div>Foo</div>
    </my-button>
  ");

        await page.GetByRole(AriaRole.Button, new() { Name = "Foo" }).ClickAsync();
    }

    [UpstreamTest("CL-088 should click shadow root button")]
    public async Task ShouldClickShadowRootButton(IPage page)
    {
        await page.SetContentAsync(@"
    <my-button>
      <template shadowrootmode=""open"">
        <button><slot></slot></button>
      </template>
      <div>Foo</div>
    </my-button>
  ");

        await page.Locator("my-button").ClickAsync();
    }

    [UpstreamTest("CL-089 should click with tweened mouse movement")]
    public async Task ShouldClickWithTweenedMouseMovement(IPage page, string browserName, bool isAndroid, bool headless)
    {
        It.Skip(isAndroid, "Bad rounding");
        It.Skip(!headless, "System cursor tends to interfere with this test");

        await page.SetContentAsync(@"
    <body style=""margin: 0; padding: 0; height: 500px; width: 500px;"">
      <div style=""position: relative; top: 280px; left: 150px; width: 100px; height: 40px"">Click me</div>
    </body>
  ");

        // The test becomes flaky on WebKit without next line.
        if (browserName == "webkit")
        {
            await page.EvaluateAsync("() => new Promise(requestAnimationFrame)");
        }
        await page.Mouse.MoveAsync(100, 100);
        await page.EvaluateAsync(@"() => {
    window['result'] = [];
    document.addEventListener('mousemove', event => {
      window['result'].push([event.clientX, event.clientY]);
    });
  }");
        // Centerpoint at 150 + 100/2, 280 + 40/2 = 200, 300
        await page.Locator("div").ClickAsync(new() { Steps = 5 });
        ExpectToEqual(await page.EvaluateAsync<object>("result"), new[]
        {
            new[] { 120, 140 },
            new[] { 140, 180 },
            new[] { 160, 220 },
            new[] { 180, 260 },
            new[] { 200, 300 },
        });
    }
}
