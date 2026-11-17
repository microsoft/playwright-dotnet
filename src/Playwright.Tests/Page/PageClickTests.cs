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
public class PageClickTests : UpstreamPageTest
{
    private static Task GiveItAChanceToClick(IPage page) => Rafraf(page, 5);

    [UpstreamTest("CL-001 should click the button @smoke")]
    public async Task ShouldClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-002 should click button inside frameset")]
    public async Task ShouldClickButtonInsideFrameset()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/frameset.html");
        var frameElement = await Page.QuerySelectorAsync("frame");
        await frameElement.EvaluateAsync("frame => frame.src = '/input/button.html'");
        var frame = await frameElement.ContentFrameAsync();
        await frame.ClickAsync("button");
        Assert.That(await frame.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-003 should issue clicks in parallel in page and popup")]
    public async Task ShouldIssueClicksInParallelInPageAndPopup()
    {
        await Page.GotoAsync(Server.Prefix + "/counter.html");
        var (popup, _) = await PromiseAll(
            Page.WaitForPopupAsync(),
            Page.EvaluateAsync("() => window.open('/counter.html')"));
        var clickPromises = new List<Task>();
        for (var i = 0; i < 21; ++i)
        {
            if (i % 3 == 0)
            {
                clickPromises.Add(popup.Locator("button").ClickAsync());
            }
            else
            {
                clickPromises.Add(Page.Locator("button").ClickAsync());
            }
        }
        await PromiseAll(clickPromises);
        Assert.That(await Page.EvaluateAsync<int>("() => window['count']"), Is.EqualTo(14));
        Assert.That(await popup.EvaluateAsync<int>("() => window['count']"), Is.EqualTo(7));
    }

    [UpstreamTest("CL-004 should click svg")]
    public async Task ShouldClickSvg()
    {
        await Page.SetContentAsync(@"
    <svg height=""100"" width=""100"">
      <circle onclick=""window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red"" />
    </svg>
  ");
        await Page.ClickAsync("circle");
        Assert.That(await Page.EvaluateAsync<int>("__CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-005 should click the button if window.Node is removed")]
    public async Task ShouldClickTheButtonIfWindowNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync("() => delete window.Node");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    // @see https://github.com/GoogleChrome/puppeteer/issues/4281
    [UpstreamTest("CL-006 should click on a span with an inline element inside")]
    public async Task ShouldClickOnASpanWithAnInlineElementInside()
    {
        await Page.SetContentAsync(@"
    <style>
    span::before {
      content: 'q';
    }
    </style>
    <span onclick='window.CLICKED=42'></span>
  ");
        await Page.ClickAsync("span");
        Assert.That(await Page.EvaluateAsync<int>("CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-007 should click the aligned 1x1 div")]
    public async Task ShouldClickTheAligned1x1Div()
    {
        await Page.SetContentAsync("<div style=\"width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-008 should click the half-aligned 1x1 div")]
    public async Task ShouldClickTheHalfAligned1x1Div()
    {
        await Page.SetContentAsync("<div style=\"margin-left: 20.5px; margin-top: 11.5px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-009 should click the unaligned 1x1 div v1")]
    public async Task ShouldClickTheUnaligned1x1DivV1()
    {
        await Page.SetContentAsync("<div style=\"margin-left: 20.23px; margin-top: 11.65px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-010 should click the unaligned 1x1 div v2")]
    public async Task ShouldClickTheUnaligned1x1DivV2()
    {
        await Page.SetContentAsync("<div style=\"margin-left: 20.68px; margin-top: 11.13px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-011 should click the unaligned 1x1 div v3")]
    public async Task ShouldClickTheUnaligned1x1DivV3()
    {
        await Page.SetContentAsync("<div style=\"margin-left: 20.68px; margin-top: 11.52px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-012 should click the unaligned 1x1 div v4")]
    public async Task ShouldClickTheUnaligned1x1DivV4()
    {
        await Page.SetContentAsync("<div style=\"margin-left: 20.15px; margin-top: 11.24px; width: 1px; height: 1px;\" onclick=\"window.__clicked = true\"></div>");
        await Page.ClickAsync("div");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-013 should click the button after navigation ")]
    public async Task ShouldClickTheButtonAfterNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-014 should click the button after a cross origin navigation ")]
    public async Task ShouldClickTheButtonAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-015 should click when one of inline box children is outside of viewport")]
    public async Task ShouldClickWhenOneOfInlineBoxChildrenIsOutsideOfViewport()
    {
        await Page.SetContentAsync(@"
    <style>
    i {
      position: absolute;
      top: -1000px;
    }
    </style>
    <span onclick='window.CLICKED = 42;'><i>woof</i><b>doggo</b></span>
  ");
        await Page.ClickAsync("span");
        Assert.That(await Page.EvaluateAsync<int>("CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-016 should select the text by triple clicking")]
    public async Task ShouldSelectTheTextByTripleClicking()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        const string text = "This is the text that we are going to try to select. Let's see how it goes.";
        await Page.FillAsync("textarea", text);
        await Page.ClickAsync("textarea", new() { ClickCount = 3 });
        Assert.That(
            await Page.EvaluateAsync<string>(@"() => {
    const textarea = document.querySelector('textarea');
    return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
  }"),
            Is.EqualTo(text));
    }

    [UpstreamTest("CL-017 should click offscreen buttons")]
    public async Task ShouldClickOffscreenButtons()
    {
        await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");
        var messages = new List<string>();
        Page.Console += (_, msg) => messages.Add(msg.Text);
        for (var i = 0; i < 11; ++i)
        {
            // We might've scrolled to click a button - reset to (0, 0).
            await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await Page.ClickAsync($"#btn{i}");
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
    public async Task ShouldWaitForVisibleWhenAlreadyVisible()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-019 should not wait with force")]
    public async Task ShouldNotWaitWithForce()
    {
        Exception error = null;
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        error = await Catch(() => Page.ClickAsync("button", new() { Force = true }));
        Assert.That(error.Message, Does.Contain("Element is not visible"));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-020 should waitFor display:none to be gone")]
    public async Task ShouldWaitForDisplayNoneToBeGone()
    {
        var done = false;
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        async Task Clicked()
        {
            await Page.ClickAsync("button", new() { Timeout = 0 });
            done = true;
        }
        var clicked = Clicked();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
        Assert.That(done, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'block'");
        await clicked;
        Assert.That(done, Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-021 should waitFor visibility:hidden to be gone")]
    public async Task ShouldWaitForVisibilityHiddenToBeGone()
    {
        var done = false;
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
        async Task Clicked()
        {
            await Page.ClickAsync("button", new() { Timeout = 0 });
            done = true;
        }
        var clicked = Clicked();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
        Assert.That(done, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'visible'");
        await clicked;
        Assert.That(done, Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-022 should waitFor visible when parent is hidden")]
    public async Task ShouldWaitForVisibleWhenParentIsHidden()
    {
        var done = false;
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'none'");
        async Task Clicked()
        {
            await Page.ClickAsync("button", new() { Timeout = 0 });
            done = true;
        }
        var clicked = Clicked();
        await GiveItAChanceToClick(Page);
        Assert.That(done, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'block'");
        await clicked;
        Assert.That(done, Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-023 should click wrapped links")]
    public async Task ShouldClickWrappedLinks()
    {
        await Page.GotoAsync(Server.Prefix + "/wrappedlink.html");
        await Page.ClickAsync("a");
        Assert.That(await Page.EvaluateAsync<bool>("__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-024 should click on checkbox input and toggle")]
    public async Task ShouldClickOnCheckboxInputAndToggle()
    {
        await Page.GotoAsync(Server.Prefix + "/input/checkbox.html");
        Assert.That(await Page.EvaluateAsync<object>("() => window['result'].check"), Is.Null);
        await Page.ClickAsync("input#agree");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(true));
        var events = await Page.EvaluateAsync<string[]>("() => window['result'].events");
        if (!Headless)
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
        await Page.ClickAsync("input#agree");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(false));
    }

    [UpstreamTest("CL-025 should click on checkbox label and toggle")]
    public async Task ShouldClickOnCheckboxLabelAndToggle()
    {
        await Page.GotoAsync(Server.Prefix + "/input/checkbox.html");
        Assert.That(await Page.EvaluateAsync<object>("() => window['result'].check"), Is.Null);
        await Page.ClickAsync("label[for=\"agree\"]");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].events"), Is.EqualTo(new[]
        {
            "click",
            "input",
            "change",
        }));
        await Page.ClickAsync("label[for=\"agree\"]");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['result'].check"), Is.EqualTo(false));
    }

    [UpstreamTest("CL-026 should scroll and click the button")]
    public async Task ShouldScrollAndClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.ClickAsync("#button-5");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('#button-5').textContent"), Is.EqualTo("clicked"));
        await Page.ClickAsync("#button-80");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('#button-80').textContent"), Is.EqualTo("clicked"));
    }

    [UpstreamTest("CL-027 should scroll and click the button with smooth scroll behavior")]
    public async Task ShouldScrollAndClickTheButtonWithSmoothScrollBehavior()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.AddStyleTagAsync(new() { Content = "html { scroll-behavior: smooth; }" });
        for (var i = 0; i < 5; i++)
        {
            await Page.ClickAsync("#button-80");
            Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('#button-80').textContent"), Is.EqualTo("clicked"));
            await Page.ClickAsync("#button-20");
            Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('#button-20').textContent"), Is.EqualTo("clicked"));
        }
    }

    [UpstreamTest("CL-028 should double click the button")]
    public async Task ShouldDoubleClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
    window['double'] = false;
    const button = document.querySelector('button');
    button.addEventListener('dblclick', event => {
      window['double'] = true;
    });
  }");
        await Page.DblClickAsync("button");
        Assert.That(await Page.EvaluateAsync<bool>("double"), Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-029 should click a partially obscured button")]
    public async Task ShouldClickAPartiallyObscuredButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
    const button = document.querySelector('button');
    button.textContent = 'Some really long text that will go offscreen';
    button.style.position = 'absolute';
    button.style.left = '368px';
  }");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-030 should click a rotated button")]
    public async Task ShouldClickARotatedButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/rotatedButton.html");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-031 should fire contextmenu event on right click")]
    public async Task ShouldFireContextmenuEventOnRightClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.ClickAsync("#button-8", new() { Button = MouseButton.Right });
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('#button-8').textContent"), Is.EqualTo("context menu"));
    }

    [UpstreamTest("CL-032 should click links which cause navigation")]
    public async Task ShouldClickLinksWhichCauseNavigation()
    {
        // @see https://github.com/GoogleChrome/puppeteer/issues/206
        await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">empty.html</a>");
        // This await should not hang.
        await Page.ClickAsync("a");
    }

    [UpstreamTest("CL-033 should not wait for navigation after clicking a link answered with 204")]
    public async Task ShouldNotWaitForNavigationAfterClickingALinkAnsweredWith204()
    {
        Server.SetRoute("/204", (req, res) =>
        {
            res.StatusCode = 204;
            res.End();
        });
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync($"<a href=\"{Server.Prefix}/204\">link</a>");
        await Page.ClickAsync("a", new() { Timeout = 5000 });
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("CL-034 should click the button inside an iframe")]
    public async Task ShouldClickTheButtonInsideAnIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
        await AttachFrame(Page, "button-test", Server.Prefix + "/input/button.html");
        var frame = Page.Frames[1];
        var button = await frame.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.That(await frame.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-035 should click the button with fixed position inside an iframe")]
    public async Task ShouldClickTheButtonWithFixedPositionInsideAnIframe()
    {
        Info.Fixme(BrowserName == "chromium");

        // @see https://github.com/GoogleChrome/puppeteer/issues/4110
        // @see https://bugs.chromium.org/p/chromium/issues/detail?id=986390
        // @see https://chromium-review.googlesource.com/c/chromium/src/+/1742784
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync("<div style=\"width:100px;height:2000px\">spacer</div>");
        await AttachFrame(Page, "button-test", Server.CrossProcessPrefix + "/input/button.html");
        var frame = Page.Frames[1];
        await frame.EvalOnSelectorAsync("button", "button => button.style.setProperty('position', 'fixed')");
        await frame.ClickAsync("button");
        Assert.That(await frame.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-036 should click the button behind sticky header")]
    public async Task ShouldClickTheButtonBehindStickyHeader()
    {
        await Page.SetViewportSizeAsync(500, 240);
        await Page.SetContentAsync(@"
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
        await Page.EvalOnSelectorAsync("#li14", "e => e.scrollIntoView()");
        await Page.ClickAsync("#target");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-037 should click the button behind position:absolute header")]
    public async Task ShouldClickTheButtonBehindPositionAbsoluteHeader()
    {
        await Page.SetViewportSizeAsync(500, 240);
        await Page.SetContentAsync(@"
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
        await Page.EvalOnSelectorAsync("ol", @"e => {
    const target = document.querySelector('#target');
    e.scrollTo({ top: target.offsetTop, behavior: 'instant' });
  }");
        await Page.ClickAsync("#target");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-038 should click the button with px border with offset")]
    public async Task ShouldClickTheButtonWithPxBorderWithOffset()
    {
        Info.Skip(IsFrozenWebkit, "needs recent webkit");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
        await Page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await Page.EvaluateAsync<int>("offsetX"), Is.EqualTo(20));
        Assert.That(await Page.EvaluateAsync<int>("offsetY"), Is.EqualTo(10));
    }

    [UpstreamTest("CL-039 should click the button with em border with offset")]
    public async Task ShouldClickTheButtonWithEmBorderWithOffset()
    {
        Info.Skip(IsFrozenWebkit, "needs recent webkit");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '2em'");
        await Page.EvalOnSelectorAsync("button", "button => button.style.fontSize = '12px'");
        await Page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await Page.EvaluateAsync<int>("offsetX"), Is.EqualTo(20));
        Assert.That(await Page.EvaluateAsync<int>("offsetY"), Is.EqualTo(10));
    }

    [UpstreamTest("CL-040 should click a very large button with offset")]
    public async Task ShouldClickAVeryLargeButtonWithOffset()
    {
        Info.Fixme(IsAndroid, "Failed to scroll to a particular point");
        Info.Skip(IsFrozenWebkit, "needs recent webkit");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
        await Page.EvalOnSelectorAsync("button", "button => button.style.height = button.style.width = '2000px'");
        await Page.ClickAsync("button", new() { Position = new() { X = 1900, Y = 1910 } });
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await Page.EvaluateAsync<int>("offsetX"), Is.EqualTo(1900));
        Assert.That(await Page.EvaluateAsync<int>("offsetY"), Is.EqualTo(1910));
    }

    [UpstreamTest("CL-041 should click a button in scrolling container with offset")]
    public async Task ShouldClickAButtonInScrollingContainerWithOffset()
    {
        Info.Fixme(IsAndroid, "Failed to scroll to a particular point");
        Info.Skip(IsFrozenWebkit, "needs recent webkit");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
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
        await Page.ClickAsync("button", new() { Position = new() { X = 1900, Y = 1910 } });
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.That(await Page.EvaluateAsync<int>("offsetX"), Is.EqualTo(1900));
        Assert.That(await Page.EvaluateAsync<int>("offsetY"), Is.EqualTo(1910));
    }

    [UpstreamTest("CL-042 should wait for stable position")]
    public async Task ShouldWaitForStablePosition()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
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
        await Rafraf(Page);
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        Assert.That(await Page.EvaluateAsync<int>("pageX"), Is.EqualTo(300));
        Assert.That(await Page.EvaluateAsync<int>("pageY"), Is.EqualTo(10));
    }

    [UpstreamTest("CL-043 should wait for becoming hit target")]
    public async Task ShouldWaitForBecomingHitTarget()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
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
            await Page.ClickAsync("button");
            clicked = true;
        }
        var clickPromise = ClickPromise();
        Assert.That(clicked, Is.EqualTo(false));

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '0'");
        await GiveItAChanceToClick(Page);
        Assert.That(clicked, Is.EqualTo(false));

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '200px'");
        await clickPromise;
        Assert.That(clicked, Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-044 should wait for becoming hit target with trial run")]
    public async Task ShouldWaitForBecomingHitTargetWithTrialRun()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
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
            await Page.ClickAsync("button", new() { Trial = true });
            clicked = true;
        }
        var clickPromise = ClickPromise();
        Assert.That(clicked, Is.EqualTo(false));

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '0'");
        await GiveItAChanceToClick(Page);
        Assert.That(clicked, Is.EqualTo(false));

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '200px'");
        await clickPromise;
        Assert.That(clicked, Is.EqualTo(true));

        // Should not actually click.
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-045 trial run should work with short timeout")]
    public async Task TrialRunShouldWorkWithShortTimeout()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.disabled = true");
        var error = await Catch(() => Page.ClickAsync("button", new() { Trial = true, Timeout = 2000 }));
        Assert.That(error.Message, Does.Contain("click action (trial run)"));
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-046 trial run should not click")]
    public async Task TrialRunShouldNotClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button", new() { Trial = true });
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-047 trial run should not double click")]
    public async Task TrialRunShouldNotDoubleClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
    window['double'] = false;
    const button = document.querySelector('button');
    button.addEventListener('dblclick', event => {
      window['double'] = true;
    });
  }");
        await Page.DblClickAsync("button", new() { Trial = true });
        Assert.That(await Page.EvaluateAsync<bool>("double"), Is.EqualTo(false));
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-048 should fail when obscured and not waiting for hit target")]
    public async Task ShouldFailWhenObscuredAndNotWaitingForHitTarget()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync(@"() => {
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
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Was not clicked"));
    }

    [UpstreamTest("CL-049 should wait for button to be enabled")]
    public async Task ShouldWaitForButtonToBeEnabled()
    {
        await Page.SetContentAsync("<button onclick=\"window.__CLICKED=true;\" disabled><span>Click target</span></button>");
        var done = false;
        async Task ClickPromise()
        {
            await Page.ClickAsync("text=Click target");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('button').removeAttribute('disabled')");
        await clickPromise;
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-050 should wait for element to become non-inert")]
    public async Task ShouldWaitForElementToBecomeNonInert()
    {
        await Page.SetContentAsync("<div inert id=container><button onclick=\"window.__CLICKED=true;\"><span>Click target</span></button></div>");
        var done = false;
        async Task ClickPromise()
        {
            await Page.ClickAsync("text=Click target");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.getElementById('container').removeAttribute('inert')");
        await clickPromise;
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-052 should wait for input to be enabled")]
    public async Task ShouldWaitForInputToBeEnabled()
    {
        await Page.SetContentAsync("<input onclick=\"window.__CLICKED=true;\" disabled>");
        var done = false;
        async Task ClickPromise()
        {
            await Page.ClickAsync("input");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('input').removeAttribute('disabled')");
        await clickPromise;
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-053 should wait for select to be enabled")]
    public async Task ShouldWaitForSelectToBeEnabled()
    {
        await Page.SetContentAsync(@"
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
            await Page.ClickAsync("select");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('select').removeAttribute('disabled')");
        await clickPromise;
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-054 should click disabled div")]
    public async Task ShouldClickDisabledDiv()
    {
        await Page.SetContentAsync("<div onclick=\"window.__CLICKED=true\" disabled>Click target</div>");
        await Page.ClickAsync("text=Click target");
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-055 should wait for BUTTON to be clickable when it has pointer-events:none")]
    public async Task ShouldWaitForBUTTONToBeClickableWhenItHasPointerEventsNone()
    {
        await Page.SetContentAsync("<button onclick=\"window.__CLICKED=true\" style=\"pointer-events:none\"><span>Click target</span></button>");
        var done = false;
        async Task ClickPromise()
        {
            await Page.ClickAsync("text=Click target");
            done = true;
        }
        var clickPromise = ClickPromise();
        await GiveItAChanceToClick(Page);
        Assert.That(await Page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        Assert.That(done, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('button').style.removeProperty('pointer-events')");
        await clickPromise;
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-056 should wait for LABEL to be clickable when it has pointer-events:none")]
    public async Task ShouldWaitForLABELToBeClickableWhenItHasPointerEventsNone()
    {
        await Page.SetContentAsync("<label onclick=\"window.__CLICKED=true\" style=\"pointer-events:none\"><span>Click target</span></label>");
        var clickPromise = Page.ClickAsync("text=Click target");
        // Do a few roundtrips to the page.
        for (var i = 0; i < 5; ++i)
        {
            Assert.That(await Page.EvaluateAsync<object>("window.__CLICKED"), Is.Null);
        }
        // remove `pointer-events: none` css from button.
        await Page.EvaluateAsync("() => document.querySelector('label').style.removeProperty('pointer-events')");
        await clickPromise;
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-057 should update modifiers correctly")]
    public async Task ShouldUpdateModifiersCorrectly()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button", new() { Modifiers = [KeyboardModifier.Shift] });
        Assert.That(await Page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(true));
        await Page.ClickAsync("button", new() { Modifiers = [] });
        Assert.That(await Page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(false));

        await Page.Keyboard.DownAsync("Shift");
        await Page.ClickAsync("button", new() { Modifiers = [] });
        Assert.That(await Page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(false));
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(true));
        await Page.Keyboard.UpAsync("Shift");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<bool>("shiftKey"), Is.EqualTo(false));
    }

    [UpstreamTest("CL-058 should click an offscreen element when scroll-behavior is smooth")]
    public async Task ShouldClickAnOffscreenElementWhenScrollBehaviorIsSmooth()
    {
        await Page.SetContentAsync(@"
    <div style=""border: 1px solid black; height: 500px; overflow: auto; width: 500px; scroll-behavior: smooth"">
    <button style=""margin-top: 2000px"" onClick=""window.clicked = true"">hi</button>
    </div>
  ");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<bool>("window.clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-059 should report nice error when element is detached and force-clicked")]
    public async Task ShouldReportNiceErrorWhenElementIsDetachedAndForceClicked()
    {
        await Page.GotoAsync(Server.Prefix + "/input/animating-button.html");
        await Page.EvaluateAsync("addButton()");
        var handle = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("stopButton(true)");
        var promise = Catch(() => handle.ClickAsync(new() { Force = true }));
        var error = await promise;
        Assert.That(await Page.EvaluateAsync<object>("window.clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("CL-060 should fail when element detaches after animation")]
    public async Task ShouldFailWhenElementDetachesAfterAnimation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/animating-button.html");
        await Page.EvaluateAsync("addButton()");
        var handle = await Page.QuerySelectorAsync("button");
        var promise = Catch(() => handle.ClickAsync());
        await Page.EvaluateAsync("stopButton(true)");
        var error = await promise;
        Assert.That(await Page.EvaluateAsync<object>("window.clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("CL-061 should retry when element detaches after animation")]
    public async Task ShouldRetryWhenElementDetachesAfterAnimation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/animating-button.html");
        await Page.EvaluateAsync("addButton()");
        var clicked = false;
        async Task Promise()
        {
            await Page.ClickAsync("button");
            clicked = true;
        }
        var promise = Promise();
        Assert.That(clicked, Is.EqualTo(false));
        Assert.That(await Page.EvaluateAsync<object>("window.clicked"), Is.Null);
        await Page.EvaluateAsync("stopButton(true)");
        await Page.EvaluateAsync("addButton()");
        Assert.That(clicked, Is.EqualTo(false));
        Assert.That(await Page.EvaluateAsync<object>("window.clicked"), Is.Null);
        await Page.EvaluateAsync("stopButton(true)");
        await Page.EvaluateAsync("addButton()");
        Assert.That(clicked, Is.EqualTo(false));
        Assert.That(await Page.EvaluateAsync<object>("window.clicked"), Is.Null);
        await Page.EvaluateAsync("stopButton(false)");
        await promise;
        Assert.That(clicked, Is.EqualTo(true));
        Assert.That(await Page.EvaluateAsync<bool>("clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-062 should retry when element is animating from outside the viewport")]
    public async Task ShouldRetryWhenElementIsAnimatingFromOutsideTheViewport()
    {
        await Page.SetContentAsync(@"<style>
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
        var handle = await Page.QuerySelectorAsync("button");
        var promise = handle.ClickAsync();
        await handle.EvaluateAsync("button => button.className = 'animated'");
        await promise;
        Assert.That(await Page.EvaluateAsync<bool>("clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-063 should fail when element is animating from outside the viewport with force")]
    public async Task ShouldFailWhenElementIsAnimatingFromOutsideTheViewportWithForce()
    {
        await Page.SetContentAsync(@"<style>
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
        var handle = await Page.QuerySelectorAsync("button");
        var promise = Catch(() => handle.ClickAsync(new() { Force = true }));
        await handle.EvaluateAsync("button => button.className = 'animated'");
        var error = await promise;
        Assert.That(await Page.EvaluateAsync<object>("window.clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("Element is outside of the viewport"));
    }

    [UpstreamTest("CL-064 should dispatch microtasks in order")]
    public async Task ShouldDispatchMicrotasksInOrder()
    {
        await Page.SetContentAsync(@"
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
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<int>("() => window['result']"), Is.EqualTo(1));
    }

    [UpstreamTest("CL-065 should click the button when window.innerWidth is corrupted")]
    public async Task ShouldClickTheButtonWhenWindowInnerWidthIsCorrupted()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync("() => Object.defineProperty(window, 'innerWidth', { value: 0 })");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("CL-066 should click zero-sized input by label")]
    public async Task ShouldClickZeroSizedInputByLabel()
    {
        await Page.SetContentAsync(@"
    <label>
      Click me
      <input onclick=""window.__clicked=true"" style=""width:0;height:0;padding:0;margin:0;border:0;"">
    </label>
  ");
        await Page.ClickAsync("text=Click me");
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-070 should climb dom for inner label with pointer-events:none")]
    public async Task ShouldClimbDomForInnerLabelWithPointerEventsNone()
    {
        await Page.SetContentAsync("<button onclick=\"window.__CLICKED=true;\"><label style=\"pointer-events:none\">Click target</label></button>");
        await Page.ClickAsync("text=Click target");
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-071 should climb up to [role=button]")]
    public async Task ShouldClimbUpToRoleButton()
    {
        await Page.SetContentAsync("<div role=button onclick=\"window.__CLICKED=true;\"><div style=\"pointer-events:none\"><span><div>Click target</div></span></div>");
        await Page.ClickAsync("text=Click target");
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-072 should climb up to a anchor")]
    public async Task ShouldClimbUpToAAnchor()
    {
        // For Firefox its not allowed to return anything: https://bugzilla.mozilla.org/show_bug.cgi?id=1392046
        // Note the intermediate div - it is necessary, otherwise <a><non-clickable/></a> is not recognized as a clickable link.
        await Page.SetContentAsync("<a href=\"#\" onclick=\"window.__CLICKED=true\" id=\"outer\"><div id=\"intermediate\"><div id=\"inner\" style=\"pointer-events: none\">Inner</div></div></a>");
        await Page.ClickAsync("#inner");
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-073 should climb up to a [role=link]")]
    public async Task ShouldClimbUpToARoleLink()
    {
        await Page.SetContentAsync("<div role=link onclick=\"window.__CLICKED=true\" id=\"outer\"><div id=\"inner\" style=\"pointer-events: none\">Inner</div></div>");
        await Page.ClickAsync("#inner");
        Assert.That(await Page.EvaluateAsync<bool>("__CLICKED"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-074 should click in an iframe with border")]
    public async Task ShouldClickInAnIframeWithBorder()
    {
        await Page.SetContentAsync(@"
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
        var locator = Page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-075 should click in an iframe with border 2")]
    public async Task ShouldClickInAnIframeWithBorder2()
    {
        await Page.SetContentAsync(@"
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
        var locator = Page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-076 should not retain removed iframe after clicking inside it")]
    public async Task ShouldNotRetainRemovedIframeAfterClickingInsideIt()
    {
        Info.Skip(IsAndroid, "requesting gc is not enough");

        await Page.SetContentAsync("<iframe srcdoc=\"<button>Click</button>\"></iframe>");
        var button = Page.FrameLocator("iframe").GetByRole(AriaRole.Button);
        await button.WaitForAsync();
        await Page.EvaluateAsync(@"() => {
    window.iframeRef = new WeakRef(document.querySelector('iframe'));
  }");
        await button.ClickAsync();
        await Page.EvaluateAsync("() => document.querySelector('iframe').remove()");
        // Move the mouse away to release Chromium's own last-hovered-node retention.
        await Page.Mouse.MoveAsync(500, 500);
        await Page.RequestGCAsync();
        var retained = await Page.EvaluateAsync<bool>("() => Boolean(window.iframeRef.deref())");
        Assert.That(retained, Is.EqualTo(false));
    }

    [UpstreamTest("CL-077 should click in a transformed iframe")]
    public async Task ShouldClickInATransformedIframe()
    {
        await Page.SetContentAsync(@"
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
        var locator = Page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-078 should click a button that is overlaid by a permission popup")]
    public async Task ShouldClickAButtonThatIsOverlaidByAPermissionPopup()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
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
        await Page.EvaluateAsync(@"() => {
    navigator.geolocation.getCurrentPosition(position => { });
  }");
        // If popup blocks the click, then some of the `page.click` calls below will hang.
        for (var i = 0; i < 30; ++i)
        {
            await Page.ClickAsync($"text={i}");
        }
    }

    [UpstreamTest("CL-079 should click in a transformed iframe with force")]
    public async Task ShouldClickInATransformedIframeWithForce()
    {
        await Page.SetContentAsync(@"
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
        var locator = Page.FrameLocator("iframe").Locator("div");
        await locator.ClickAsync(new() { Force = true });
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-080 should click in a nested transformed iframe")]
    public async Task ShouldClickInANestedTransformedIframe()
    {
        await Page.SetContentAsync(@"
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
        var locator = Page.FrameLocator("iframe").FrameLocator("iframe").Locator("div");
        await locator.EvaluateAsync(@"div => {
    div.addEventListener('click', () => window.top['_clicked'] = true);
  }");
        await locator.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("CL-081 ensure events are dispatched in the individual tasks")]
    public async Task EnsureEventsAreDispatchedInTheIndividualTasks()
    {
        await Page.SetContentAsync(@"
    <div id=""outer"" style=""background: #d4d4d4; width: 60px; height: 60px;"">
      <div id=""inner"" style=""background: #adadad; width: 46px; height: 46px;""></div>
    </div>
  ");

        await Page.EvaluateAsync(@"() => {
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
        Page.Console += (_, msg) => messages.Add(msg.Text);

        // Click on the inner div element
        await Page.Locator("#inner").ClickAsync();

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
    public async Task ShouldClickIfOpenedSelectCoversTheButton()
    {
        await Page.SetContentAsync(@"
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
        await Page.ClickAsync("select");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<int>("window.__CLICKED"), Is.EqualTo(42));
    }

    [UpstreamTest("CL-083 should fire contextmenu event on right click in correct order")]
    public async Task ShouldFireContextmenuEventOnRightClickInCorrectOrder()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <button id=""target"">Click me</button>
  ");
        await Page.EvaluateAsync(@"() => {
    const logEvent = e => console.log(e.type);
    document.addEventListener('mousedown', logEvent);
    document.addEventListener('mouseup', logEvent);
    document.addEventListener('contextmenu', logEvent);
  }");
        var entries = new List<string>();
        Page.Console += (_, message) => entries.Add(message.Text);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync(new() { Button = MouseButton.Right });
        if (BrowserName == "chromium" && IsWindows)
        {
            await Expect(() => Assert.That(entries, Is.EqualTo(new[] { "mousedown", "mouseup", "contextmenu" }))).ToPassAsync();
        }
        else
        {
            await Expect(() => Assert.That(entries, Is.EqualTo(new[] { "mousedown", "contextmenu", "mouseup" }))).ToPassAsync();
        }
    }

    [UpstreamTest("CL-084 should click after a right click")]
    public async Task ShouldClickAfterARightClick()
    {
        await Page.SetContentAsync(@"
    <button>Click me</button>
    <script>
      const button = document.querySelector('button');
      button.addEventListener('click', () => button.textContent = 'Clicked!');
    </script>
  ");
        await Page.GetByRole(AriaRole.Button).ClickAsync(new() { Button = MouseButton.Right });
        await Page.GetByRole(AriaRole.Button).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button)).ToHaveTextAsync("Clicked!");
    }

    [UpstreamTest("CL-085 should set PointerEvent.pressure on pointerdown")]
    public async Task ShouldSetPointerEventPressureOnPointerdown()
    {
        await Page.SetContentAsync(@"
    <button id=""target"">Click me</button>
    <script>
      window['pressures'] = [];
      document.addEventListener('pointerdown', e => window['pressures'].push(['pointerdown', e.pressure]));
      document.addEventListener('pointerup', e => window['pressures'].push(['pointerup', e.pressure]));
    </script>
  ");
        await Page.ClickAsync("button");
        ExpectToEqual(await Page.EvaluateAsync<object>("() => window['pressures']"), ArrayContaining(new object[]
        {
            new object[] { "pointerdown", 0.5 },
            new object[] { "pointerup", 0 },
        }));
    }

    [UpstreamTest("CL-086 should set PointerEvent.pressure on pointermove")]
    public async Task ShouldSetPointerEventPressureOnPointermove()
    {
        Info.Fixme(IsAndroid, "Android coordinates seem to have rounding issues.");
        await Page.SetContentAsync(@"
    <body style=""margin: 0; padding: 0;"">
      <div id=""target"" style=""width: 500px; height: 500px; background-color: red;""></div>
      <script>
        window['pressures'] = [];
        document.addEventListener('pointermove', e => window['pressures'].push([e.pressure, e.clientX, e.clientY]));
      </script>
    </body>
  ");
        await Page.ClickAsync("div#target");
        await Page.Mouse.MoveAsync(10, 10);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(250, 250);
        await Page.Mouse.UpAsync();
        await Page.Mouse.MoveAsync(50, 50);
        ExpectToEqual(await Page.EvaluateAsync<object>("() => window['pressures']"), ArrayContaining(new object[]
        {
            new object[] { 0, 250, 250 },
            new object[] { 0, 10, 10 },
            new object[] { 0.5, 250, 250 },
            new object[] { 0, 50, 50 },
        }));
    }

    [UpstreamTest("CL-087 should click into shadow root with slotted div")]
    public async Task ShouldClickIntoShadowRootWithSlottedDiv()
    {
        await Page.SetContentAsync(@"
    <my-button>
      <template shadowrootmode=""open"">
        <button><slot></slot></button>
      </template>
      <div>Foo</div>
    </my-button>
  ");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Foo" }).ClickAsync();
    }

    [UpstreamTest("CL-088 should click shadow root button")]
    public async Task ShouldClickShadowRootButton()
    {
        await Page.SetContentAsync(@"
    <my-button>
      <template shadowrootmode=""open"">
        <button><slot></slot></button>
      </template>
      <div>Foo</div>
    </my-button>
  ");

        await Page.Locator("my-button").ClickAsync();
    }

    [UpstreamTest("CL-089 should click with tweened mouse movement")]
    public async Task ShouldClickWithTweenedMouseMovement()
    {
        Info.Skip(IsAndroid, "Bad rounding");
        Info.Skip(!Headless, "System cursor tends to interfere with this test");

        await Page.SetContentAsync(@"
    <body style=""margin: 0; padding: 0; height: 500px; width: 500px;"">
      <div style=""position: relative; top: 280px; left: 150px; width: 100px; height: 40px"">Click me</div>
    </body>
  ");

        // The test becomes flaky on WebKit without next line.
        if (BrowserName == "webkit")
        {
            await Page.EvaluateAsync("() => new Promise(requestAnimationFrame)");
        }
        await Page.Mouse.MoveAsync(100, 100);
        await Page.EvaluateAsync(@"() => {
    window['result'] = [];
    document.addEventListener('mousemove', event => {
      window['result'].push([event.clientX, event.clientY]);
    });
  }");
        // Centerpoint at 150 + 100/2, 280 + 40/2 = 200, 300
        await Page.Locator("div").ClickAsync(new() { Steps = 5 });
        ExpectToEqual(await Page.EvaluateAsync<object>("result"), new[]
        {
            new[] { 120, 140 },
            new[] { 140, 180 },
            new[] { 160, 220 },
            new[] { 180, 260 },
            new[] { 200, 300 },
        });
    }
}
