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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-viewport.spec.ts.</summary>
public class BrowsercontextViewportTests : UpstreamBrowserTest
{
    [UpstreamTest("VP-001 should get the proper default viewport size")]
    public async Task ShouldGetTheProperDefaultViewportSize()
    {
        var page = await PageFixture();
        await VerifyViewport(page, 1280, 720);
    }

    [UpstreamTest("VP-002 should set the proper viewport size")]
    public async Task ShouldSetTheProperViewportSize()
    {
        var page = await PageFixture();
        await VerifyViewport(page, 1280, 720);
        await page.SetViewportSizeAsync(345, 456);
        await VerifyViewport(page, 345, 456);
    }

    [UpstreamTest("VP-003 should return correct outerWidth and outerHeight")]
    public async Task ShouldReturnCorrectOuterWidthAndOuterHeight()
    {
        var page = await PageFixture();
        await page.SetViewportSizeAsync(410, 420);
        var size = await page.EvaluateAsync<JsonElement>(@"() => {
    return {
      innerWidth: window.innerWidth,
      innerHeight: window.innerHeight,
      outerWidth: window.outerWidth,
      outerHeight: window.outerHeight,
    };
  }");
        Assert.That(size.GetProperty("innerWidth").GetInt32(), Is.EqualTo(410));
        Assert.That(size.GetProperty("innerHeight").GetInt32(), Is.EqualTo(420));
        Assert.That(size.GetProperty("outerWidth").GetInt32() >= size.GetProperty("innerWidth").GetInt32(), Is.True);
        Assert.That(size.GetProperty("outerHeight").GetInt32() >= size.GetProperty("innerHeight").GetInt32(), Is.True);
    }

    [UpstreamTest("VP-004 landscape viewport should have width larger than height")]
    public async Task LandscapeViewportShouldHaveWidthLargerThanHeight()
    {
        // .NET: deviceDescriptors of the driver, which the test imports, is playwright.Devices.
        var deviceDescriptors = Playwright.Devices;
        foreach (var device in deviceDescriptors.Keys)
        {
            var configuration = deviceDescriptors[device];
            if (device.Contains("landscape") || device.Contains("Landscape"))
            {
                Assert.That(configuration.ViewportSize.Width, Is.GreaterThan(configuration.ViewportSize.Height));
            }
        }
    }

    [UpstreamTest("VP-005 should emulate device width")]
    public async Task ShouldEmulateDeviceWidth()
    {
        var page = await PageFixture();
        ExpectToEqual(page.ViewportSize, new { width = 1280, height = 720 });
        await page.SetViewportSizeAsync(300, 300);
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.width"), Is.EqualTo(300));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-width: 200px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-width: 400px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-width: 200px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-width: 400px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-width: 600px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-width: 300px)').matches"), Is.EqualTo(true));
        await page.SetViewportSizeAsync(500, 500);
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.width"), Is.EqualTo(500));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-width: 400px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-width: 600px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-width: 400px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-width: 600px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-width: 200px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-width: 500px)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("VP-006 should emulate device height")]
    public async Task ShouldEmulateDeviceHeight()
    {
        var page = await PageFixture();
        ExpectToEqual(page.ViewportSize, new { width = 1280, height = 720 });
        await page.SetViewportSizeAsync(300, 300);
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.height"), Is.EqualTo(300));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-height: 200px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-height: 400px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-height: 200px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-height: 400px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-height: 600px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-height: 300px)').matches"), Is.EqualTo(true));
        await page.SetViewportSizeAsync(500, 500);
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.height"), Is.EqualTo(500));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-height: 400px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(min-device-height: 600px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-height: 400px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(max-device-height: 600px)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-height: 200px)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(device-height: 500px)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("VP-007 should emulate availWidth and availHeight")]
    public async Task ShouldEmulateAvailWidthAndAvailHeight()
    {
        var page = await PageFixture();
        await page.SetViewportSizeAsync(500, 600);
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.availWidth"), Is.EqualTo(500));
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.availHeight"), Is.EqualTo(600));
    }

    [UpstreamTest("VP-008 should not have touch by default")]
    public async Task ShouldNotHaveTouchByDefault()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(false));
    }

    [UpstreamTest("VP-009 should throw on tap if hasTouch is not enabled")]
    public async Task ShouldThrowOnTapIfHasTouchIsNotEnabled()
    {
        var page = await PageFixture();
        await page.SetContentAsync("<div>a</div>");
        {
            var error = await Catch(() => page.TapAsync("div"));
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Message, Does.Contain("The page does not support tap"));
        }
        {
            var error = await Catch(() => page.Locator("div").TapAsync());
            Assert.That(error, Is.Not.Null);
            Assert.That(error.Message, Does.Contain("The page does not support tap"));
        }
    }

    [UpstreamTest("VP-010 should support touch with null viewport")]
    public async Task ShouldSupportTouchWithNullViewport()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { ViewportSize = ViewportSize.NoViewport, HasTouch = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("VP-011 should keep touch emulation after screenshot beyond viewport")]
    [LocalFixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
    public async Task ShouldKeepTouchEmulationAfterScreenshotBeyondViewport()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { ViewportSize = new() { Width = 393, Height = 727 }, HasTouch = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<div id=\"tall\" style=\"height:3000px\">tall</div>");
        var probe = () => page.EvaluateAsync<JsonElement>(@"() => ({
    ontouchstart: 'ontouchstart' in window,
    maxTouchPoints: navigator.maxTouchPoints,
    coarse: matchMedia('(pointer: coarse)').matches,
  })");
        var expected = await probe();
        Assert.That(expected.GetProperty("ontouchstart").GetBoolean(), Is.EqualTo(true));
        await page.ScreenshotAsync(new() { FullPage = true });
        ExpectToEqual(await probe(), expected);
        await page.Locator("#tall").ScreenshotAsync();
        ExpectToEqual(await probe(), expected);
        await page.GotoAsync(Server.EmptyPage);
        ExpectToEqual(await probe(), expected);
        await context.CloseAsync();
    }

    [UpstreamTest("VP-012 should set both screen and viewport options")]
    public async Task ShouldSetBothScreenAndViewportOptions()
    {
        var context = await ContextFactory(new()
        {
            ScreenSize = new() { Width = 1280, Height = 720 },
            ViewportSize = new() { Width = 1000, Height = 600 },
        });
        var page = await context.NewPageAsync();
        var screen = await page.EvaluateAsync<object>("() => ({ w: screen.width, h: screen.height })");
        ExpectToEqual(screen, new { w = 1280, h = 720 });
        var inner = await page.EvaluateAsync<object>("() => ({ w: window.innerWidth, h: window.innerHeight })");
        ExpectToEqual(inner, new { w = 1000, h = 600 });
    }

    [UpstreamTest("VP-013 should report null viewportSize when given null viewport")]
    public async Task ShouldReportNullViewportSizeWhenGivenNullViewport()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions()) { ViewportSize = ViewportSize.NoViewport });
        var page = await context.NewPageAsync();
        Assert.That(page.ViewportSize, Is.Null);
        await context.CloseAsync();
    }

    [UpstreamTest("VP-014 should drag with high dpi")]
    public async Task ShouldDragWithHighDpi()
    {
        Info.Skip(!Headless, "Stray mouse events mess up the test");

        var page = await Browser.NewPageAsync(new(NewPageOptions()) { DeviceScaleFactor = 2 });
        await page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        await page.HoverAsync("#source");
        await page.Mouse.DownAsync();
        await page.HoverAsync("#target");
        await page.Mouse.UpAsync();
        Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        await page.CloseAsync();
    }

    [UpstreamTest("VP-015 WebKit Windows headed should have a minimal viewport")]
    public async Task WebKitWindowsHeadedShouldHaveAMinimalViewport()
    {
        Info.Skip(BrowserName != "webkit" || Headless || !IsWindows, "Not relevant for this browser");

        var error = await ThrowsAsync<PlaywrightException>(() => ContextFactory(new()
        {
            ViewportSize = new() { Width = 100, Height = 100 },
        }));
        Assert.That(error.Message, Does.Contain("WebKit on Windows has a minimal viewport of 250x240."));

        var context = await ContextFactory();
        var page = await context.NewPageAsync();
        var error2 = await ThrowsAsync<PlaywrightException>(() => page.SetViewportSizeAsync(100, 100));
        Assert.That(error2.Message, Does.Contain("WebKit on Windows has a minimal viewport of 250x240."));
        await context.CloseAsync();
    }

    [UpstreamTest("VP-016 should be able to get correct orientation angle on non-mobile devices")]
    public async Task ShouldBeAbleToGetCorrectOrientationAngleOnNonMobileDevices()
    {
        Info.Skip(BrowserName == "webkit", "Desktop webkit dont support orientation API");

        var context = await Browser.NewContextAsync(new(ContextOptions()) { ViewportSize = new() { Width = 300, Height = 400 }, IsMobile = false });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/index.html");
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.orientation.angle"), Is.EqualTo(0));
        await page.SetViewportSizeAsync(400, 300);
        Assert.That(await page.EvaluateAsync<int>("() => window.screen.orientation.angle"), Is.EqualTo(0));
        await context.CloseAsync();
    }

    [UpstreamTest("VP-017 should set window.screen.orientation.type for mobile devices")]
    public async Task ShouldSetWindowScreenOrientationTypeForMobileDevices()
    {
        // .NET: devices of '@playwright/test', which the test imports, is playwright.Devices.
        var devices = Playwright.Devices;
        var context = await ContextFactory(devices["iPhone 14"]);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/index.html");
        Assert.That(await page.EvaluateAsync<string>("() => window.screen.orientation.type"), Is.EqualTo("portrait-primary"));
        await context.CloseAsync();
    }
}
