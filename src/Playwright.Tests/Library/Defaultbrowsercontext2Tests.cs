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
public class Defaultbrowsercontext2Tests : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(Mode != "default", "Remote persistent contexts are not supported");

    [UpstreamTest("DC-012 should support hasTouch option")]
    public async Task ShouldSupportHasTouchOption()
    {
        var (_, page) = await LaunchPersistent(new() { HasTouch = true });
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(true));
    }

    [UpstreamTest("DC-013 should work in persistent context")]
    public async Task ShouldWorkInPersistentContext()
    {
        var (_, page) = await LaunchPersistent(new() { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
        await page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(await page.EvaluateAsync<int>("() => window.innerWidth"), Is.EqualTo(980));
    }

    [UpstreamTest("DC-014 should support colorScheme option")]
    public async Task ShouldSupportColorSchemeOption()
    {
        var (_, page) = await LaunchPersistent(new() { ColorScheme = ColorScheme.Dark });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"), Is.EqualTo(false));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"), Is.EqualTo(true));
    }

    [UpstreamTest("DC-015 should support reducedMotion option")]
    public async Task ShouldSupportReducedMotionOption()
    {
        var (_, page) = await LaunchPersistent(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("DC-016 should support forcedColors option")]
    public async Task ShouldSupportForcedColorsOption()
    {
        var (_, page) = await LaunchPersistent(new() { ForcedColors = ForcedColors.Active });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: none)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("DC-017 should support contrast option")]
    public async Task ShouldSupportContrastOption()
    {
        var (_, page) = await LaunchPersistent(new() { Contrast = Contrast.More });
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-contrast: more)').matches"), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<bool>("() => matchMedia('(prefers-contrast: no-preference)').matches"), Is.EqualTo(false));
    }

    [UpstreamTest("DC-018 should support timezoneId option")]
    public async Task ShouldSupportTimezoneIdOption()
    {
        var (_, page) = await LaunchPersistent(new() { Locale = "en-US", TimezoneId = "America/Jamaica" });
        Assert.That(await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"), Is.EqualTo("Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)"));
    }

    [UpstreamTest("DC-019 should support locale option")]
    public async Task ShouldSupportLocaleOption()
    {
        var (_, page) = await LaunchPersistent(new() { Locale = "fr-FR" });
        Assert.That(await page.EvaluateAsync<string>("() => navigator.language"), Is.EqualTo("fr-FR"));
    }

    [UpstreamTest("DC-020 should support geolocation and permissions options")]
    public async Task ShouldSupportGeolocationAndPermissionsOptions()
    {
        var (_, page) = await LaunchPersistent(new() { Geolocation = new() { Longitude = 10, Latitude = 10 }, Permissions = new[] { "geolocation" } });
        await page.GotoAsync(Server.EmptyPage);
        var geolocation = await page.EvaluateAsync<object>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
    resolve({ latitude: position.coords.latitude, longitude: position.coords.longitude });
  }))");
        ExpectToEqual(geolocation, new { latitude = 10, longitude = 10 });
    }

    [UpstreamTest("DC-021 should support ignoreHTTPSErrors option")]
    public async Task ShouldSupportIgnoreHTTPSErrorsOption()
    {
        var (_, page) = await LaunchPersistent(new() { IgnoreHTTPSErrors = true });
        Exception error = null;
        IResponse response = null;
        try
        {
            response = await page.GotoAsync(HttpsServer.EmptyPage);
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error, Is.Null);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("DC-022 should support extraHTTPHeaders option")]
    public async Task ShouldSupportExtraHTTPHeadersOption()
    {
        var (_, page) = await LaunchPersistent(new() { ExtraHTTPHeaders = new Dictionary<string, string> { ["foo"] = "bar" } });
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.GotoAsync(Server.EmptyPage));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("DC-023 should accept userDataDir")]
    public async Task ShouldAcceptUserDataDir()
    {
        var userDataDir = await CreateUserDataDir();
        var context = await BrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentContextOptions());
        Assert.That(Directory.GetFileSystemEntries(userDataDir).Length, Is.GreaterThan(0));
        await context.CloseAsync();
        Assert.That(Directory.GetFileSystemEntries(userDataDir).Length, Is.GreaterThan(0));
    }

    [UpstreamTest("DC-024 should accept relative userDataDir")]
    public async Task ShouldAcceptRelativeUserDataDir()
    {
        var userDataDir = await CreateUserDataDir();
        var context = await BrowserType.LaunchPersistentContextAsync(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Combine(userDataDir, "foobar")), LaunchPersistentContextOptions());
        Assert.That(Directory.GetFileSystemEntries(Path.Combine(userDataDir, "foobar")).Length, Is.GreaterThan(0));
        await context.CloseAsync();
    }

    [UpstreamTest("DC-025 should restore state from userDataDir")]
    public async Task ShouldRestoreStateFromUserDataDir()
    {
        Info.Slow();
        Info.Fixme(Channel == "webkit-wsl", "Pending local storage writes are lost on close, see https://github.com/microsoft/playwright-browsers/issues/2275");

        var userDataDir = await CreateUserDataDir();
        var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentContextOptions());
        var page = await browserContext.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync("() => localStorage.hey = 'hello'");
        // Browsers do not persist local storage immediately, they do it asynchronously in another process.
        // Navigate away to give it a chance to save (best-effort).
        await page.GotoAsync(Server.EmptyPage);
        await browserContext.CloseAsync();

        var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentContextOptions());
        var page2 = await browserContext2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.That(await page2.EvaluateAsync<string>("() => localStorage.hey"), Is.EqualTo("hello"));
        await browserContext2.CloseAsync();

        var userDataDir2 = await CreateUserDataDir();
        var browserContext3 = await BrowserType.LaunchPersistentContextAsync(userDataDir2, LaunchPersistentContextOptions());
        var page3 = await browserContext3.NewPageAsync();
        await page3.GotoAsync(Server.EmptyPage);
        Assert.That(await page3.EvaluateAsync<string>("() => localStorage.hey"), Is.Not.EqualTo("hello"));
        await browserContext3.CloseAsync();
    }

    [UpstreamTest("DC-026 should create userDataDir if it does not exist")]
    public async Task ShouldCreateUserDataDirIfItDoesNotExist()
    {
        var userDataDir = Path.Combine(await CreateUserDataDir(), "nonexisting");
        var context = await BrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentContextOptions());
        await context.CloseAsync();
        Assert.That(Directory.GetFileSystemEntries(userDataDir).Length, Is.GreaterThan(0));
    }

    [UpstreamTest("DC-027 should goto about:blank on relaunched persistent context")]
    public async Task ShouldGotoAboutBlankOnRelaunchedPersistentContext()
    {
        Info.Fixme(BrowserName == "firefox" && !IsBidi);

        var userDataDir = await CreateUserDataDir();

        var context1 = await BrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentContextOptions());
        await context1.Pages[0].GotoAsync("about:blank");
        await context1.CloseAsync();

        // When relaunching with an existing profile, Firefox session restore can race with the user's goto and cause "interrupted by another navigation".
        // This issue is timing-sensitive and might not fire on every run, so rely on CI's --repeat-each matrix for coverage.
        var context2 = await BrowserType.LaunchPersistentContextAsync(userDataDir, LaunchPersistentContextOptions());
        await context2.Pages[0].GotoAsync("about:blank");
        Assert.That(context2.Pages[0].Url, Is.EqualTo("about:blank"));
        await context2.CloseAsync();
    }

    [UpstreamTest("DC-028 should have default URL when launching browser")]
    public async Task ShouldHaveDefaultURLWhenLaunchingBrowser()
    {
        var (context, _) = await LaunchPersistent();
        var urls = context.Pages.Select(page => page.Url);
        Assert.That(urls, Is.EqualTo(new[] { "about:blank" }));
    }

    [UpstreamTest("DC-029 should throw if page argument is passed")]
    public async Task ShouldThrowIfPageArgumentIsPassed()
    {
        Info.Skip(BrowserName == "firefox" && !IsBidi);

        var options = new BrowserTypeLaunchPersistentContextOptions(LaunchPersistentContextOptions()) { Args = new[] { Server.EmptyPage } };
        var error = await Catch(async () => await BrowserType.LaunchPersistentContextAsync(await CreateUserDataDir(), options));
        Assert.That(error.Message, Does.Contain("can not specify page"));
    }

    [UpstreamTest("DC-033 should fire close event for a persistent context")]
    public async Task ShouldFireCloseEventForAPersistentContext()
    {
        var (context, _) = await LaunchPersistent();
        var closed = false;
        context.Close += (_, _) => closed = true;
        await context.CloseAsync();
        Assert.That(closed, Is.EqualTo(true));
    }

    [UpstreamTest("DC-035 should respect selectors")]
    public async Task ShouldRespectSelectors()
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
        await Playwright.Selectors.RegisterAsync("defaultContextCSS", new() { Script = "(" + defaultContextCSS + ")()" });

        var (_, page) = await LaunchPersistent();
        await page.SetContentAsync("<div>hello</div>");
        Assert.That(await page.InnerHTMLAsync("css=div"), Is.EqualTo("hello"));
        Assert.That(await page.InnerHTMLAsync("defaultContextCSS=div"), Is.EqualTo("hello"));
    }

    [UpstreamTest("DC-037 should support har option")]
    public async Task ShouldSupportHarOption()
    {
        var path = Asset("har-fulfill.har");
        var (_, page) = await LaunchPersistent();
        await page.RouteFromHARAsync(path);
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script that should be followed automatically.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file that should not be used.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 0, 0)");
    }

    [UpstreamTest("DC-039 dialog.accept should work")]
    public async Task DialogAcceptShouldWork()
    {
        var (context, page) = await LaunchPersistent();
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
    public async Task CacheStorageEntryShouldSurvivePageReload()
    {
        var (context, page) = await LaunchPersistent();
        await page.GotoAsync(Server.EmptyPage);
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
    public async Task ExposesBrowser()
    {
        var (context, _) = await LaunchPersistent();
        var browser = context.Browser;
        Assert.That(browser.Version, Is.Not.Empty);
        var page = await browser.NewPageAsync(NewPageOptions());
        await page.GotoAsync("data:text/html,<html><title>Title</title></html>");
        Assert.That(await page.TitleAsync(), Is.EqualTo("Title"));
        await browser.CloseAsync();
        Assert.That(context.Pages.Count, Is.EqualTo(0));
        // Next line should not throw.
        await context.CloseAsync();
    }

    [UpstreamTest("DC-042 should support storage.getDirectory()")]
    public async Task ShouldSupportStorageGetDirectory()
    {
        var (context, _) = await LaunchPersistent();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
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
