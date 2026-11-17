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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/defaultbrowsercontext-2.spec.ts.</summary>
public class Defaultbrowsercontext2Tests
{
    [BeforeEach]
    public static void Skip(string mode) => Info.Skip(mode != "default", "Remote persistent contexts are not supported");

    [UpstreamTest("DC-012 should support hasTouch option")]
    public async Task ShouldSupportHasTouchOption(Server server, LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { HasTouch = true });
        await page.GotoAsync(server.Prefix + "/mobile.html");
        Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(true));
    }

    [UpstreamTest("DC-013 should work in persistent context")]
    public async Task ShouldWorkInPersistentContext(Server server, LaunchPersistent launchPersistent, string browserName)
    {
        var (_, page) = await launchPersistent(new() { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
        await page.GotoAsync(server.Prefix + "/empty.html");
        Assert.That(await page.EvaluateAsync<int>("() => window.innerWidth"), Is.EqualTo(980));
    }

    [UpstreamTest("DC-014 should support colorScheme option")]
    public async Task ShouldSupportColorSchemeOption(LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { ColorScheme = ColorScheme.Dark });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("DC-015 should support reducedMotion option")]
    public async Task ShouldSupportReducedMotionOption(LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("DC-016 should support forcedColors option")]
    public async Task ShouldSupportForcedColorsOption(LaunchPersistent launchPersistent, string browserName)
    {
        var (_, page) = await launchPersistent(new() { ForcedColors = ForcedColors.Active });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("DC-017 should support contrast option")]
    public async Task ShouldSupportContrastOption(LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { Contrast = Contrast.More });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-contrast: more)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-contrast: no-preference)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("DC-018 should support timezoneId option")]
    public async Task ShouldSupportTimezoneIdOption(LaunchPersistent launchPersistent, string browserName)
    {
        var (_, page) = await launchPersistent(new() { Locale = "en-US", TimezoneId = "America/Jamaica" });
        Assert.That(await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"), Is.EqualTo("Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)"));
    }

    [UpstreamTest("DC-019 should support locale option")]
    public async Task ShouldSupportLocaleOption(LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { Locale = "fr-FR" });
        Assert.That(await page.EvaluateAsync<string>("() => navigator.language"), Is.EqualTo("fr-FR"));
    }

    [UpstreamTest("DC-020 should support geolocation and permissions options")]
    public async Task ShouldSupportGeolocationAndPermissionsOptions(Server server, LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { Geolocation = new() { Longitude = 10, Latitude = 10 }, Permissions = new[] { "geolocation" } });
        await page.GotoAsync(server.EmptyPage);
        var geolocation = await page.EvaluateAsync<object>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
    resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
  }))");
        ExpectToEqual(geolocation, new { latitude = 10, longitude = 10 });
    }

    [UpstreamTest("DC-021 should support ignoreHTTPSErrors option")]
    public async Task ShouldSupportIgnoreHTTPSErrorsOption(Server httpsServer, LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { IgnoreHTTPSErrors = true });
        Exception error = null;
        IResponse response = null;
        try
        {
            response = await page.GotoAsync(httpsServer.EmptyPage);
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error, Is.Null);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("DC-022 should support extraHTTPHeaders option")]
    public async Task ShouldSupportExtraHTTPHeadersOption(Server server, LaunchPersistent launchPersistent)
    {
        var (_, page) = await launchPersistent(new() { ExtraHTTPHeaders = new Dictionary<string, string> { ["foo"] = "bar" } });
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("DC-023 should accept userDataDir")]
    public async Task ShouldAcceptUserDataDir(CreateUserDataDir createUserDataDir, IBrowserType browserType, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var userDataDir = await createUserDataDir();
        var context = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        Assert.That(Directory.GetFileSystemEntries(userDataDir).Length, Is.GreaterThan(0));
        await context.CloseAsync();
        Assert.That(Directory.GetFileSystemEntries(userDataDir).Length, Is.GreaterThan(0));
    }

    [UpstreamTest("DC-024 should accept relative userDataDir")]
    public async Task ShouldAcceptRelativeUserDataDir(CreateUserDataDir createUserDataDir, IBrowserType browserType, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var userDataDir = await createUserDataDir();
        var context = await browserType.LaunchPersistentContextAsync(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Combine(userDataDir, "foobar")), launchPersistentContextOptions);
        Assert.That(Directory.GetFileSystemEntries(Path.Combine(userDataDir, "foobar")).Length, Is.GreaterThan(0));
        await context.CloseAsync();
    }

    [UpstreamTest("DC-025 should restore state from userDataDir")]
    public async Task ShouldRestoreStateFromUserDataDir(IBrowserType browserType, Server server, CreateUserDataDir createUserDataDir, string channel, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        Info.Slow();
        Info.Fixme(channel == "webkit-wsl", "Pending local storage writes are lost on close, see https://github.com/microsoft/playwright-browsers/issues/2275");

        var userDataDir = await createUserDataDir();
        var browserContext = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        var page = await browserContext.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync("() => localStorage.hey = 'hello'");
        // Browsers do not persist local storage immediately, they do it asynchronously in another process.
        // Navigate away to give it a chance to save (best-effort).
        await page.GotoAsync(server.EmptyPage);
        await browserContext.CloseAsync();

        var browserContext2 = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        var page2 = await browserContext2.NewPageAsync();
        await page2.GotoAsync(server.EmptyPage);
        Assert.That(await page2.EvaluateAsync<string>("() => localStorage.hey"), Is.EqualTo("hello"));
        await browserContext2.CloseAsync();

        var userDataDir2 = await createUserDataDir();
        var browserContext3 = await browserType.LaunchPersistentContextAsync(userDataDir2, launchPersistentContextOptions);
        var page3 = await browserContext3.NewPageAsync();
        await page3.GotoAsync(server.EmptyPage);
        Assert.That(await page3.EvaluateAsync<string>("() => localStorage.hey"), Is.Not.EqualTo("hello"));
        await browserContext3.CloseAsync();
    }

    [UpstreamTest("DC-026 should create userDataDir if it does not exist")]
    public async Task ShouldCreateUserDataDirIfItDoesNotExist(CreateUserDataDir createUserDataDir, IBrowserType browserType, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var userDataDir = Path.Combine(await createUserDataDir(), "nonexisting");
        var context = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        await context.CloseAsync();
        Assert.That(Directory.GetFileSystemEntries(userDataDir).Length, Is.GreaterThan(0));
    }

    [UpstreamTest("DC-027 should goto about:blank on relaunched persistent context")]
    public async Task ShouldGotoAboutBlankOnRelaunchedPersistentContext(IBrowserType browserType, CreateUserDataDir createUserDataDir, string browserName, bool isBidi, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        Info.Fixme(browserName == "firefox" && !isBidi);

        var userDataDir = await createUserDataDir();

        var context1 = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        await context1.Pages[0].GotoAsync("about:blank");
        await context1.CloseAsync();

        // When relaunching with an existing profile, Firefox session restore can race with the user's goto and cause "interrupted by another navigation".
        // This issue is timing-sensitive and might not fire on every run, so rely on CI's --repeat-each matrix for coverage.
        var context2 = await browserType.LaunchPersistentContextAsync(userDataDir, launchPersistentContextOptions);
        await context2.Pages[0].GotoAsync("about:blank");
        Assert.That(context2.Pages[0].Url, Is.EqualTo("about:blank"));
        await context2.CloseAsync();
    }

    [UpstreamTest("DC-028 should have default URL when launching browser")]
    public async Task ShouldHaveDefaultURLWhenLaunchingBrowser(LaunchPersistent launchPersistent)
    {
        var (context, _) = await launchPersistent();
        var urls = context.Pages.Select(page => page.Url);
        Assert.That(urls, Is.EqualTo(new[] { "about:blank" }));
    }

    [UpstreamTest("DC-029 should throw if page argument is passed")]
    public async Task ShouldThrowIfPageArgumentIsPassed(IBrowserType browserType, Server server, CreateUserDataDir createUserDataDir, string browserName, bool isBidi, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        Info.Skip(browserName == "firefox" && !isBidi);

        var options = new BrowserTypeLaunchPersistentContextOptions(launchPersistentContextOptions) { Args = new[] { server.EmptyPage } };
        var error = await Catch(async () => await browserType.LaunchPersistentContextAsync(await createUserDataDir(), options));
        Assert.That(error.Message, Does.Contain("can not specify page"));
    }

    [UpstreamTest("DC-033 should fire close event for a persistent context")]
    public async Task ShouldFireCloseEventForAPersistentContext(LaunchPersistent launchPersistent)
    {
        var (context, _) = await launchPersistent();
        var closed = false;
        context.Close += (_, _) => closed = true;
        await context.CloseAsync();
        Assert.That(closed, Is.EqualTo(true));
    }

    [UpstreamTest("DC-035 should respect selectors")]
    public async Task ShouldRespectSelectors(IPlaywright playwright, LaunchPersistent launchPersistent)
    {
        const string defaultContextCSS = @"() => ({
    query(root, selector) {
      return root.querySelector(selector);
    },
    queryAll(root, selector) {
      return Array.from(root.querySelectorAll(selector));
    }
  })";
        // .NET: a function is the text of a script that calls it.
        await playwright.Selectors.RegisterAsync("defaultContextCSS", new() { Script = "(" + defaultContextCSS + ")()" });

        var (_, page) = await launchPersistent();
        await page.SetContentAsync("<div>hello</div>");
        Assert.That(await page.InnerHTMLAsync("css=div"), Is.EqualTo("hello"));
        Assert.That(await page.InnerHTMLAsync("defaultContextCSS=div"), Is.EqualTo("hello"));
    }

    [UpstreamTest("DC-037 should support har option")]
    public async Task ShouldSupportHarOption(LaunchPersistent launchPersistent, Func<string, string> asset)
    {
        var path = asset("har-fulfill.har");
        var (_, page) = await launchPersistent();
        await page.RouteFromHARAsync(path);
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [UpstreamTest("DC-039 dialog.accept should work")]
    public async Task DialogAcceptShouldWork(LaunchPersistent launchPersistent)
    {
        var (context, page) = await launchPersistent();
        await page.GotoAsync("data:text/html,<html><title>Title</title><button onclick=\"alert('Alert')\">Button</button></html>");
        var shown = false;
        page.Dialog += (_, dialog) =>
        {
            shown = true;
            IgnoreErrors(dialog.AcceptAsync());
        };
        await page.GetByRole(AriaRole.Button, new() { Name = "Button" }).ClickAsync();
        Assert.That(shown, Is.EqualTo(true));
        await context.CloseAsync();
    }

    [UpstreamTest("DC-040 CacheStorage entry should survive page.reload()")]
    public async Task CacheStorageEntryShouldSurvivePageReload(LaunchPersistent launchPersistent, Server server)
    {
        var (context, page) = await launchPersistent();
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"async () => {
    const cache = await caches.open('repro-cache');
    await cache.put('/meta', new Response('payload'));
  }");

        await page.ReloadAsync();

        var after = await page.EvaluateAsync<string>(@"async () => {
    const cache = await caches.open('repro-cache');
    const resp = await cache.match('/meta');
    return resp ? await resp.text() : null;
  }");
        Assert.That(after, Is.EqualTo("payload"));
        await context.CloseAsync();
    }

    [UpstreamTest("DC-041 exposes browser")]
    public async Task ExposesBrowser(LaunchPersistent launchPersistent, BrowserNewPageOptions newPageOptions)
    {
        var (context, _) = await launchPersistent();
        var browser = context.Browser;
        Assert.That(browser.Version, Is.Not.Empty);
        var page = await browser.NewPageAsync(newPageOptions);
        await page.GotoAsync("data:text/html,<html><title>Title</title></html>");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Title"));
        await browser.CloseAsync();
        Assert.That(context.Pages.Count, Is.EqualTo(0));
        // Next line should not throw.
        await context.CloseAsync();
    }

    [UpstreamTest("DC-042 should support storage.getDirectory()")]
    public async Task ShouldSupportStorageGetDirectory(LaunchPersistent launchPersistent, Server server)
    {
        var (context, _) = await launchPersistent();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        object name;
        try
        {
            name = await page.EvaluateAsync<string>(@"async () => {
    const dir = await navigator.storage.getDirectory();
    return dir.name;
  }");
        }
        catch (Exception e)
        {
            name = e;
        }
        Assert.That(name, Is.EqualTo(""));
        await context.CloseAsync();
    }
}
