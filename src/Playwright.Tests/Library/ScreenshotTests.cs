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

/// <summary>Port of tests/library/screenshot.spec.ts.</summary>
public class ScreenshotTests
{
    public class PageScreenshot
    {
        [BeforeEach]
        public static void Skip(string browserName, bool headless) => It.Skip(browserName == "firefox" && !headless, "Firefox headed produces a different image.");

        [UpstreamTest("SS-068 should run in parallel in multiple pages")]
        public async Task ShouldRunInParallelInMultiplePages(Server server, ContextFactory contextFactory)
        {
            var context = await contextFactory();
            const int N = 5;
            var pages = await PromiseAll(Enumerable.Repeat(0, N).Select(async _ =>
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(server.Prefix + "/grid.html");
                return page;
            }));
            var promises = new List<Task<byte[]>>();
            for (var i = 0; i < N; ++i)
            {
                promises.Add(pages[i].ScreenshotAsync(new() { Clip = new() { X = 50 * (i % 2), Y = 0, Width = 50, Height = 50 } }));
            }
            var screenshots = await PromiseAll(promises);
            for (var i = 0; i < N; ++i)
            {
                ExpectToMatchSnapshot(screenshots[i], $"grid-cell-{i % 2}.png");
            }
            await PromiseAll(pages.Select(page => page.CloseAsync()));
        }

        [UpstreamTest("SS-069 should work with a mobile viewport")]
        public async Task ShouldWorkWithAMobileViewport(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
        {
            It.Skip(browserName == "firefox");

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/overflow.html");
            var screenshot = await page.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-mobile.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-070 should work with a mobile viewport and clip")]
        public async Task ShouldWorkWithAMobileViewportAndClip(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
        {
            It.Skip(browserName == "firefox");

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/overflow.html");
            var screenshot = await page.ScreenshotAsync(new() { Clip = new() { X = 10, Y = 10, Width = 100, Height = 150 } });
            ExpectToMatchSnapshot(screenshot, "screenshot-mobile-clip.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-071 should work with a mobile viewport and fullPage")]
        public async Task ShouldWorkWithAMobileViewportAndFullPage(IBrowser browser, Server server, string browserName, BrowserNewContextOptions contextOptions)
        {
            It.Skip(browserName == "firefox");

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/overflow-large.html");
            var screenshot = await page.ScreenshotAsync(new() { FullPage = true });
            ExpectToMatchSnapshot(screenshot, "screenshot-mobile-fullpage.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-072 should work with device scale factor")]
        public async Task ShouldWorkWithDeviceScaleFactor(IBrowser browser, Server server, bool isMac, string browserName, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, DeviceScaleFactor = 2 });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-device-scale-factor.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-073 should work with device scale factor and clip")]
        public async Task ShouldWorkWithDeviceScaleFactorAndClip(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 500 }, DeviceScaleFactor = 3 });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new() { Clip = new() { X = 50, Y = 100, Width = 150, Height = 100 } });
            ExpectToMatchSnapshot(screenshot, "screenshot-device-scale-factor-clip.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-074 should work with device scale factor and scale:css")]
        public async Task ShouldWorkWithDeviceScaleFactorAndScaleCss(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, DeviceScaleFactor = 2 });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new() { Scale = ScreenshotScale.Css });
            ExpectToMatchSnapshot(screenshot, "screenshot-device-scale-factor-css-size.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-075 should produce screenshot of correct size with scale:css and null viewport")]
        public async Task ShouldProduceScreenshotOfCorrectSizeWithScaleCssAndNullViewport(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            // .NET: const [innerWidth, innerHeight] = await page.evaluate(...)
            var sizes = await page.EvaluateAsync<int[]>("() => [window.innerWidth, window.innerHeight]");
            var innerWidth = sizes[0];
            var innerHeight = sizes[1];
            var screenshot = await page.ScreenshotAsync(new() { Scale = ScreenshotScale.Css });
            // .NET: PNG.sync.read(screenshot)
            var decoded = ImageComparator.Decode("image/png", screenshot);
            Assert.That(decoded.Width, Is.EqualTo(innerWidth));
            Assert.That(decoded.Height, Is.EqualTo(innerHeight));
            await context.CloseAsync();
        }

        [UpstreamTest("SS-076 should work with device scale factor, clip and scale:css")]
        public async Task ShouldWorkWithDeviceScaleFactorClipAndScaleCss(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 500 }, DeviceScaleFactor = 3 });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new() { Clip = new() { X = 50, Y = 100, Width = 150, Height = 100 }, Scale = ScreenshotScale.Css });
            ExpectToMatchSnapshot(screenshot, "screenshot-device-scale-factor-clip-css-size.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-077 should throw if screenshot size is too large with device scale factor")]
        public async Task ShouldThrowIfScreenshotSizeIsTooLargeWithDeviceScaleFactor(IBrowser browser, string browserName, bool isMac, bool isBidi, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 500, Height = 500 }, DeviceScaleFactor = 2 });
            var page = await context.NewPageAsync();
            {
                await page.SetContentAsync("<style>body {margin: 0; padding: 0;}</style><div style='min-height: 16383px; background: red;'></div>");
                var result = await page.ScreenshotAsync(new() { FullPage = true });
                Assert.That(result, Is.Not.Null);
            }
            {
                await page.SetContentAsync("<style>body {margin: 0; padding: 0;}</style><div style='min-height: 16384px; background: red;'></div>");
                var exception = await Catch(() => page.ScreenshotAsync(new() { FullPage = true }));
                if ((browserName == "firefox" && !isBidi) || (browserName == "webkit" && !isMac))
                {
                    Assert.That(exception.Message, Does.Contain("Cannot take screenshot larger than 32767"));
                }

                var image = await page.ScreenshotAsync(new() { FullPage = true, Scale = ScreenshotScale.Css });
                Assert.That(image, Is.Not.Null);
            }
            await context.CloseAsync();
        }

        [UpstreamTest("SS-078 should work with large size")]
        public async Task ShouldWorkWithLargeSize(string browserName, bool headless, string platform, ContextFactory contextFactory, string channel)
        {
            It.Fixme(browserName == "chromium" && !headless && platform == "linux", "Chromium has gpu problems on linux with large screenshots");
            It.Slow(true, "Large screenshot is slow");

            var context = await contextFactory();
            var page = await context.NewPageAsync();

            await page.SetViewportSizeAsync(1280, 800);
            await page.EvaluateAsync(@"() => {
      document.body.style.margin = '0';
      document.body.style.padding = '0';
      document.documentElement.style.margin = '0';
      document.documentElement.style.padding = '0';
      const div = document.createElement('div');
      div.style.width = '1250px';
      div.style.height = '8440px';
      div.style.background = 'linear-gradient(red, blue)';
      document.body.appendChild(div);
    }");
            var buffer = await page.ScreenshotAsync(new() { FullPage = true });
            // .NET: PNG.sync.read(buffer)
            var decoded = ImageComparator.Decode("image/png", buffer);

            var pixel = (int x, int y) =>
            {
                // .NET: PNG.bitblt(decoded, dst, x, y, 1, 1) with a dst of one pixel
                var pixels = decoded.Data.AsSpan(((decoded.Width * y) + x) * 4, 4).ToArray();
                return new { r = pixels[0], g = pixels[1], b = pixels[2], a = pixels[3] };
            };

            Assert.That(pixel(0, 0).r, Is.GreaterThan(128));
            Assert.That(pixel(0, 0).b, Is.LessThan(128));
            Assert.That(pixel(0, 8339).r, Is.LessThan(128));
            Assert.That(pixel(0, 8339).b, Is.GreaterThan(128));
        }

        [UpstreamTest("SS-079 should handle vh units ")]
        public async Task ShouldHandleVhUnits(ContextFactory contextFactory)
        {
            var context = await contextFactory();
            var page = await context.NewPageAsync();

            await page.SetViewportSizeAsync(800, 500);
            await page.EvaluateAsync(@"() => {
      document.body.style.margin = '0';
      document.body.style.padding = '0';
      document.documentElement.style.margin = '0';
      document.documentElement.style.padding = '0';
      const div = document.createElement('div');
      div.style.width = '100%';
      div.style.borderTop = '100vh solid red';
      div.style.borderBottom = '100vh solid blue';
      document.body.appendChild(div);
    }");
            var buffer = await page.ScreenshotAsync(new() { FullPage = true });
            // .NET: PNG.sync.read(buffer)
            var decoded = ImageComparator.Decode("image/png", buffer);

            var pixel = (int x, int y) =>
            {
                // .NET: PNG.bitblt(decoded, dst, x, y, 1, 1) with a dst of one pixel
                var pixels = decoded.Data.AsSpan(((decoded.Width * y) + x) * 4, 4).ToArray();
                return new { r = pixels[0], g = pixels[1], b = pixels[2], a = pixels[3] };
            };

            Assert.That(pixel(0, 0).r, Is.GreaterThan(128));
            Assert.That(pixel(0, 0).b, Is.LessThan(128));
            Assert.That(pixel(0, 999).r, Is.LessThan(128));
            Assert.That(pixel(0, 999).b, Is.GreaterThan(128));
        }
    }

    public class ElementScreenshot
    {
        [BeforeEach]
        public static void Skip(string browserName, bool headless) => It.Skip(browserName == "firefox" && !headless);

        [UpstreamTest("SS-081 element screenshot should work with a mobile viewport")]
        public async Task ElementScreenshotShouldWorkWithAMobileViewport(IBrowser browser, Server server, string browserName, bool isBidi, BrowserNewContextOptions contextOptions)
        {
            It.Skip(browserName == "firefox" && !isBidi);

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, IsMobile = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-mobile.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-082 element screenshot should work with device scale factor")]
        public async Task ElementScreenshotShouldWorkWithDeviceScaleFactor(IBrowser browser, Server server, string browserName, bool isBidi, BrowserNewContextOptions contextOptions)
        {
            It.Skip(browserName == "firefox" && !isBidi);

            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 320, Height = 480 }, DeviceScaleFactor = 2 });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-mobile-dsf.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-083 should take screenshots when default viewport is null")]
        public async Task ShouldTakeScreenshotsWhenDefaultViewportIsNull(Server server, IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport });
            var page = await context.NewPageAsync();
            await page.SetContentAsync("<div style='height: 10000px; background: red'></div>");
            var windowSize = await page.EvaluateAsync<JsonElement>("() => ({ width: window.innerWidth * window.devicePixelRatio, height: window.innerHeight * window.devicePixelRatio })");
            var sizeBefore = await page.EvaluateAsync<JsonElement>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");

            var screenshot = await page.ScreenshotAsync();
            Assert.That(screenshot, Is.InstanceOf<byte[]>());
            // .NET: PNG.sync.read(screenshot)
            var decoded = ImageComparator.Decode("image/png", screenshot);
            Assert.That(decoded.Width, Is.EqualTo(windowSize.GetProperty("width").GetDouble()));
            Assert.That(decoded.Height, Is.EqualTo(windowSize.GetProperty("height").GetDouble()));

            var sizeAfter = await page.EvaluateAsync<JsonElement>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.That(sizeBefore.GetProperty("width").GetDouble(), Is.EqualTo(sizeAfter.GetProperty("width").GetDouble()));
            Assert.That(sizeBefore.GetProperty("height").GetDouble(), Is.EqualTo(sizeAfter.GetProperty("height").GetDouble()));
            await context.CloseAsync();
        }

        [UpstreamTest("SS-084 should take fullPage screenshots when default viewport is null")]
        public async Task ShouldTakeFullPageScreenshotsWhenDefaultViewportIsNull(Server server, IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/grid.html");
            var sizeBefore = await page.EvaluateAsync<JsonElement>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            var screenshot = await page.ScreenshotAsync(new()
            {
                FullPage = true,
            });
            Assert.That(screenshot, Is.InstanceOf<byte[]>());

            var sizeAfter = await page.EvaluateAsync<JsonElement>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.That(sizeBefore.GetProperty("width").GetDouble(), Is.EqualTo(sizeAfter.GetProperty("width").GetDouble()));
            Assert.That(sizeBefore.GetProperty("height").GetDouble(), Is.EqualTo(sizeAfter.GetProperty("height").GetDouble()));
            await context.CloseAsync();
        }

        [UpstreamTest("SS-085 should restore default viewport after fullPage screenshot")]
        public async Task ShouldRestoreDefaultViewportAfterFullPageScreenshot(IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 456, Height = 789 } });
            var page = await context.NewPageAsync();
            await VerifyViewport(page, 456, 789);
            var screenshot = await page.ScreenshotAsync(new() { FullPage = true });
            Assert.That(screenshot, Is.InstanceOf<byte[]>());
            await VerifyViewport(page, 456, 789);
            await context.CloseAsync();
        }

        [UpstreamTest("SS-088 should take element screenshot when default viewport is null and restore back")]
        public async Task ShouldTakeElementScreenshotWhenDefaultViewportIsNullAndRestoreBack(Server server, IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = ViewportSize.NoViewport });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(@"
      <div style=""height: 14px"">oooo</div>
      <style>
      div.to-screenshot {
        border: 1px solid blue;
        width: 600px;
        height: 600px;
        margin-left: 50px;
      }
      ::-webkit-scrollbar{
        display: none;
      }
      </style>
      <div class=""to-screenshot""></div>
      <div class=""to-screenshot""></div>
      <div class=""to-screenshot""></div>
    ");
            var sizeBefore = await page.EvaluateAsync<JsonElement>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            var screenshot = await elementHandle.ScreenshotAsync();
            Assert.That(screenshot, Is.InstanceOf<byte[]>());
            var sizeAfter = await page.EvaluateAsync<JsonElement>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
            Assert.That(sizeBefore.GetProperty("width").GetDouble(), Is.EqualTo(sizeAfter.GetProperty("width").GetDouble()));
            Assert.That(sizeBefore.GetProperty("height").GetDouble(), Is.EqualTo(sizeAfter.GetProperty("height").GetDouble()));
            await context.CloseAsync();
        }

        [UpstreamTest("SS-090 element screenshots should handle vh units ")]
        public async Task ElementScreenshotsShouldHandleVhUnits(ContextFactory contextFactory)
        {
            var context = await contextFactory();
            var page = await context.NewPageAsync();

            await page.SetViewportSizeAsync(800, 500);
            await page.EvaluateAsync(@"() => {
      const div = document.createElement('div');
      div.style.width = '100%';
      div.style.borderTop = '100vh solid red';
      div.style.borderBottom = '100vh solid blue';
      document.body.appendChild(div);
    }");
            var elementHandle = await page.QuerySelectorAsync("div");
            var buffer = await elementHandle.ScreenshotAsync();
            // .NET: PNG.sync.read(buffer)
            var decoded = ImageComparator.Decode("image/png", buffer);

            var pixel = (int x, int y) =>
            {
                // .NET: PNG.bitblt(decoded, dst, x, y, 1, 1) with a dst of one pixel
                var pixels = decoded.Data.AsSpan(((decoded.Width * y) + x) * 4, 4).ToArray();
                return new { r = pixels[0], g = pixels[1], b = pixels[2], a = pixels[3] };
            };

            Assert.That(pixel(0, 0).r, Is.GreaterThan(128));
            Assert.That(pixel(0, 0).b, Is.LessThan(128));
            Assert.That(pixel(0, 999).r, Is.LessThan(128));
            Assert.That(pixel(0, 999).b, Is.GreaterThan(128));
        }

        [UpstreamTest("SS-091 should work if the main resource hangs")]
        public async Task ShouldWorkIfTheMainResourceHangs(IBrowser browser, string browserName, string mode, Server server, BrowserNewPageOptions newPageOptions)
        {
            It.Skip(mode != "default");
            It.Skip(browserName == "chromium", "https://github.com/microsoft/playwright/issues/9757");

            var page = await browser.NewPageAsync(newPageOptions);
            server.SetRoute("/slow", (req, res) =>
            {
                res.WriteHead(200, new()
                {
                    ["content-length"] = 4096,
                    ["content-type"] = "text/html",
                });
            });
            try
            {
                await Catch(() => page.GotoAsync(server.Prefix + "/slow", new() { Timeout = 1000 }));
                var screenshot = await page.ScreenshotAsync();
                ExpectToMatchSnapshot(screenshot, "hanging-main-resource.png");
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        [UpstreamTest("SS-092 should capture full element when larger than viewport with device scale factor")]
        [Fixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver", Strict = false)]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportWithDeviceScaleFactor(IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 501, Height = 501 }, DeviceScaleFactor = 2.5f });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(@"
      <div style=""height: 14px"">oooo</div>
      <style>
      div.to-screenshot {
        border: 4px solid red;
        box-sizing: border-box;
        width: 600px;
        height: 600px;
        margin-left: 50px;
        background: rgb(0, 100, 200);
      }
      ::-webkit-scrollbar{
        display: none;
      }
      </style>
      <div class=""to-screenshot""></div>
    ");
            var screenshot = await page.Locator("div.to-screenshot").ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "element-larger-than-viewport-dsf.png");
            await context.CloseAsync();
        }

        [UpstreamTest("SS-093 should capture full element when larger than viewport with device scale factor and scale:css")]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportWithDeviceScaleFactorAndScaleCss(IBrowser browser, BrowserNewContextOptions contextOptions)
        {
            var context = await browser.NewContextAsync(new(contextOptions) { ViewportSize = new() { Width = 501, Height = 501 }, DeviceScaleFactor = 2.5f });
            var page = await context.NewPageAsync();
            await page.SetContentAsync(@"
      <div style=""height: 14px"">oooo</div>
      <style>
      div.to-screenshot {
        border: 4px solid red;
        box-sizing: border-box;
        width: 600px;
        height: 600px;
        margin-left: 50px;
        background: rgb(0, 100, 200);
      }
      ::-webkit-scrollbar{
        display: none;
      }
      </style>
      <div class=""to-screenshot""></div>
    ");
            var screenshot = await page.Locator("div.to-screenshot").ScreenshotAsync(new() { Scale = ScreenshotScale.Css });
            ExpectToMatchSnapshot(screenshot, "element-larger-than-viewport-dsf-css-size.png");
            await context.CloseAsync();
        }
    }
}
