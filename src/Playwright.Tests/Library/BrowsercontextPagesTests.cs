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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-pages.spec.ts.</summary>
public class BrowsercontextPagesTests
{
    [UpstreamTest("BC-051 should not be visible in context.pages")]
    public async Task ShouldNotBeVisibleInContextPages(ContextFactory contextFactory)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        Assert.That(context.Pages, Does.Contain(page));
        await page.CloseAsync();
        Assert.That(context.Pages, Does.Not.Contain(page));
    }

    [UpstreamTest("BC-052 page.context should return the correct instance")]
    public async Task PageContextShouldReturnTheCorrectInstance(ContextFactory contextFactory)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        Assert.That(page.Context, Is.SameAs(context));
    }

    [UpstreamTest("BC-053 frame.focus should work multiple times")]
    public async Task FrameFocusShouldWorkMultipleTimes(ContextFactory contextFactory)
    {
        var context = await contextFactory();
        var page1 = await context.NewPageAsync();
        var page2 = await context.NewPageAsync();
        foreach (var page in new[] { page1, page2 })
        {
            await page.SetContentAsync("<button id=\"foo\" onfocus=\"window.gotFocus=true\"></button>");
            await page.FocusAsync("#foo");
            Assert.That(await page.EvaluateAsync<bool>("() => !!window['gotFocus']"), Is.EqualTo(true));
        }
    }

    [UpstreamTest("BC-054 should click with disabled javascript")]
    public async Task ShouldClickWithDisabledJavascript(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/wrappedlink.html");
        await PromiseAll(
            page.ClickAsync("a"),
            page.WaitForNavigationAsync());
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/wrappedlink.html#clicked"));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-055 should not hang with touch-enabled viewports")]
    public async Task ShouldNotHangWithTouchEnabledViewports(IBrowser browser, IPlaywright playwright, BrowserNewContextOptions contextOptions)
    {
        // @see https://github.com/GoogleChrome/puppeteer/issues/161
        // .NET: const { viewport, hasTouch } = playwright.devices['iPhone 6'];
        var (viewport, hasTouch) = (playwright.Devices["iPhone 6"].ViewportSize, playwright.Devices["iPhone 6"].HasTouch);
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = viewport, HasTouch = hasTouch });
        var page = await context.NewPageAsync();
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(100, 10);
        await page.Mouse.UpAsync();
        await context.CloseAsync();
    }

    [UpstreamTest("BC-056 should click the button with deviceScaleFactor set")]
    public async Task ShouldClickTheButtonWithDeviceScaleFactorSet(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 400, Height = 400 }, DeviceScaleFactor = 5 });
        var page = await context.NewPageAsync();
        Assert.That(await page.EvaluateAsync<int>("() => window.devicePixelRatio"), Is.EqualTo(5));
        await page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
        await AttachFrame(page, "button-test", server.Prefix + "/input/button.html");
        var frame = page.Frames[1];
        var button = await frame.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.That(await frame.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-057 should click the button with offset with page scale")]
    public async Task ShouldClickTheButtonWithOffsetWithPageScale(IBrowser browser, Server server, string browserName, bool isBidi, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(browserName == "firefox" && !isBidi);

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 400, Height = 400 }, IsMobile = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", @"button => {
    button.style.borderWidth = '8px';
    document.body.style.margin = '0';
  }");
        await page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        var expectCloseTo = (double expected, double actual) =>
        {
            if (Math.Abs(expected - actual) > 2)
            {
                throw new Exception($"Expected: {expected}, received: {actual}");
            }
        };
        // Expect 20;10 + 8px of border in each direction. Allow some delta as different
        // browsers round up or down differently during css -> dip -> css conversion.
        expectCloseTo(28, await page.EvaluateAsync<double>("pageX"));
        expectCloseTo(18, await page.EvaluateAsync<double>("pageY"));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-058 should return bounding box with page scale")]
    public async Task ShouldReturnBoundingBoxWithPageScale(IBrowser browser, Server server, string browserName, bool isBidi, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(browserName == "firefox" && !isBidi);

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 400, Height = 400 }, IsMobile = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/input/button.html");
        var button = await page.QuerySelectorAsync("button");
        await button.EvaluateAsync(@"button => {
    document.body.style.margin = '0';
    button.style.borderWidth = '0';
    button.style.width = '200px';
    button.style.height = '20px';
    button.style.marginLeft = '17px';
    button.style.marginTop = '23px';
  }");
        var box = await button.BoundingBoxAsync();
        Assert.That(Math.Round(box.X * 100, MidpointRounding.AwayFromZero), Is.EqualTo(17 * 100));
        Assert.That(Math.Round(box.Y * 100, MidpointRounding.AwayFromZero), Is.EqualTo(23 * 100));
        Assert.That(Math.Round(box.Width * 100, MidpointRounding.AwayFromZero), Is.EqualTo(200 * 100));
        Assert.That(Math.Round(box.Height * 100, MidpointRounding.AwayFromZero), Is.EqualTo(20 * 100));
        await context.CloseAsync();
    }

    [UpstreamTest("BC-060 should close page while a reload is committing")]
    public async Task ShouldClosePageWhileAReloadIsCommitting(IBrowserContext context, string browserName)
    {
        Info.Fixme(browserName == "chromium", "Chromium loses the close when the reload commits into a new RenderFrameHost; fix is not rolled yet");
        // The evaluate never settles on its own, so it only rejects once the reload
        // commits. Closing right after that races the commit. Repeat a few times,
        // since whether the close or the commit wins is timing-dependent.
        for (var i = 0; i < 10; i++)
        {
            var page = await context.NewPageAsync();
            var error = await ThrowsAsync<PlaywrightException>(() => page.EvaluateAsync(@"() => {
      location.reload();
      return new Promise(() => {});
    }"));
            Assert.That(error.Message, Does.Match("navigation"));
            await page.CloseAsync();
        }
    }

    [UpstreamTest("BC-061 should keep selection in multiple pages")]
    public async Task ShouldKeepSelectionInMultiplePages(IBrowserContext context)
    {
        var page1 = await context.NewPageAsync();
        var page2 = await context.NewPageAsync();
        foreach (var page in new[] { page1, page2 })
        {
            await page.SetContentAsync("<div id=\"container\">lorem ipsum dolor sit amet</div>");
            await page.EvaluateAsync(@"() => {
      const element = document.getElementById('container');
      const textNode = element.firstChild;

      const range = document.createRange();
      range.setStart(textNode, 6);
      range.setEnd(textNode, 11);

      const selection = document.getSelection();
      selection?.removeAllRanges();
      selection?.addRange(range);
    }");
        }
        // In WebKit, selection is updated via IPC, give it enough time to take effect.
        await Task.Delay(1_000);
        foreach (var page in new[] { page1, page2 })
        {
            var range = await page.EvaluateAsync<object>(@"() => {
      const selection = document.getSelection();
      const range = selection?.getRangeAt(0);
      return {
        rangeCount: selection?.rangeCount,
        startOffset: range?.startOffset,
        endOffset: range?.endOffset,
      };
    }");
            ExpectToEqual(range, new { rangeCount = 1, startOffset = 6, endOffset = 11 });
        }
    }
}
