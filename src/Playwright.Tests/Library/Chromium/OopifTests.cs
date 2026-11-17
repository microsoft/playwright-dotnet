/**
 * Copyright 2017 Google Inc. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library.Chromium;

/// <summary>Port of tests/library/chromium/oopif.spec.ts.</summary>
public class OopifTests
{
    // .NET: upstream runs the files of library/chromium/ in the Chromium project only.
    [BeforeEach]
    public static void Skip(string browserName) => Info.Skip(browserName != "chromium", "chromium only");

    [Fixture]
    public static BrowserTypeLaunchOptions LaunchOptions(BrowserTypeLaunchOptions launchOptions) => new(launchOptions) { Args = ["--site-per-process"] };

    [UpstreamTest("OO-001 should report oopif frames")]
    public async Task ShouldReportOopifFrames(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
    }

    [UpstreamTest("OO-002 should handle oopif detach")]
    public async Task ShouldHandleOopifDetach(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        var frame = page.Frames[1];
        Assert.That(await frame.EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        var (detachedFrame, _) = await PromiseAll(
            Once<IFrame>(h => page.FrameDetached += h, h => page.FrameDetached -= h),
            page.EvaluateAsync("() => document.querySelector('iframe').remove()"));
        Assert.That(detachedFrame, Is.SameAs(frame));
    }

    [UpstreamTest("OO-003 should remove workers of a detached oopif")]
    public async Task ShouldRemoveWorkersOfADetachedOopif(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (worker, _) = await PromiseAll(
            page.WaitForWorkerAsync(),
            AttachFrame(page, "frame1", server.CrossProcessPrefix + "/worker/worker.html"));
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Workers.Count, Is.EqualTo(1));
        await PromiseAll(
            Once<IWorker>(h => worker.Close += h, h => worker.Close -= h),
            page.GotoAsync(server.Prefix + "/title.html"));
        Assert.That(page.Workers.Count, Is.EqualTo(0));
    }

    [UpstreamTest("OO-004 should not hang in unrouteAll when oopif worker is gone")]
    public async Task ShouldNotHangInUnrouteAllWhenOopifWorkerIsGone(IPage page, IBrowserContext context, IBrowser browser, Server server)
    {
        await context.RouteAsync("**/*", route => route.ContinueAsync());
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.WaitForWorkerAsync(),
            AttachFrame(page, "frame1", server.CrossProcessPrefix + "/worker/worker.html"));
        await AssertOOPIFCount(browser, 1);
        await page.GotoAsync(server.CrossProcessPrefix + "/title.html");
        await context.UnrouteAllAsync();
    }

    [UpstreamTest("OO-005 should handle remote -> local -> remote transitions")]
    public async Task ShouldHandleRemoteLocalRemoteTransitions(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        Assert.That(await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        await PromiseAll(
            page.Frames[1].WaitForNavigationAsync(),
            page.EvaluateAsync("goLocal()"));
        Assert.That(await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.Prefix + "/grid.html"));
        await AssertOOPIFCount(browser, 0);
        await PromiseAll(
            page.Frames[1].WaitForNavigationAsync(),
            page.EvaluateAsync("goRemote()"));
        Assert.That(await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        await AssertOOPIFCount(browser, 1);
    }

    [UpstreamTest("OO-006 should get the proper viewport")]
    [Ignore("fixme")]
    public async Task ShouldGetTheProperViewport(IPage page, IBrowser browser, Server server)
    {
        ExpectToEqual(page.ViewportSize, new { width = 1280, height = 720 });
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        var oopif = page.Frames[1];
        Assert.That(await oopif.EvaluateAsync<int>("() => screen.width"), Is.EqualTo(1280));
        Assert.That(await oopif.EvaluateAsync<int>("() => screen.height"), Is.EqualTo(720));
        Assert.That(await oopif.EvaluateAsync<bool>("() => matchMedia('(device-width: 1280px)').matches"), Is.EqualTo(true));
        Assert.That(await oopif.EvaluateAsync<bool>("() => matchMedia('(device-height: 720px)').matches"), Is.EqualTo(true));
        Assert.That(await oopif.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(false));
        await page.SetViewportSizeAsync(123, 456);
        Assert.That(await oopif.EvaluateAsync<int>("() => screen.width"), Is.EqualTo(123));
        Assert.That(await oopif.EvaluateAsync<int>("() => screen.height"), Is.EqualTo(456));
        Assert.That(await oopif.EvaluateAsync<bool>("() => matchMedia('(device-width: 123px)').matches"), Is.EqualTo(true));
        Assert.That(await oopif.EvaluateAsync<bool>("() => matchMedia('(device-height: 456px)').matches"), Is.EqualTo(true));
        Assert.That(await oopif.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(false));
    }

    [UpstreamTest("OO-007 should expose function")]
    public async Task ShouldExposeFunction(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        var oopif = page.Frames[1];
        await page.ExposeFunctionAsync("mul", (int a, int b) => a * b);
        var result = await oopif.EvaluateAsync<int>(@"async function() {
    return await window['mul'](9, 4);
  }");
        Assert.That(result, Is.EqualTo(36));
    }

    [UpstreamTest("OO-008 should emulate media")]
    public async Task ShouldEmulateMedia(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        var oopif = page.Frames[1];
        Assert.That(await oopif.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(false));
        await page.EmulateMediaAsync(new() { ColorScheme = ColorScheme.Dark });
        Assert.That(await oopif.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("OO-009 should emulate offline")]
    public async Task ShouldEmulateOffline(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        var oopif = page.Frames[1];
        Assert.That(await oopif.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(true));
        await page.Context.SetOfflineAsync(true);
        Assert.That(await oopif.EvaluateAsync<bool>("() => navigator.onLine"), Is.EqualTo(false));
    }

    [UpstreamTest("OO-010 should support context options")]
    public async Task ShouldSupportContextOptions(IBrowser browser, Server server, IPlaywright playwright, BrowserNewContextOptions contextOptions)
    {
        var iPhone = playwright.Devices["iPhone 6"];
        var context = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone) { TimezoneId = "America/Jamaica", Locale = "fr-CH", UserAgent = "UA" }, contextOptions));
        var page = await context.NewPageAsync();

        var (request, _) = await PromiseAll(
            server.WaitForRequest("/grid.html"),
            page.GotoAsync(server.Prefix + "/dynamic-oopif.html"));
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        var oopif = page.Frames[1];

        Assert.That(await oopif.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(true));
        Assert.That(await oopif.EvaluateAsync<string>("() => new Date(1479579154987).toString()"), Is.EqualTo("Sat Nov 19 2016 13:12:34 GMT-0500 (heure normale de l’Est nord-américain)"));
        Assert.That(await oopif.EvaluateAsync<string>("() => navigator.language"), Is.EqualTo("fr-CH"));
        Assert.That(await oopif.EvaluateAsync<string>("() => navigator.userAgent"), Is.EqualTo("UA"));
        Assert.That(request.Headers["user-agent"], Is.EqualTo("UA"));

        await context.CloseAsync();
    }

    [UpstreamTest("OO-011 should respect route")]
    public async Task ShouldRespectRoute(IPage page, IBrowser browser, Server server)
    {
        var intercepted = false;
        await page.RouteAsync("**/digits/0.png", route =>
        {
            intercepted = true;
            IgnoreErrors(route.ContinueAsync());
        });
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        Assert.That(intercepted, Is.EqualTo(true));
    }

    [UpstreamTest("OO-012 should take screenshot")]
    public async Task ShouldTakeScreenshot(IPage page, IBrowser browser, Server server)
    {
        await page.SetViewportSizeAsync(500, 500);
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        await Rafraf(page.Frames[1]);
        ExpectToMatchSnapshot(await page.ScreenshotAsync(), "screenshot-oopif.png");
    }

    [UpstreamTest("OO-013 should load oopif iframes with subresources and route")]
    public async Task ShouldLoadOopifIframesWithSubresourcesAndRoute(IPage page, IBrowser browser, Server server)
    {
        await page.RouteAsync("**/*", route => route.ContinueAsync());
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
    }

    [UpstreamTest("OO-014 should report main requests")]
    public async Task ShouldReportMainRequests(IPage page, IBrowser browser, Server server)
    {
        var requestFrames = new List<IFrame>();
        page.Request += (_, r) => requestFrames.Add(r.Frame);
        var finishedFrames = new List<IFrame>();
        page.RequestFinished += (_, r) => finishedFrames.Add(r.Frame);

        await page.GotoAsync(server.Prefix + "/empty.html");
        var main = page.MainFrame;

        await main.EvaluateAsync(@"url => {
    const iframe = document.createElement('iframe');
    iframe.src = url;
    document.body.appendChild(iframe);
    return new Promise(f => iframe.onload = f);
  }", server.CrossProcessPrefix + "/empty.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        var child = main.ChildFrames[0];
        await child.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await child.EvaluateAsync(@"url => {
    const iframe = document.createElement('iframe');
    iframe.src = url;
    document.body.appendChild(iframe);
    return new Promise(f => iframe.onload = f);
  }", server.Prefix + "/empty.html");
        Assert.That(page.Frames.Count, Is.EqualTo(3));
        var grandChild = child.ChildFrames[0];
        await grandChild.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        await AssertOOPIFCount(browser, 2);
        Assert.That(requestFrames[0], Is.SameAs(main));
        Assert.That(finishedFrames[0], Is.SameAs(main));
        Assert.That(requestFrames[1], Is.SameAs(child));
        Assert.That(finishedFrames[1], Is.SameAs(child));
        Assert.That(requestFrames[2], Is.SameAs(grandChild));
        Assert.That(finishedFrames[2], Is.SameAs(grandChild));
    }

    [UpstreamTest("OO-015 should support exposeFunction")]
    public async Task ShouldSupportExposeFunction(IPage page, IBrowser browser, Server server)
    {
        await page.Context.ExposeFunctionAsync("dec", (int a) => a - 1);
        await page.ExposeFunctionAsync("inc", (int a) => a + 1);
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(await page.Frames[0].EvaluateAsync<int>("() => window['inc'](3)"), Is.EqualTo(4));
        Assert.That(await page.Frames[1].EvaluateAsync<int>("() => window['inc'](4)"), Is.EqualTo(5));
        Assert.That(await page.Frames[0].EvaluateAsync<int>("() => window['dec'](3)"), Is.EqualTo(2));
        Assert.That(await page.Frames[1].EvaluateAsync<int>("() => window['dec'](4)"), Is.EqualTo(3));
    }

    [UpstreamTest("OO-016 should support addInitScript")]
    public async Task ShouldSupportAddInitScript(IPage page, IBrowser browser, Server server)
    {
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.Context.AddInitScriptAsync("(() => window['bar'] = 17)()");
        // .NET: AddInitScriptAsync takes the source of a script, not a function: the source calls the function, as the Node.js client does.
        await page.AddInitScriptAsync("(() => window['foo'] = 42)()");
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(await page.Frames[0].EvaluateAsync<int>("() => window['foo']"), Is.EqualTo(42));
        Assert.That(await page.Frames[1].EvaluateAsync<int>("() => window['foo']"), Is.EqualTo(42));
        Assert.That(await page.Frames[0].EvaluateAsync<int>("() => window['bar']"), Is.EqualTo(17));
        Assert.That(await page.Frames[1].EvaluateAsync<int>("() => window['bar']"), Is.EqualTo(17));
    }

    // @see https://github.com/microsoft/playwright/issues/1240
    [UpstreamTest("OO-017 should click a button when it overlays oopif")]
    public async Task ShouldClickAButtonWhenItOverlaysOopif(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/button-overlay-oopif.html");
        await AssertOOPIFCount(browser, 1);
        await page.ClickAsync("button");
        Assert.That(await page.EvaluateAsync<bool>("() => window['BUTTON_CLICKED']"), Is.EqualTo(true));
    }

    [UpstreamTest("OO-018 should report google.com frame with headed")]
    public async Task ShouldReportGoogleComFrameWithHeaded(IBrowserType browserType, Server server, string channel, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        Info.Skip(channel == "chromium-headless-shell", "Headless Shell does not support headed mode");
        // @see https://github.com/GoogleChrome/puppeteer/issues/2548
        // https://google.com is isolated by default in Chromium embedder.
        var browser = await browserType.LaunchAsync(new(launchOptions) { Headless = false });
        var page = await browser.NewPageAsync(newPageOptions);
        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "YO, GOOGLE.COM" }));
        });
        await page.EvaluateAsync(@"() => {
    const frame = document.createElement('iframe');
    frame.setAttribute('src', 'https://google.com/');
    document.body.appendChild(frame);
    return new Promise(x => frame.onload = x);
  }");
        await page.WaitForSelectorAsync("iframe[src=\"https://google.com/\"]");
        await AssertOOPIFCount(browser, 1);
        var urls = page.Frames.Select(frame => frame.Url);
        Assert.That(urls, Is.EqualTo(new[]
        {
            server.EmptyPage,
            "https://google.com/",
        }));
        await browser.CloseAsync();
    }

    [UpstreamTest("OO-019 ElementHandle.boundingBox() should work")]
    public async Task ElementHandleBoundingBoxShouldWork(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await page.EvalOnSelectorAsync("iframe", @"iframe => {
    iframe.style.width = '520px';
    iframe.style.height = '520px';
    iframe.style.marginLeft = '42px';
    iframe.style.marginTop = '17px';
  }");
        await page.Frames[1].GotoAsync(page.Frames[1].Url);

        await AssertOOPIFCount(browser, 1);
        var handle1 = await page.Frames[1].QuerySelectorAsync(".box:nth-of-type(13)");
        await Expect(async () => ExpectToEqual(await handle1.BoundingBoxAsync(), new { x = 100 + 42, y = 50 + 17, width = 50, height = 50 })).ToPassAsync();

        await PromiseAll(
            page.Frames[1].WaitForNavigationAsync(),
            page.EvaluateAsync("goLocal()"));
        await AssertOOPIFCount(browser, 0);
        var handle2 = await page.Frames[1].QuerySelectorAsync(".box:nth-of-type(13)");
        await Expect(async () => ExpectToEqual(await handle2.BoundingBoxAsync(), new { x = 100 + 42, y = 50 + 17, width = 50, height = 50 })).ToPassAsync();
    }

    [UpstreamTest("OO-020 should click")]
    public async Task ShouldClick(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await page.EvalOnSelectorAsync("iframe", @"iframe => {
    iframe.style.width = '500px';
    iframe.style.height = '500px';
    iframe.style.marginLeft = '102px';
    iframe.style.marginTop = '117px';
  }");
        await page.Frames[1].GotoAsync(page.Frames[1].Url);

        await AssertOOPIFCount(browser, 1);
        var handle1 = await page.Frames[1].QuerySelectorAsync(".box:nth-of-type(13)");
        await handle1.EvaluateAsync("div => div.addEventListener('click', () => window['_clicked'] = true, false)");
        await handle1.ClickAsync();
        Assert.That(await handle1.EvaluateAsync<bool>("() => window['_clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("OO-021 contentFrame should work")]
    public async Task ContentFrameShouldWork(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        Assert.That(await page.Locator("iframe").ContentFrame.Locator("div").CountAsync(), Is.EqualTo(200));
        var oopif = await page.QuerySelectorAsync("iframe");
        var content = await oopif.ContentFrameAsync();
        Assert.That(await content.Locator("div").CountAsync(), Is.EqualTo(200));
    }

    [UpstreamTest("OO-022 should allow cdp sessions on oopifs")]
    public async Task ShouldAllowCdpSessionsOnOopifs(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));

        var parentCDP = await page.Context.NewCDPSessionAsync(page.Frames[0]);
        var parent = await parentCDP.SendAsync("DOM.getDocument", new() { ["pierce"] = true, ["depth"] = -1 });
        Assert.That(JsonStringify(parent), Does.Not.Contain("./digits/1.png"));

        var oopifCDP = await page.Context.NewCDPSessionAsync(page.Frames[1]);
        var oopif = await oopifCDP.SendAsync("DOM.getDocument", new() { ["pierce"] = true, ["depth"] = -1 });
        Assert.That(JsonStringify(oopif), Does.Contain("./digits/1.png"));
    }

    [UpstreamTest("OO-023 should emit filechooser event for iframe")]
    public async Task ShouldEmitFilechooserEventForIframe(IPage page, Server server, IBrowser browser)
    {
        // Add listener before OOPIF is created.
        var chooserPromise = page.WaitForFileChooserAsync();
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        var frame = page.Frames[1];
        await frame.SetContentAsync("<input type=file>");
        var chooser = await PromiseAll(
            chooserPromise,
            frame.ClickAsync("input"));
        Assert.That(chooser, Is.Not.Null);
    }

    [UpstreamTest("OO-024 should be able to click in iframe")]
    public async Task ShouldBeAbleToClickInIframe(IPage page, Server server, IBrowser browser)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        var frame = page.Frames[1];
        await frame.SetContentAsync("<button onclick=\"console.log('clicked')\">OK</button>");
        var message = await PromiseAll(
            page.WaitForConsoleMessageAsync(),
            frame.ClickAsync("button"));
        Assert.That(message.Text, Is.EqualTo("clicked"));
    }

    [UpstreamTest("OO-025 should not throw on exposeFunction when oopif detaches")]
    public async Task ShouldNotThrowOnExposeFunctionWhenOopifDetaches(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/dynamic-oopif.html");
        await AssertOOPIFCount(browser, 1);
        await PromiseAll(
            page.ExposeFunctionAsync("myFunc", () => 2022),
            page.EvaluateAsync("() => document.querySelector('iframe').remove()"));
        Assert.That(await page.EvaluateAsync<int>("() => window.myFunc()"), Is.EqualTo(2022));
    }

    [UpstreamTest("OO-026 should intercept response body from oopif")]
    public async Task ShouldInterceptResponseBodyFromOopif(IPage page, IBrowser browser, Server server)
    {
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/grid.html"),
            page.GotoAsync(server.Prefix + "/dynamic-oopif.html"));
        Assert.That(await response.TextAsync(), Is.Not.Empty);
    }

    [UpstreamTest("OO-027 should navigate a local frame to oopif with frame.goto")]
    public async Task ShouldNavigateALocalFrameToOopifWithFrameGoto(IPage page, IBrowser browser, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        await AssertOOPIFCount(browser, 0);
        var frame = page.Frames[1];
        Assert.That(frame.Url, Is.EqualTo(server.Prefix + "/frames/frame.html"));

        var navigations = new List<string>();
        page.FrameNavigated += (_, f) => navigations.Add(f.Url);

        var response = await frame.GotoAsync(server.CrossProcessPrefix + "/grid.html");
        await AssertOOPIFCount(browser, 1);
        Assert.That(response.Url, Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        Assert.That(response.Frame, Is.SameAs(frame));
        Assert.That(frame.Url, Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        Assert.That(await frame.EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        Assert.That(navigations, Is.EqualTo(new[] { server.CrossProcessPrefix + "/grid.html" }));
        Assert.That(page.Frames.Count, Is.EqualTo(2));
    }

    [UpstreamTest("OO-028 should allow to re-connect to OOPIFs with CDP when iframes were there already")]
    public async Task ShouldAllowToReConnectToOOPIFsWithCDPWhenIframesWereThereAlready(IBrowserType browserType, Server server, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PWTEST_CHANNEL")), "Test default channel only");

        var cdpPort = 10123 + (Info.ParallelIndex * 4);
        var hostBrowser = await browserType.LaunchAsync(new(launchOptions) { Channel = "chromium", Args = ["--remote-debugging-port=" + cdpPort] });
        var hostPage = await hostBrowser.NewPageAsync(newPageOptions);
        await hostPage.GotoAsync(server.Prefix + "/dynamic-oopif.html");

        var browser = await browserType.ConnectOverCDPAsync($"http://localhost:{cdpPort}");
        var page = browser.Contexts[0].Pages[0];
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        await AssertOOPIFCount(browser, 1);
        Assert.That(await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(server.CrossProcessPrefix + "/grid.html"));
        await browser.CloseAsync();
        await hostBrowser.CloseAsync();
    }

    [UpstreamTest("OO-029 should report the whole frame tree when connecting over CDP to nested OOPIFs")]
    public async Task ShouldReportTheWholeFrameTreeWhenConnectingOverCDPToNestedOOPIFs(IBrowserType browserType, Server server, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PWTEST_CHANNEL")), "Test default channel only");

        // top > cross-site iframe (oopif) > same-process child iframe > sandboxed srcdoc iframe (oopif).
        server.SetRoute("/nested-top.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<h1 id=top>TOP</h1><iframe src=\"{server.CrossProcessPrefix}/nested-b.html\"></iframe>");
        });
        server.SetRoute("/nested-b.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<iframe src=\"/nested-c.html\"></iframe>");
        });
        server.SetRoute("/nested-c.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<iframe sandbox=\"allow-scripts\" srcdoc=\"<p>D</p>\"></iframe>");
        });

        var cdpPort = 10123 + (Info.ParallelIndex * 4);
        var hostBrowser = await browserType.LaunchAsync(new(launchOptions)
        {
            Channel = "chromium",
            Args = ["--remote-debugging-port=" + cdpPort, "--site-per-process", "--enable-features=IsolateSandboxedIframes"],
        });
        var hostPage = await hostBrowser.NewPageAsync(newPageOptions);
        await hostPage.GotoAsync(server.Prefix + "/nested-top.html");

        var browser = await browserType.ConnectOverCDPAsync($"http://localhost:{cdpPort}");
        await AssertOOPIFCount(browser, 2);
        var page = browser.Contexts[0].Pages[0];

        var expectedTree = new[]
        {
            server.Prefix + "/nested-top.html",
            "    " + server.CrossProcessPrefix + "/nested-b.html",
            "        " + server.CrossProcessPrefix + "/nested-c.html",
            "            about:srcdoc",
        };
        async Task<List<string>> dumpFrames(IFrame frame, string indent = "")
        {
            Assert.That(await frame.EvaluateAsync<string>("() => '' + location.href"), Is.EqualTo(frame.Url));
            var result = new List<string> { indent + frame.Url };
            foreach (var child in frame.ChildFrames)
            {
                result.AddRange(await dumpFrames(child, indent + "    "));
            }
            return result;
        }
        Assert.That(await dumpFrames(page.MainFrame), Is.EqualTo(expectedTree));
        Assert.That(page.Frames.Count, Is.EqualTo(4));

        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/nested-top.html"));
        Assert.That(await page.EvaluateAsync<bool>("() => window.top === window"), Is.EqualTo(true));
        await Expect(page.Locator("#top")).ToHaveTextAsync("TOP");
        await Expect(page.FrameLocator("iframe").FrameLocator("iframe").FrameLocator("iframe").Locator("p")).ToHaveTextAsync("D");

        await browser.CloseAsync();
        await hostBrowser.CloseAsync();
    }

    private static async Task AssertOOPIFCount(IBrowser browser, int count)
    {
        if (browser.BrowserType.Name != "chromium")
        {
            return;
        }
        Assert.That(await CountOOPIFs(browser), Is.EqualTo(count));
    }

    private static async Task<int> CountOOPIFs(IBrowser browser)
    {
        var browserSession = await browser.NewBrowserCDPSessionAsync();
        var oopifs = new List<JsonElement>();
        browserSession.Event("Target.targetCreated").OnEvent += (_, e) =>
        {
            var targetInfo = e.Value.GetProperty("targetInfo");
            if (targetInfo.GetProperty("type").GetString() == "iframe")
            {
                oopifs.Add(targetInfo);
            }
        };
        await browserSession.SendAsync("Target.setDiscoverTargets", new() { ["discover"] = true });
        await browserSession.DetachAsync();
        return oopifs.Count;
    }
}
