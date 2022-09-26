/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

/// <playwright-file>defaultbrowsercontext-2.spec.ts</playwright-file>
public class DefaultBrowsercontext2Tests : PlaywrightTestEx
{
    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support hasTouch option")]
    public async Task ShouldSupportHasTouchOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            HasTouch = true
        });

        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.True(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should work in persistent context")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkInPersistentContext()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            ViewportSize = new()
            {
                Width = 320,
                Height = 480,
            },
            IsMobile = true,
        });

        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(980, await page.EvaluateAsync<int>("() => window.innerWidth"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support colorScheme option")]
    public async Task ShouldSupportColorSchemeOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            ColorScheme = ColorScheme.Dark,
        });

        Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: light)').matches"));
        Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support reducedMotion option")]
    public async Task ShouldSupportReducedMotionOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            ReducedMotion = ReducedMotion.Reduce
        });

        Assert.True(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"));
        Assert.False(await page.EvaluateAsync<bool?>("() => matchMedia('(prefers-reduced-motion: no-preference)').matches"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support timezoneId option")]
    public async Task ShouldSupportTimezoneIdOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            TimezoneId = "America/Jamaica",
        });

        Assert.AreEqual("Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)", await page.EvaluateAsync<string>("() => new Date(1479579154987).toString()"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support locale option")]
    public async Task ShouldSupportLocaleOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            Locale = "fr-FR",
        });

        Assert.AreEqual("fr-FR", await page.EvaluateAsync<string>("() => navigator.language"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support geolocation and permissions options")]
    public async Task ShouldSupportGeolocationAndPermissionsOptions()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            Geolocation = new()
            {
                Latitude = 10,
                Longitude = 10,
            },
            Permissions = new[] { "geolocation" },
        });

        await page.GotoAsync(Server.EmptyPage);
        var geolocation = await page.EvaluateAsync<Geolocation>(@"() => new Promise(resolve => navigator.geolocation.getCurrentPosition(position => {
                resolve({latitude: position.coords.latitude, longitude: position.coords.longitude});
            }))");
        Assert.AreEqual(10, geolocation.Latitude);
        Assert.AreEqual(10, geolocation.Longitude);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support ignoreHTTPSErrors option")]
    public async Task ShouldSupportIgnoreHTTPSErrorsOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            IgnoreHTTPSErrors = true
        });

        var response = await page.GotoAsync(HttpsServer.Prefix + "/empty.html");
        Assert.True(response.Ok);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should support extraHTTPHeaders option")]
    public async Task ShouldSupportExtraHTTPHeadersOption()
    {
        var (tmp, context, page) = await LaunchAsync(new()
        {
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["foo"] = "bar",
            },
        });

        string fooHeader = string.Empty;

        await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", r => fooHeader = r.Headers["foo"]),
            page.GotoAsync(Server.EmptyPage));

        Assert.AreEqual("bar", fooHeader);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should accept userDataDir")]
    public async Task ShouldAcceptUserDataDir()
    {
        var (tmp, context, _) = await LaunchAsync();
        Assert.IsNotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());
        await context.CloseAsync();
        Assert.IsNotEmpty(new DirectoryInfo(tmp.Path).GetDirectories());

        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should restore state from userDataDir")]
    public async Task ShouldRestoreStateFromUserDataDir()
    {
        using var userDataDir = new TempDirectory();

        await using (var browserContext = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path))
        {
            var page = await browserContext.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await page.EvaluateAsync("() => localStorage.hey = 'hello'");
        }

        await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path))
        {
            var page = await browserContext2.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("hello", await page.EvaluateAsync<string>("() => localStorage.hey"));
        }

        using var userDataDir2 = new TempDirectory();
        await using (var browserContext2 = await BrowserType.LaunchPersistentContextAsync(userDataDir2.Path))
        {
            var page = await browserContext2.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.That("hello", Is.Not.EqualTo(await page.EvaluateAsync<string>("() => localStorage.hey")));
        }
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should have default URL when launching browser")]
    public async Task ShouldHaveDefaultURLWhenLaunchingBrowser()
    {
        var (tmp, context, page) = await LaunchAsync();

        string[] urls = context.Pages.Select(p => p.Url).ToArray();
        Assert.AreEqual(new[] { "about:blank" }, urls);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should throw if page argument is passed")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldThrowIfPageArgumentIsPassed()
    {
        using var tmp = new TempDirectory();
        var args = new[] { Server.EmptyPage };
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
            BrowserType.LaunchPersistentContextAsync(tmp.Path, new() { Args = args }));
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should fire close event for a persistent context")]
    public async Task ShouldFireCloseEventForAPersistentContext()
    {
        var (tmp, context, _) = await LaunchAsync();
        bool closed = false;
        context.Close += (_, _) => closed = true;
        await context.CloseAsync();

        Assert.True(closed);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-2.spec.ts", "should respect selectors")]
    public async Task ShouldRespectSelectors()
    {
        var (tmp, context, page) = await LaunchAsync();
        const string defaultContextCSS = @"({
                create(root, target) {},
                query(root, selector) {
                    return root.querySelector(selector);
                },
                queryAll(root, selector) {
                    return Array.from(root.querySelectorAll(selector));
                }
            })";

        await TestUtils.RegisterEngineAsync(Playwright, "defaultContextCSS", defaultContextCSS);
        await page.SetContentAsync("<div>hello</div>");
        Assert.AreEqual("hello", await page.InnerHTMLAsync("css=div"));
        Assert.AreEqual("hello", await page.InnerHTMLAsync("defaultContextCSS=div"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchAsync(BrowserTypeLaunchPersistentContextOptions options = null)
    {
        var tmp = new TempDirectory();
        var context = await BrowserType.LaunchPersistentContextAsync(tmp.Path, options);
        var page = context.Pages.First();

        return (tmp, context, page);
    }
}
