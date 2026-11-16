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

/// <summary>Port of tests/library/browsercontext-viewport-mobile.spec.ts.</summary>
public class BrowsercontextViewportMobileTests
{
    public class MobileViewport
    {
        [BeforeEach]
        public static void Skip(string browserName, bool isBidi) => It.Skip(browserName == "firefox" && !isBidi);

        [UpstreamTest("DV-011 should support mobile emulation")]
        public async Task ShouldSupportMobileEmulation(IPlaywright playwright, IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var iPhone = playwright.Devices["iPhone 6"];
            var context = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone), contextOptions));
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<int>("() => window.innerWidth"), Is.EqualTo(375));
            await page.SetViewportSizeAsync(400, 300);
            Assert.That(await page.EvaluateAsync<int>("() => window.innerWidth"), Is.EqualTo(400));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-012 should support touch emulation")]
        public async Task ShouldSupportTouchEmulation(IPlaywright playwright, IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            // .NET: the function dispatchTouch() that upstream declares at the end of the test, as its source.
            const string dispatchTouch = @"function dispatchTouch() {
      let fulfill;
      const promise = new Promise(x => fulfill = x);
      window.ontouchstart = function(e) {
        fulfill('Received touch');
      };
      window.dispatchEvent(new Event('touchstart'));

      fulfill('Did not receive touch');

      return promise;
    }";

            var iPhone = playwright.Devices["iPhone 6"];
            var context = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone), contextOptions));
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window"), Is.EqualTo(true));
            Assert.That(await page.EvaluateAsync<string>(dispatchTouch), Is.EqualTo("Received touch"));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-013 should be detectable")]
        public async Task ShouldBeDetectable(IPlaywright playwright, IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var iPhone = playwright.Devices["iPhone 6"];
            var context = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone), contextOptions));
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window || !!window.TouchEvent"), Is.EqualTo(true));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-014 should detect touch when applying viewport with touches")]
        public async Task ShouldDetectTouchWhenApplyingViewportWithTouches(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 800, Height = 600 }, HasTouch = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await page.EvaluateAsync<bool>("() => 'ontouchstart' in window || !!window.TouchEvent"), Is.EqualTo(true));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-015 should support landscape emulation")]
        public async Task ShouldSupportLandscapeEmulation(IPlaywright playwright, IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var iPhone = playwright.Devices["iPhone 6"];
            var iPhoneLandscape = playwright.Devices["iPhone 6 landscape"];
            var context1 = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone), contextOptions));
            var page1 = await context1.NewPageAsync();
            await page1.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page1.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"), Is.EqualTo(false));
            var context2 = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhoneLandscape), contextOptions));
            var page2 = await context2.NewPageAsync();
            Assert.That(await page2.EvaluateAsync<bool>("() => matchMedia('(orientation: landscape)').matches"), Is.EqualTo(true));
            await context1.CloseAsync();
            await context2.CloseAsync();
        }

        [UpstreamTest("DV-016 should support window.orientation emulation")]
        public async Task ShouldSupportWindowOrientationEmulation(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 300, Height = 400 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<int>("() => window.orientation"), Is.EqualTo(0));
            await page.SetViewportSizeAsync(400, 300);
            Assert.That(await page.EvaluateAsync<int>("() => window.orientation"), Is.EqualTo(90));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-017 should preserve window.orientation override after navigation")]
        public async Task ShouldPreserveWindowOrientationOverrideAfterNavigation(IBrowser browser, Server server, bool isFrozenWebkit, BrowserNewContextOptions contextOptions)
        {
            It.Skip(isFrozenWebkit);

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 400, Height = 300 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<int>("() => window.orientation"), Is.EqualTo(90));
            await page.GotoAsync(server.CrossProcessPrefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<int>("() => window.orientation"), Is.EqualTo(90));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-018 should preserve screen.orientation.type override after navigation")]
        public async Task ShouldPreserveScreenOrientationTypeOverrideAfterNavigation(IBrowser browser, Server server, bool isFrozenWebkit, BrowserNewContextOptions contextOptions)
        {
            It.Skip(isFrozenWebkit);

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 300, Height = 400 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<string>("() => window.screen.orientation.type"), Is.EqualTo("portrait-primary"));
            await page.GotoAsync(server.CrossProcessPrefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<string>("() => window.screen.orientation.type"), Is.EqualTo("portrait-primary"));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-019 should fire orientationchange event")]
        public async Task ShouldFireOrientationchangeEvent(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 300, Height = 400 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            await page.EvaluateAsync(@"() => {
      let counter = 0;
      window.addEventListener('orientationchange', () => console.log(++counter));
    }");

            var event1 = page.WaitForConsoleMessageAsync();
            await page.SetViewportSizeAsync(400, 300);
            Assert.That((await event1).Text, Is.EqualTo("1"));

            var event2 = page.WaitForConsoleMessageAsync();
            await page.SetViewportSizeAsync(300, 400);
            Assert.That((await event2).Text, Is.EqualTo("2"));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-020 default mobile viewports to 980 width")]
        public async Task DefaultMobileViewportsTo980Width(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/empty.html");
            Assert.That(await page.EvaluateAsync<int>("() => window.innerWidth"), Is.EqualTo(980));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-021 respect meta viewport tag")]
        public async Task RespectMetaViewportTag(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<int>("() => window.innerWidth"), Is.EqualTo(320));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-022 should emulate the hover media feature")]
        public async Task ShouldEmulateTheHoverMediaFeature(IPlaywright playwright, IBrowser browser, BrowserNewPageOptions newPageOptions)
        {
            var iPhone = playwright.Devices["iPhone 6"];
            // .NET: playwright.Devices has BrowserNewContextOptions, and NewPageAsync takes BrowserNewPageOptions with the same properties.
            var mobilepage = await browser.NewPageAsync(MergeOptions(MergeOptions(new BrowserNewPageOptions(), iPhone), newPageOptions));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(hover: hover)').matches"), Is.EqualTo(false));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(hover: none)').matches"), Is.EqualTo(true));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(any-hover: hover)').matches"), Is.EqualTo(false));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(any-hover: none)').matches"), Is.EqualTo(true));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(pointer: coarse)').matches"), Is.EqualTo(true));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(pointer: fine)').matches"), Is.EqualTo(false));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(any-pointer: coarse)').matches"), Is.EqualTo(true));
            Assert.That(await mobilepage.EvaluateAsync<bool>("() => matchMedia('(any-pointer: fine)').matches"), Is.EqualTo(false));
            await mobilepage.CloseAsync();

            var desktopPage = await browser.NewPageAsync(newPageOptions);
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(hover: none)').matches"), Is.EqualTo(false));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(hover: hover)').matches"), Is.EqualTo(true));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(any-hover: none)').matches"), Is.EqualTo(false));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(any-hover: hover)').matches"), Is.EqualTo(true));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(pointer: coarse)').matches"), Is.EqualTo(false));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(pointer: fine)').matches"), Is.EqualTo(true));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(any-pointer: coarse)').matches"), Is.EqualTo(false));
            Assert.That(await desktopPage.EvaluateAsync<bool>("() => matchMedia('(any-pointer: fine)').matches"), Is.EqualTo(true));
            await desktopPage.CloseAsync();
        }

        [UpstreamTest("DV-023 mouse should work with mobile viewports and cross process navigations")]
        public async Task MouseShouldWorkWithMobileViewportsAndCrossProcessNavigations(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            // @see https://crbug.com/929806
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 360, Height = 640 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            await page.GotoAsync(server.CrossProcessPrefix + "/mobile.html");
            await page.EvaluateAsync(@"() => {
      document.addEventListener('click', event => {
        window['result'] = { x: event.clientX, y: event.clientY };
      });
    }");

            await page.Mouse.ClickAsync(30, 40);

            ExpectToEqual(await page.EvaluateAsync<object>("result"), new { x = 30, y = 40 });
            await context.CloseAsync();
        }

        [UpstreamTest("DV-024 should scroll when emulating a mobile viewport")]
        public async Task ShouldScrollWhenEmulatingAMobileViewport(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions)
            {
                ViewportSize = new() { Width = 1000, Height = 600 },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/input/scrollable.html");
            await page.Mouse.MoveAsync(50, 60);
            var error = await Catch(() => page.Mouse.WheelAsync(0, 100));
            if (browserName == "webkit")
            {
                Assert.That(error.Message, Does.Contain("Mouse wheel is not supported in mobile WebKit"));
            }
            else
            {
                await page.WaitForFunctionAsync("window.scrollY === 100");
            }
            await context.CloseAsync();
        }

        [UpstreamTest("DV-025 should scroll mobile page with background-attachment: fixed")]
        public async Task ShouldScrollMobilePageWithBackgroundAttachmentFixed(IPlaywright playwright, IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var iPhone = playwright.Devices["iPhone 12"];
            var context = await browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone), contextOptions));
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/input/background-fixed.html");
            await page.GetByRole(AriaRole.Button).ClickAsync();
            Assert.That(await page.EvaluateAsync<double>("() => window.scrollY"), Is.GreaterThan(1000));
            await context.CloseAsync();
        }

        [UpstreamTest("DV-026 view scale should reset after navigation")]
        public async Task ViewScaleShouldResetAfterNavigation(IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions)
            {
                ViewportSize = new() { Width = 390, Height = 664 },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync("data:text/html,<meta name='viewport' content='device-width, initial-scale=1'><button>Mobile Viewport</button>");
            await page.RouteAsync("**/button.html", route =>
            {
                IgnoreErrors(route.FulfillAsync(new()
                {
                    Body = @"<body>
          <button>Click me</button>
          <script>
            window.clicks = [];
            document.addEventListener('click', e => {
              const dot = document.createElement('div');
              dot.style.position = 'absolute';
              dot.style.width = '10px';
              dot.style.height = '10px';
              dot.style.borderRadius = '5px';
              dot.style.backgroundColor = 'red';
              dot.style.left = e.pageX + 'px';
              dot.style.top = e.pageY + 'px';
              dot.textContent = 'x: ' + e.pageX + ' y: ' + e.pageY;
              document.body.appendChild(dot);
              window.clicks.push({ x: e.pageX, y: e.pageY });
            });
          </script>
        </body>",
                    ContentType = "text/html",
                }));
            });
            await page.GotoAsync("http://localhost/button.html");
            await page.GetByText("Click me").ClickAsync(new() { Force = true });
            var box = await page.Locator("button").BoundingBoxAsync();
            var clicks = await page.EvaluateAsync<JsonElement>("() => window.clicks");
            Assert.That(clicks.GetArrayLength(), Is.EqualTo(1));
            // .NET: const [{ x, y }] = clicks;
            var x = clicks[0].GetProperty("x").GetDouble();
            var y = clicks[0].GetProperty("y").GetDouble();
            var isClickInsideButton = box.X <= x && x <= box.X + box.Width && box.Y <= y && y <= box.Y + box.Height;
            Assert.That(isClickInsideButton, Is.EqualTo(true));
            await context.CloseAsync();
        }
    }
}
