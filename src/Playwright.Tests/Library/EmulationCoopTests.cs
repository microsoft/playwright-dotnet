/**
 * Copyright (c) Microsoft Corporation.
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

/// <summary>Port of tests/library/emulation-coop.spec.ts.</summary>
public class EmulationCoopTests : UpstreamBrowserTest
{
    private static void SetupRoutes(Server server)
    {
        foreach (var (path, coop) in new[] { ("/plain.html", false), ("/coop.html", true) })
        {
            server.SetRoute(path, (req, res) =>
            {
                if (coop)
                {
                    res.SetHeader("Cross-Origin-Opener-Policy", "same-origin");
                }
                res.SetHeader("Content-Type", "text/html");
                res.End("<title>t</title><p>hello</p>");
            });
        }
    }

    private static async Task<object> Probe(IBrowser browser, Server server, BrowserNewContextOptions options, string path, PageEmulateMediaOptions media = null)
    {
        var context = await browser.NewContextAsync(options);
        var page = await context.NewPageAsync();
        if (media != null)
        {
            await page.EmulateMediaAsync(media);
        }
        await page.GotoAsync(server.Prefix + path);
        var result = await page.EvaluateAsync<object>(@"() => ({
    dark: matchMedia('(prefers-color-scheme: dark)').matches,
    dpr: devicePixelRatio,
    reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
    forcedColors: matchMedia('(forced-colors: active)').matches,
    contrast: matchMedia('(prefers-contrast: more)').matches,
    print: matchMedia('print').matches,
    lang: navigator.language,
    tz: Intl.DateTimeFormat().resolvedOptions().timeZone,
    ua: navigator.userAgent,
    touch: 'ontouchstart' in window,
    coarse: matchMedia('(pointer: coarse)').matches,
    viewport: [innerWidth, innerHeight],
    screen: [screen.width, screen.height],
    orientation: screen.orientation.type,
  })");
        await context.CloseAsync();
        return result;
    }

    public class EmulationWithCOOP : UpstreamBrowserTest
    {
        [UpstreamTest("EM-025 should preserve context options")]
        public async Task ShouldPreserveContextOptions()
        {
            SetupRoutes(Server);
            var options = new BrowserNewContextOptions(ContextOptions())
            {
                ColorScheme = ColorScheme.Dark,
                DeviceScaleFactor = 2,
                ReducedMotion = ReducedMotion.Reduce,
                Contrast = Contrast.More,
                Locale = "de-DE",
                TimezoneId = "Asia/Tokyo",
                UserAgent = "MyUA",
                HasTouch = true,
                ViewportSize = new() { Width = 500, Height = 600 },
            };
            var plain = await Probe(Browser, Server, options, "/plain.html");
            ExpectToEqual(plain, ObjectContaining(new
            {
                dark = true,
                dpr = 2,
                reducedMotion = true,
                contrast = true,
                lang = "de-DE",
                tz = "Asia/Tokyo",
                ua = "MyUA",
                touch = true,
                viewport = new[] { 500, 600 },
            }));
            var coop = await Probe(Browser, Server, options, "/coop.html");
            ExpectToEqual(coop, plain);
        }

        [UpstreamTest("EM-026 should preserve page.emulateMedia")]
        public async Task ShouldPreservePageEmulateMedia()
        {
            SetupRoutes(Server);
            var media = new PageEmulateMediaOptions { Media = Media.Print, ColorScheme = ColorScheme.Dark, ReducedMotion = ReducedMotion.Reduce, Contrast = Contrast.More, ForcedColors = ForcedColors.Active };
            var plain = await Probe(Browser, Server, new(ContextOptions()), "/plain.html", media);
            ExpectToEqual(plain, ObjectContaining(new
            {
                print = true,
                reducedMotion = true,
                contrast = true,
                forcedColors = true,
            }));
            var coop = await Probe(Browser, Server, new(ContextOptions()), "/coop.html", media);
            ExpectToEqual(coop, plain);
        }

        [UpstreamTest("EM-027 should preserve mobile emulation")]
        public async Task ShouldPreserveMobileEmulation()
        {
            SetupRoutes(Server);
            var options = new BrowserNewContextOptions(ContextOptions())
            {
                IsMobile = true,
                HasTouch = true,
                ViewportSize = new() { Width = 500, Height = 600 },
                ScreenSize = new() { Width = 700, Height = 900 },
            };
            var plain = await Probe(Browser, Server, options, "/plain.html");
            ExpectToEqual(plain, ObjectContaining(new
            {
                coarse = true,
                orientation = "portrait-primary",
            }));
            var coop = await Probe(Browser, Server, options, "/coop.html");
            ExpectToEqual(coop, plain);
        }
    }
}
