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

public class BrowserContextViewportMobileTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support mobile emulation")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldSupportMobileEmulation()
    {
        await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.AreEqual(375, await page.EvaluateAsync<int>("window.innerWidth"));
        await page.SetViewportSizeAsync(400, 300);
        Assert.AreEqual(400, await page.EvaluateAsync<int>("window.innerWidth"));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support touch emulation")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldSupportTouchEmulation()
    {
        const string dispatchTouch = @"
            function dispatchTouch() {
              let fulfill;
              const promise = new Promise(x => fulfill = x);
              window.ontouchstart = function(e) {
                fulfill('Received touch');
              };
              window.dispatchEvent(new Event('touchstart'));

              fulfill('Did not receive touch');

              return promise;
            }";

        await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.True(await page.EvaluateAsync<bool>("'ontouchstart' in window"));
        Assert.AreEqual("Received touch", await page.EvaluateAsync<string>(dispatchTouch));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should be detectable by Modernizr")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldBeDetectableByModernizr()
    {
        await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/detect-touch.html");
        Assert.AreEqual("YES", await page.EvaluateAsync<string>("document.body.textContent.trim()"));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should detect touch when applying viewport with touches")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldDetectTouchWhenApplyingViewportWithTouches()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 800,
                Height = 600,
            },
            HasTouch = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.AddScriptTagAsync(new() { Url = Server.Prefix + "/modernizr.js" });
        Assert.True(await page.EvaluateAsync<bool>("() => Modernizr.touchevents"));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support landscape emulation")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldSupportLandscapeEmulation()
    {
        await using var context1 = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.False(await page1.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));

        await using var context2 = await Browser.NewContextAsync(Playwright.Devices["iPhone 6 landscape"]);
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.True(await page2.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should support window.orientation emulation")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldSupportWindowOrientationEmulation()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 300,
                Height = 400,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.AreEqual(0, await page.EvaluateAsync<int?>("() => window.orientation"));

        await page.SetViewportSizeAsync(400, 300);
        Assert.AreEqual(90, await page.EvaluateAsync<int?>("() => window.orientation"));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "should fire orientationchange event")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldFireOrientationChangeEvent()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 300,
                Height = 400,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        await page.EvaluateAsync(@"() => {
                window.counter = 0;
                window.addEventListener('orientationchange', () => console.log(++window.counter));
            }");

        var event1Task = page.WaitForConsoleMessageAsync();
        await page.SetViewportSizeAsync(400, 300);
        var event1 = await event1Task;
        Assert.AreEqual("1", event1.Text);

        var event2Task = page.WaitForConsoleMessageAsync();
        await page.SetViewportSizeAsync(300, 400);
        var event2 = await event2Task;
        Assert.AreEqual("2", event2.Text);
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "default mobile viewports to 980 width")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task DefaultMobileViewportsTo980Width()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 320,
                Height = 480,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual(980, await page.EvaluateAsync<int>("() => window.innerWidth"));
    }

    [PlaywrightTest("browsercontext-viewport-mobile.spec.ts", "respect meta viewport tag")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task RespectMetaViewportTag()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 320,
                Height = 480,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.AreEqual(320, await page.EvaluateAsync<int>("() => window.innerWidth"));
    }
}
