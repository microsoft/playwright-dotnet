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

using System.Globalization;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/emulation-focus.spec.ts.</summary>
public class EmulationFocusTests
{
    [UpstreamTest("EM-013 should think that it is focused by default")]
    public async Task ShouldThinkThatItIsFocusedByDefault(IPage page)
    {
        Assert.That(await page.EvaluateAsync<bool>("document.hasFocus()"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-014 should think that all pages are focused @smoke")]
    public async Task ShouldThinkThatAllPagesAreFocused(IPage page)
    {
        var page2 = await page.Context.NewPageAsync();
        Assert.That(await page.EvaluateAsync<bool>("document.hasFocus()"), Is.EqualTo(true));
        Assert.That(await page2.EvaluateAsync<bool>("document.hasFocus()"), Is.EqualTo(true));
        await page2.CloseAsync();
    }

    [UpstreamTest("EM-015 should focus popups by default")]
    public async Task ShouldFocusPopupsByDefault(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => { window.open(url); }", server.EmptyPage));
        Assert.That(await popup.EvaluateAsync<bool>("document.hasFocus()"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("document.hasFocus()"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-016 should provide target for keyboard events")]
    public async Task ShouldProvideTargetForKeyboardEvents(IPage page, Server server)
    {
        var page2 = await page.Context.NewPageAsync();
        await PromiseAll(
            page.GotoAsync(server.Prefix + "/input/textarea.html"),
            page2.GotoAsync(server.Prefix + "/input/textarea.html"));
        await PromiseAll(
            page.FocusAsync("input"),
            page2.FocusAsync("input"));
        var text = "first";
        var text2 = "second";
        await PromiseAll(
            page.Keyboard.TypeAsync(text),
            page2.Keyboard.TypeAsync(text2));
        var results = await PromiseAll(new[]
        {
            page.EvaluateAsync<string>("result"),
            page2.EvaluateAsync<string>("result"),
        });
        Assert.That(results, Is.EqualTo(new[] { text, text2 }));
    }

    [UpstreamTest("EM-017 should not affect mouse event target page")]
    public async Task ShouldNotAffectMouseEventTargetPage(IPage page, Server server)
    {
        var page2 = await page.Context.NewPageAsync();
        const string clickCounter = @"function clickCounter() {
    document.onclick = () => window['clickCount']  = (window['clickCount'] || 0) + 1;
  }";
        await PromiseAll(
            page.EvaluateAsync(clickCounter),
            page2.EvaluateAsync(clickCounter),
            page.FocusAsync("body"),
            page2.FocusAsync("body"));
        await PromiseAll(
            page.Mouse.ClickAsync(1, 1),
            page2.Mouse.ClickAsync(1, 1));
        var counters = await PromiseAll(new[]
        {
            page.EvaluateAsync<int>("window.clickCount"),
            page2.EvaluateAsync<int>("window.clickCount"),
        });
        Assert.That(counters, Is.EqualTo(new[] { 1, 1 }));
    }

    [UpstreamTest("EM-018 should change document.activeElement")]
    public async Task ShouldChangeDocumentActiveElement(IPage page, Server server)
    {
        var page2 = await page.Context.NewPageAsync();
        await PromiseAll(
            page.GotoAsync(server.Prefix + "/input/textarea.html"),
            page2.GotoAsync(server.Prefix + "/input/textarea.html"));
        await PromiseAll(
            page.FocusAsync("input"),
            page2.FocusAsync("textarea"));
        var active = await PromiseAll(new[]
        {
            page.EvaluateAsync<string>("document.activeElement.tagName"),
            page2.EvaluateAsync<string>("document.activeElement.tagName"),
        });
        Assert.That(active, Is.EqualTo(new[] { "INPUT", "TEXTAREA" }));
    }

    [UpstreamTest("EM-019 should not affect screenshots")]
    public async Task ShouldNotAffectScreenshots(IPage page, Server server, string browserName, bool headless, bool isWindows, bool isLinux, bool isHeadlessShell, string channel)
    {
        It.Skip(browserName == "webkit" && isWindows && !headless, "WebKit/Windows/headed has a larger minimal viewport. See https://github.com/microsoft/playwright/issues/22616");
        It.Skip(browserName == "webkit" && isLinux && !headless, "WebKit headed has a larger minimal viewport on gtk4.");
        It.Skip(browserName == "firefox" && !headless, "Firefox headed produces a different image");

        var page2 = await page.Context.NewPageAsync();
        await PromiseAll(
            page.SetViewportSizeAsync(500, 500),
            page.GotoAsync(server.Prefix + "/grid.html"),
            page2.SetViewportSizeAsync(50, 50),
            page2.GotoAsync(server.Prefix + "/grid.html"));
        await PromiseAll(
            page.FocusAsync("body"),
            page2.FocusAsync("body"));
        var screenshots = await PromiseAll(new[]
        {
            page.ScreenshotAsync(),
            page2.ScreenshotAsync(),
        });
        ExpectToMatchSnapshot(screenshots[0], "screenshot-sanity.png");
        ExpectToMatchSnapshot(screenshots[1], "grid-cell-0.png");
    }

    [UpstreamTest("EM-020 should change focused iframe")]
    public async Task ShouldChangeFocusedIframe(IPage page, Server server, string browserName, bool headless)
    {
        It.Skip(browserName == "firefox" && !headless, "Headed FF might lose focus");
        await page.GotoAsync(server.EmptyPage);
        var (frame1, frame2) = await PromiseAll(
            AttachFrame(page, "frame1", server.Prefix + "/input/textarea.html"),
            AttachFrame(page, "frame2", server.Prefix + "/input/textarea.html"));
        const string logger = @"function logger() {
    self['_events'] = [];
    const element = document.querySelector('input');
    element.onfocus = element.onblur = e => self['_events'].push(e.type);
  }";
        await PromiseAll(
            frame1.EvaluateAsync(logger),
            frame2.EvaluateAsync(logger));
        var focused = await PromiseAll(new[]
        {
            frame1.EvaluateAsync<bool>("document.hasFocus()"),
            frame2.EvaluateAsync<bool>("document.hasFocus()"),
        });
        Assert.That(focused, Is.EqualTo(new[] { false, false }));
        {
            await frame1.FocusAsync("input");
            var events = await PromiseAll(new[]
            {
                frame1.EvaluateAsync<string[]>("self._events"),
                frame2.EvaluateAsync<string[]>("self._events"),
            });
            ExpectToEqual(events, new[] { new[] { "focus" }, Array.Empty<string>() });
            // .NET: 'focused' is a local of the test already (CS0136).
            var focused2 = await PromiseAll(new[]
            {
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"),
            });
            Assert.That(focused2, Is.EqualTo(new[] { true, false }));
        }
        {
            await frame2.FocusAsync("input");
            var events = await PromiseAll(new[]
            {
                frame1.EvaluateAsync<string[]>("self._events"),
                frame2.EvaluateAsync<string[]>("self._events"),
            });
            ExpectToEqual(events, new[] { new[] { "focus", "blur" }, new[] { "focus" } });
            // .NET: 'focused' is a local of the test already (CS0136).
            var focused2 = await PromiseAll(new[]
            {
                frame1.EvaluateAsync<bool>("document.hasFocus()"),
                frame2.EvaluateAsync<bool>("document.hasFocus()"),
            });
            Assert.That(focused2, Is.EqualTo(new[] { false, true }));
        }
    }

    // @see https://github.com/microsoft/playwright/issues/3476
    [UpstreamTest("EM-021 should focus with more than one page/context")]
    public async Task ShouldFocusWithMoreThanOnePageContext(ContextFactory contextFactory)
    {
        var page1 = await (await contextFactory()).NewPageAsync();
        var page2 = await (await contextFactory()).NewPageAsync();
        await page1.SetContentAsync("<button id=\"foo\" onfocus=\"window.gotFocus=true\">foo</button>");
        await page2.SetContentAsync("<button id=\"foo\" onfocus=\"window.gotFocus=true\">foo</button>");
        await page1.FocusAsync("#foo");
        await page2.FocusAsync("#foo");
        Assert.That(await page1.EvaluateAsync<bool>("() => !!window['gotFocus']"), Is.EqualTo(true));
        Assert.That(await page2.EvaluateAsync<bool>("() => !!window['gotFocus']"), Is.EqualTo(true));
    }

    [UpstreamTest("EM-022 should not fire blur events when interacting with more than one page/context")]
    public async Task ShouldNotFireBlurEventsWhenInteractingWithMoreThanOnePageContext(ContextFactory contextFactory, string browserName)
    {
        var page1 = await (await contextFactory()).NewPageAsync();
        var page2 = await (await contextFactory()).NewPageAsync();
        await page1.SetContentAsync("<button id=\"foo\" onblur=\"window.gotBlur=true\">foo</button>");
        await page2.SetContentAsync("<button id=\"foo\" onblur=\"window.gotBlur=true\">foo</button>");
        await page1.ClickAsync("#foo");
        await page2.ClickAsync("#foo");
        Assert.That(await page1.EvaluateAsync<bool>("() => !!window['gotBlur']"), Is.EqualTo(false));
        Assert.That(await page2.EvaluateAsync<bool>("() => !!window['gotBlur']"), Is.EqualTo(false));
    }

    [UpstreamTest("EM-023 should not blur iframe when interacting with another page/context")]
    public async Task ShouldNotBlurIframeWhenInteractingWithAnotherPageContext(ContextFactory contextFactory, string browserName)
    {
        It.Fixme(browserName == "firefox", "https://github.com/microsoft/playwright/issues/43280");

        var page1 = await (await contextFactory()).NewPageAsync();
        var page2 = await (await contextFactory()).NewPageAsync();
        var srcdoc = "<button id=foo>foo</button><script>window.addEventListener('blur', () => window.gotBlur = true)</script>";
        await page1.SetContentAsync($"<iframe srcdoc=\"{srcdoc}\"></iframe><iframe srcdoc=\"{srcdoc}\"></iframe>");
        var (frame1, frame2) = (page1.Frames[1], page1.Frames[2]);
        await frame1.ClickAsync("#foo");
        await page2.SetContentAsync("<p>other page</p>");
        await page2.Mouse.ClickAsync(10, 10);
        Assert.That(await frame1.EvaluateAsync<bool>("() => !!window['gotBlur']"), Is.EqualTo(false));
        Assert.That(await frame1.EvaluateAsync<bool>("() => document.hasFocus()"), Is.EqualTo(true));
        Assert.That(await frame2.EvaluateAsync<bool>("() => document.hasFocus()"), Is.EqualTo(false));
    }

    [UpstreamTest("EM-024 should trigger hover state concurrently")]
    public async Task ShouldTriggerHoverStateConcurrently(IBrowserType browserType, bool headless, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions, BrowserNewPageOptions newPageOptions)
    {
        It.Skip(!headless, "headed messes up with hover");

        var browser1 = await browserType.LaunchAsync(launchOptions);
        var context1 = await browser1.NewContextAsync(contextOptions);
        var page1 = await context1.NewPageAsync();
        var page2 = await context1.NewPageAsync();
        var browser2 = await browserType.LaunchAsync(launchOptions);
        var page3 = await browser2.NewPageAsync(newPageOptions);

        foreach (var page in new[] { page1, page2, page3 })
        {
            await page.SetContentAsync(@"
      <style>
        button { display: none; }
        div:hover button { display: inline };
      </style>
      <div><span>hover me</span><button onclick=""window.clicked=1+(window.clicked || 0)"">click me</button></div>
    ");
        }

        foreach (var page in new[] { page1, page2, page3 })
        {
            await page.HoverAsync("span");
        }
        foreach (var page in new[] { page1, page2, page3 })
        {
            await page.ClickAsync("button");
        }
        foreach (var page in new[] { page1, page2, page3 })
        {
            Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
        }
        foreach (var page in new[] { page1, page2, page3 })
        {
            await page.ClickAsync("button");
        }
        foreach (var page in new[] { page1, page2, page3 })
        {
            Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(2));
        }
    }

    [UpstreamTest("EM-028 should keep mouse drag when interacting with another page/context")]
    public async Task ShouldKeepMouseDragWhenInteractingWithAnotherPageContext(ContextFactory contextFactory)
    {
        var page1 = await (await contextFactory()).NewPageAsync();
        var page2 = await (await contextFactory()).NewPageAsync();
        foreach (var page in new[] { page1, page2 })
        {
            await page.SetContentAsync(@"
      <body style=""margin: 0"">
        <input type=""range"" style=""margin: 0; width: 200px; height: 20px"" onchange=""window.changed = true"">
      </body>
    ");
        }
        await page1.Mouse.MoveAsync(100, 10);
        await page1.Mouse.DownAsync();
        await page1.Mouse.MoveAsync(150, 10);
        await page2.Mouse.MoveAsync(50, 50);
        await page1.Mouse.MoveAsync(180, 10);
        await page1.Mouse.UpAsync();
        Assert.That(await page1.EvaluateAsync<bool>("() => !!window['changed']"), Is.EqualTo(true));
        Assert.That(double.Parse(await page1.Locator("input").InputValueAsync(), CultureInfo.InvariantCulture), Is.GreaterThan(80));
    }
}
