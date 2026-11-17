/**
 * Copyright 2018 Google Inc. All rights reserved.
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

/// <summary>Port of tests/library/headful.spec.ts.</summary>
public class HeadfulTests
{
    [BeforeEach]
    public static void Skip(bool headless) => Info.Skip(headless, "avoid popping windows in headless mode");

    [BeforeEach]
    public static void Skip2(string channel) => Info.Skip(channel == "chromium-headless-shell", "shell is never headed");

    [UpstreamTest("HE-001 should have default url when launching browser @smoke")]
    public async Task ShouldHaveDefaultUrlWhenLaunchingBrowser(LaunchPersistent launchPersistent)
    {
        var (context, _) = await launchPersistent();
        var urls = context.Pages.Select(page => page.Url);
        Assert.That(urls, Is.EqualTo(new[] { "about:blank" }));
    }

    [UpstreamTest("HE-002 should close browser with beforeunload page")]
    public async Task ShouldCloseBrowserWithBeforeunloadPage(LaunchPersistent launchPersistent, Server server)
    {
        Info.Slow();

        var (context, _) = await launchPersistent();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");
        await context.CloseAsync();
    }

    [UpstreamTest("HE-003 should close browsercontext with pending beforeunload dialog")]
    public async Task ShouldCloseBrowsercontextWithPendingBeforeunloadDialog(Server server, IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/beforeunload.html");
        // We have to interact with a page so that 'beforeunload' handlers
        // fire.
        await page.ClickAsync("body");
        await PromiseAll(
            Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h),
            page.CloseAsync(new() { RunBeforeUnload = true }));
        await context.CloseAsync();
    }

    [UpstreamTest("HE-004 should not crash when creating second context")]
    public async Task ShouldNotCrashWhenCreatingSecondContext(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        {
            var browserContext = await browser.NewContextAsync(contextOptions);
            await browserContext.NewPageAsync();
            await browserContext.CloseAsync();
        }
        {
            var browserContext = await browser.NewContextAsync(contextOptions);
            await browserContext.NewPageAsync();
            await browserContext.CloseAsync();
        }
    }

    [UpstreamTest("HE-005 should click when viewport size is larger than screen")]
    public async Task ShouldClickWhenViewportSizeIsLargerThanScreen(IPage page)
    {
        await page.SetViewportSizeAsync(3000, 3000);
        await page.SetContentAsync(@"
    <style>
      html, body { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
    </style>
    <button style=""position: absolute; right: 0; bottom: 0;"">Button in the bottom-right corner</button>
  ");
        await page.Locator("button").ClickAsync();
    }

    [UpstreamTest("HE-006 should dispatch click events to oversized viewports")]
    public async Task ShouldDispatchClickEventsToOversizedViewports(IPage page)
    {
        // Some prime numbers for width/height.
        const int width = 2971;
        const int height = 3067;
        await page.SetViewportSizeAsync(width, height);
        await page.EvaluateAsync(@"() => {
    window['events'] = [];
    window.addEventListener('click', event => window['events'].push({ x: event.clientX, y: event.clientY }), false);
  }");
        var expectedEvents = new List<object>();
        // Allow a little padding from the edges of viewport.
        for (var i = 3; i < 23; ++i)
        {
            var x = width - i;
            var y = height - i;
            expectedEvents.Add(new { x, y });
            await page.Mouse.MoveAsync(x, y);
            await page.Mouse.DownAsync();
            await page.Mouse.UpAsync();
        }
        var actualEvents = await page.EvaluateAsync<object>("() => window['events']");
        ExpectToEqual(expectedEvents, actualEvents);
    }

    [UpstreamTest("HE-007 should click background tab")]
    public async Task ShouldClickBackgroundTab(IPage page, Server server)
    {
        await page.SetContentAsync($"<button>Hello</button><a target=_blank href=\"{server.EmptyPage}\">empty.html</a>");
        await page.ClickAsync("a");
        await page.ClickAsync("button");
    }

    [UpstreamTest("HE-008 should close browser after context menu was triggered")]
    public async Task ShouldCloseBrowserAfterContextMenuWasTriggered(IBrowserType browserType, Server server, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        var browser = await browserType.LaunchAsync(launchOptions);
        var page = await browser.NewPageAsync(newPageOptions);
        await page.GotoAsync(server.Prefix + "/grid.html");
        await page.ClickAsync("body", new() { Button = MouseButton.Right });
        await browser.CloseAsync();
    }

    [UpstreamTest("HE-009 should(not) block third party cookies")]
    public async Task ShouldNotBlockThirdPartyCookies(IPage page, Server server, bool allowsThirdParty, string defaultSameSiteCookieValue)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"src => {
    let fulfill;
    const promise = new Promise(x => fulfill = x);
    const iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    iframe.onload = fulfill;
    iframe.src = src;
    return promise;
  }", server.CrossProcessPrefix + "/grid.html");
        var documentCookie = await page.Frames[1].EvaluateAsync<string>(@"() => {
    document.cookie = 'username=John Doe';
    return document.cookie;
  }");
        await page.WaitForTimeoutAsync(2000);
        Assert.That(documentCookie, Is.EqualTo(allowsThirdParty ? "username=John Doe" : ""));
        var cookies = await page.Context.CookiesAsync(server.CrossProcessPrefix + "/grid.html");
        if (allowsThirdParty)
        {
            ExpectToEqual(cookies, new[]
            {
                new
                {
                    domain = "127.0.0.1",
                    expires = -1,
                    httpOnly = false,
                    name = "username",
                    path = "/",
                    sameSite = defaultSameSiteCookieValue,
                    secure = false,
                    value = "John Doe",
                },
            });
        }
        else
        {
            ExpectToEqual(cookies, Array.Empty<object>());
        }
    }

    [UpstreamTest("HE-010 should not block third party SameSite=None cookies")]
    public async Task ShouldNotBlockThirdPartySameSiteNoneCookies(Server httpsServer, string browserName, IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        Info.Skip(browserName == "webkit", "No third party cookies in WebKit");
        Info.Skip(Environment.GetEnvironmentVariable("PW_CLOCK") == "frozen");
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            IgnoreHTTPSErrors = true,
        });

        httpsServer.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html",
            });
            res.End($"<iframe src=\"{httpsServer.CrossProcessPrefix}/grid.html\"></iframe>");
        });

        httpsServer.SetRoute("/grid.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Set-Cookie"] = new[] { "a=b; Path=/; Max-Age=3600; SameSite=None; Secure" },
                ["Content-Type"] = "text/html",
            });
            res.End(@"Hello world
    <script>
    setTimeout(() => fetch('/json'), 1000);
    </script>");
        });

        var cookie = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        httpsServer.SetRoute("/json", (req, res) =>
        {
            cookie.TrySetResult(req.Headers["cookie"]);
            res.End();
        });

        await page.GotoAsync(httpsServer.EmptyPage);
        Assert.That(await cookie.Task, Is.EqualTo("a=b"));
        await page.CloseAsync();
    }

    [UpstreamTest("HE-011 should not override viewport size when passed null")]
    public async Task ShouldNotOverrideViewportSizeWhenPassedNull(string browserName, Server server, IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(browserName == "webkit", "Our WebKit embedder does not respect window features");

        var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync(@"() => {
      const win = window.open(window.location.href, 'Title', 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=600,height=300,top=0,left=0');
      win.resizeTo(500, 450);
    }"));
        await popup.WaitForLoadStateAsync();
        await popup.WaitForFunctionAsync("() => window.outerWidth === 500 && window.outerHeight === 450");
        await context.CloseAsync();
    }

    [UpstreamTest("HE-012 Page.bringToFront should work")]
    public async Task PageBringToFrontShouldWork(IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var page1 = await browser.NewPageAsync(newPageOptions);
        await page1.SetContentAsync("Page1");
        var page2 = await browser.NewPageAsync(newPageOptions);
        await page2.SetContentAsync("Page2");

        await page1.BringToFrontAsync();
        Assert.That(await page1.EvaluateAsync<string>("document.visibilityState"), Is.EqualTo("visible"));
        Assert.That(await page2.EvaluateAsync<string>("document.visibilityState"), Is.EqualTo("visible"));

        await page2.BringToFrontAsync();
        Assert.That(await page1.EvaluateAsync<string>("document.visibilityState"), Is.EqualTo("visible"));
        Assert.That(await page2.EvaluateAsync<string>("document.visibilityState"), Is.EqualTo(
            "visible"));

        await page1.CloseAsync();
        await page2.CloseAsync();
    }

    [UpstreamTest("HE-013 should click in OOPIF")]
    public async Task ShouldClickInOOPIF(string browserName, LaunchPersistent launchPersistent, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End($"<iframe src=\"{server.CrossProcessPrefix}/iframe.html\"></iframe>");
        });
        server.SetRoute("/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End(@"<button id=""button"" onclick=""console.log('ok')"">Submit</button>
    <script>console.log('frame loaded')</script>");
        });

        var (_, page) = await launchPersistent();
        var consoleLog = new List<string>();
        page.Console += (_, m) => consoleLog.Add(m.Text);
        await page.GotoAsync(server.EmptyPage);
        await page.Frames[1].ClickAsync("text=Submit");
        Assert.That(consoleLog, Does.Contain("ok"));
    }

    [UpstreamTest("HE-014 should click bottom row w/ infobar in OOPIF")]
    public async Task ShouldClickBottomRowWInfobarInOOPIF(string browserName, LaunchPersistent launchPersistent, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End($@"
      <style>
        html, body {{ margin: 0; padding: 0; width: 100%; height: 100%; }}
        iframe {{ position: absolute; bottom: 0; }}
      </style>
      <iframe src=""{server.CrossProcessPrefix}/iframe.html""></iframe>
    ");
        });

        server.SetRoute("/iframe.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End(@"
      <style>
        html, body { margin: 0; padding: 0; width: 100%; height: 100%; }
        button { position: absolute; bottom: 0; }
      </style>
      <button id=""button"" onclick=""window._clicked=true"">Submit</button>");
        });

        var (_, page) = await launchPersistent();
        await page.GotoAsync(server.EmptyPage);
        if (browserName == "chromium")
        {
            // CHROME BUG:
            //   Unfortunately, on some platforms the automation infobar is shown up late or animates in.
            //   Upon showing, it triggers a resize of WebContentsView and RenderWidgetHostView
            //   through the native view hierarchy. This in turn resizes the compositor to the visible view size
            //   instead of the emulated size specified in Emulation.setDeviceMetricsOverride.
            //   Hit testing for OOPIFs is affected by the new size, and clicks do not reach the iframe.
            //
            //   The workaround is to re-apply the viewport after a delay, in this case after the navigation.
            await page.SetViewportSizeAsync(800, 600);
        }
        await page.Frames[1].ClickAsync("text=Submit");
        Assert.That(await page.Frames[1].EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HE-015 headless and headful should use same default fonts")]
    public async Task HeadlessAndHeadfulShouldUseSameDefaultFonts(IPage page, string browserName, IBrowserType browserType, bool isBidi, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        Info.Skip(browserName == "firefox" && !isBidi, "Text is misaligned in headed vs headless");

        var genericFontFamilies = new[]
        {
            "standard",
            "serif",
            "sans-serif",
            "monospace",
            "cursive",
            "fantasy",
            "emoji",
        };
        var headlessBrowser = await browserType.LaunchAsync(new(launchOptions) { Headless = true });
        var headlessPage = await headlessBrowser.NewPageAsync(newPageOptions);
        foreach (var family in genericFontFamilies)
        {
            var content = $@"<div style=""font: 15px bold {(family == "standard" ? "" : family)}; max-width: 300px; max-height: 300px; overflow: hidden"">
      Lorem ipsum dolor sit amet consectetur adipiscing elit proin, integer curabitur imperdiet rhoncus cursus tincidunt bibendum, consequat sed magnis laoreet luctus mollis tellus. Nisl parturient mus accumsan feugiat sem laoreet magnis nisi, aptent per sollicitudin gravida orci ac blandit, viverra eros praesent auctor vivamus semper bibendum. Consequat sed habitasse luctus dictumst gravida platea semper phasellus, nascetur ridiculus purus est varius quisque et scelerisque, id vehicula eleifend montes sollicitudin dis velit. Pellentesque ridiculus per natoque et eleifend taciti nunc, laoreet auctor at condimentum imperdiet ante, conubia mi cubilia scelerisque sociosqu sem.</p> <p>Curabitur magna per felis primis mauris non dapibus luctus ultricies eros, quis et egestas condimentum lobortis eget semper montes litora purus, ridiculus elementum sollicitudin imperdiet dictum lacinia parturient cras eu. Risus cum varius rhoncus eros torquent pretium taciti id erat dis egestas, nibh tristique montes convallis metus lacus phasellus blandit ut auctor bibendum semper, facilisis mi integer eget ultrices lobortis odio viverra duis dui. Risus ullamcorper lacinia in venenatis sodales fusce tortor potenti volutpat quis, dictum vulputate suspendisse velit mollis torquent sociis aptent morbi, senectus nascetur justo maecenas conubia magnis viverra gravida fames. Phasellus sed nec gravida nibh class augue lectus, blandit quis turpis orci diam nam pellentesque, ultricies metus imperdiet hendrerit lacinia lacus.</p> <p>Inceptos facilisi montes cum hendrerit, pulvinar ut tellus eget velit, arcu nulla aenean. Phasellus augue urna nostra molestie interdum vehicula, posuere fames cum euismod massa curabitur donec, inceptos cubilia tellus facilisis fermentum. Lacus laoreet facilisis ultrices cursus quisque at ad porta vestibulum massa inceptos, curae class aliquet maecenas cum ullamcorper pulvinar erat mus vitae. Cum in aenean convallis dis quam tincidunt justo sed quisque, imperdiet faucibus hendrerit felis commodo scelerisque magnis vehicula etiam leo, eros varius platea lobortis maecenas condimentum nisi phasellus. Turpis vulputate mus himenaeos sociosqu facilisis dignissim leo quam, ultricies habitasse commodo molestie est tortor vitae et, porttitor risus erat cursus phasellus facilisi litora.</p> <p>Nostra habitasse egestas magnis velit pellentesque parturient cum lectus viverra, vestibulum sociosqu nunc vel urna consequat lacinia phasellus at sapien, aenean pretium dictum sed montes interdum imperdiet iaculis. Leo hac eros arcu senectus maecenas, tortor pulvinar venenatis lacinia volutpat, mattis platea ut facilisi. Aenean condimentum at et donec sociosqu fermentum luctus potenti semper vulputate, sapien justo non est auctor gravida ultricies fames per commodo, sed habitasse facilisi nulla quisque hendrerit aliquet viverra bibendum.</p> <p>Interdum nisl quam etiam montes porttitor laoreet nullam senectus velit, mauris proin tellus imperdiet litora venenatis fames massa quis, sollicitudin justo vivamus curae in sociis suscipit facilisi. Platea inceptos lacus elementum pellentesque quam euismod dictumst sociis tincidunt vulputate porttitor eros, turpis netus ut ad tempor sapien aliquet sodales molestie consequat nostra. Cum augue in quisque primis ut nunc sodales, sem orci tempus posuere cubilia suspendisse lacinia ligula, magna sed ridiculus at maecenas habitant.</p> <p>Natoque magna ac feugiat tellus bibendum diam, metus lobortis nisl ornare varius praesent, dictumst gravida lacus parturient semper. Pellentesque faucibus congue fusce posuere placerat dictum vitae, dui vestibulum eu sociis tempus aliquam ultricies malesuada, potenti laoreet lacus sem gravida nisi. Nostra platea sagittis hendrerit congue conubia senectus bibendum quis sapien pharetra, scelerisque nam imperdiet fermentum feugiat suspendisse viverra luctus at, semper ac consequat vitae mi gravida parturient mollis nascetur. Vel taciti justo consequat primis et blandit convallis sed, felis purus fusce a venenatis etiam aenean scelerisque, fringilla volutpat sagittis egestas rutrum id dis.</p> <p>Feugiat fermentum tortor ante ac iaculis sollicitudin ut interdum, cras orci ullamcorper potenti tristique vehicula. Molestie tortor ullamcorper rutrum turpis malesuada phasellus sem ultricies praesent mattis lobortis porta, senectus venenatis diam nostra laoreet volutpat per aptent justo elementum cum. Urna cursus vel felis cras eleifend arcu enim magnis, duis rutrum nibh nascetur cubilia interdum ultrices curae, id lacus aliquam dictumst diam fringilla lacinia.</p> <p>Luctus diam morbi eget tellus libero taciti faucibus inceptos, natoque facilisis lectus maecenas risus dapibus suscipit nibh, vel curae conubia orci imperdiet metus fusce. Condimentum massa donec luctus pharetra cum, in viverra placerat nisl litora facilisis, neque nascetur sociis dictumst. Suscipit accumsan eget rhoncus pharetra justo malesuada aliquet, suspendisse metus eleifend tincidunt varius ridiculus, convallis primis vitae curabitur quis mus.</p> <p>Gravida donec lacus molestie tortor aenean ultricies blandit per tempor, nostra penatibus orci vestibulum semper lectus vel a, montes potenti cum dapibus natoque eu volutpat nulla. Himenaeos purus nam malesuada habitasse nisl pharetra laoreet feugiat mi non, ultrices ultricies a cras ante eu venenatis ligula. Suscipit ut mus habitasse at aliquet sodales commodo justo, feugiat platea sagittis phasellus eleifend pellentesque interdum iaculis, integer cubilia montes metus hendrerit tincidunt purus.</p> <p>Vel posuere tellus dapibus eget duis cubilia, nec class vehicula libero gravida ligula, tempus urna taciti donec congue. Facilisis ridiculus congue cum dui per augue natoque, molestie hac etiam pellentesque dignissim urna class, feugiat aenean massa himenaeos penatibus ut eu, convallis purus et fusce tempus mattis. At mattis suscipit porta nostra nec facilisis sodales turpis, integer et lectus conubia justo nam congue taciti odio, fermentum semper cubilia fusce nunc purus velit.
      <div/>
    ";
            await PromiseAll(page.SetContentAsync(content), headlessPage.SetContentAsync(content));
            // .NET: PNG.sync.read(buffer)
            var images = (await PromiseAll(new[]
            {
                page.ScreenshotAsync(), headlessPage.ScreenshotAsync(),
            })).Select(buffer => ImageComparator.Decode("image/png", buffer)).ToArray();
            var (image1, image2) = (images[0], images[1]);
            // .NET: compare() of utils (coreBundle)
            var count = ImageTools.Compare(image1.Data, image2.Data, null, image1.Width, image2.Height);
            Assert.That(count, Is.EqualTo(0));
        }
        await headlessBrowser.CloseAsync();
    }

    [UpstreamTest("HE-016 should have the same hyphen rendering on headless and headed")]
    public async Task ShouldHaveTheSameHyphenRenderingOnHeadlessAndHeaded(IBrowserType browserType, IPage page, bool headless, Server server, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        const string content = @"
    <!DOCTYPE html>
    <html lang=""en"">
    <head>
      <style>
        .hyphenated {
          width: 100px;
          hyphens: auto;
          text-align: justify;
          border: 1px solid black;
        }
      </style>
    </head>
    <body>
      <div class=""hyphenated"">
        supercalifragilisticexpialidocious
      </div>
    </body>
    </html>
  ";
        server.SetRoute("/hyphenated.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End(content);
        });
        var oppositeBrowser = await browserType.LaunchAsync(new(launchOptions) { Headless = !headless });
        var oppositePage = await oppositeBrowser.NewPageAsync(newPageOptions);
        await oppositePage.GotoAsync(server.Prefix + "/hyphenated.html");
        await page.GotoAsync(server.Prefix + "/hyphenated.html");

        var (divHeight1, divHeight2) = await PromiseAll(
            page.EvaluateAsync<double>("() => document.querySelector('.hyphenated').getBoundingClientRect().height"),
            oppositePage.EvaluateAsync<double>("() => document.querySelector('.hyphenated').getBoundingClientRect().height"));
        Assert.That(divHeight1, Is.EqualTo(divHeight2));
        await oppositeBrowser.CloseAsync();
    }
}
