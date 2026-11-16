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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/elementhandle-screenshot.spec.ts.</summary>
public class ElementhandleScreenshotTests
{
    public class ElementScreenshot
    {
        [BeforeEach]
        public static void Skip(string browserName, bool headless) => It.Skip(browserName == "firefox" && !headless);

        [BeforeEach]
        public static void Skip2(bool isAndroid) => It.Skip(isAndroid, "Different dpr. Remove after using 1x scale for screenshots.");

        [UpstreamTest("EH-074 should work")]
        public async Task ShouldWork(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-bounding-box.png");
        }

        [UpstreamTest("EH-075 should work with webp")]
        public async Task ShouldWorkWithWebp(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var webpImage = ImageComparator.Decode("image/webp", await elementHandle.ScreenshotAsync(new() { Type = ScreenshotType.Webp }));
            // The default webp screenshot is lossless, so it must decode to the exact same pixels as png.
            var pngImage = ImageComparator.Decode("image/png", await elementHandle.ScreenshotAsync(new() { Type = ScreenshotType.Png }));
            Assert.That(webpImage.Width, Is.EqualTo(pngImage.Width));
            Assert.That(webpImage.Height, Is.EqualTo(pngImage.Height));
            Assert.That(webpImage.Data.SequenceEqual(pngImage.Data), Is.EqualTo(true));
        }

        [UpstreamTest("EH-076 should work when main world busts JSON.stringify")]
        public async Task ShouldWorkWhenMainWorldBustsJSONStringify(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync(@"() => {
      window.scrollBy(50, 100);
      JSON.stringify = () => undefined;
    }");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-bounding-box.png");
        }

        [UpstreamTest("EH-077 should take into account padding and border")]
        public async Task ShouldTakeIntoAccountPaddingAndBorder(IPage page, bool isLinux, bool headless, string browserName)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.SetContentAsync(@"
      <div style=""height: 14px"">oooo</div>
      <style>div {
        border: 2px solid blue;
        background: green;
        width: 50px;
        height: 50px;
      }
      </style>
      <div id=""d""></div>
    ");
            var elementHandle = await page.QuerySelectorAsync("div#d");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-padding-border.png");
        }

        [UpstreamTest("EH-078 should capture full element when larger than viewport in parallel")]
        public async Task ShouldCaptureFullElementWhenLargerThanViewportInParallel(IPage page, string browserName)
        {
            await page.SetViewportSizeAsync(500, 500);

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
            var elementHandles = await page.QuerySelectorAllAsync("div.to-screenshot");
            var promises = elementHandles.Select(handle => handle.ScreenshotAsync()).ToArray();
            var screenshots = await PromiseAll(promises);
            ExpectToMatchSnapshot(screenshots[2], "screenshot-element-larger-than-viewport.png");

            await VerifyViewport(page, 500, 500);
        }

        [UpstreamTest("EH-079 should capture full element when larger than viewport")]
        public async Task ShouldCaptureFullElementWhenLargerThanViewport(IPage page)
        {
            await page.SetViewportSizeAsync(500, 500);

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
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-larger-than-viewport.png");

            await VerifyViewport(page, 500, 500);
        }

        [UpstreamTest("EH-080 should scroll element into view")]
        public async Task ShouldScrollElementIntoView(IPage page)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.SetContentAsync(@"
      <div style=""height: 14px"">oooo</div>
      <style>div.above {
        border: 2px solid blue;
        background: red;
        height: 1500px;
      }
      div.to-screenshot {
        border: 2px solid blue;
        background: green;
        width: 50px;
        height: 50px;
      }
      </style>
      <div class=""above""></div>
      <div class=""to-screenshot""></div>
    ");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-scrolled-into-view.png");
        }

        [UpstreamTest("EH-081 should scroll 15000px into view")]
        public async Task ShouldScroll15000pxIntoView(IPage page)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.SetContentAsync(@"
      <div style=""height: 14px"">oooo</div>
      <style>div.above {
        border: 2px solid blue;
        background: red;
        height: 15000px;
      }
      div.to-screenshot {
        border: 2px solid blue;
        background: green;
        width: 50px;
        height: 50px;
      }
      </style>
      <div class=""above""></div>
      <div class=""to-screenshot""></div>
    ");
            var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-scrolled-into-view.png");
        }

        [UpstreamTest("EH-082 should work with a rotated element")]
        public async Task ShouldWorkWithARotatedElement(IPage page)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.SetContentAsync(@"<div style=""position:absolute;
                                      top: 100px;
                                      left: 100px;
                                      width: 100px;
                                      height: 100px;
                                      background: green;
                                      transform: rotateZ(200deg);"">&nbsp;</div>");
            var elementHandle = await page.QuerySelectorAsync("div");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-rotate.png");
        }

        [UpstreamTest("EH-083 should fail to screenshot a detached element")]
        public async Task ShouldFailToScreenshotADetachedElement(IPage page, Server server)
        {
            await page.SetContentAsync("<h1>remove this</h1>");
            var elementHandle = await page.QuerySelectorAsync("h1");
            await page.EvaluateAsync("element => element.remove()", elementHandle);
            var screenshotError = await Catch(() => elementHandle.ScreenshotAsync());
            Assert.That(screenshotError.Message, Does.Contain("Element is not attached to the DOM"));
        }

        [UpstreamTest("EH-084 should timeout waiting for visible")]
        [Fixme("dotnet: the message is 'Timeout 3000ms exceeded.', without the 'elementHandle.screenshot: ' prefix of the call")]
        public async Task ShouldTimeoutWaitingForVisible(IPage page, Server server)
        {
            await page.SetContentAsync("<div style=\"width: 50px; height: 0\"></div>");
            var div = await page.QuerySelectorAsync("div");
            var error = await Catch(() => div.ScreenshotAsync(new() { Timeout = 3000 }));
            Assert.That(error.Message, Does.Contain("elementHandle.screenshot: Timeout 3000ms exceeded"));
            Assert.That(error.Message, Does.Contain("element is not visible"));
        }

        [UpstreamTest("EH-085 should wait for visible")]
        public async Task ShouldWaitForVisible(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            await elementHandle.EvaluateAsync("e => e.style.visibility = 'hidden'");
            var done = false;
            async Task<byte[]> Then()
            {
                var buffer = await elementHandle.ScreenshotAsync();
                done = true;
                return buffer;
            }
            var promise = Then();
            await Rafraf(page, 10);
            Assert.That(done, Is.EqualTo(false));
            await elementHandle.EvaluateAsync("e => e.style.visibility = 'visible'");
            var screenshot = await promise;
            ExpectToMatchSnapshot(screenshot, "screenshot-element-bounding-box.png");
        }

        [UpstreamTest("EH-086 should work for an element with fractional dimensions")]
        public async Task ShouldWorkForAnElementWithFractionalDimensions(IPage page)
        {
            await page.SetContentAsync("<div style=\"width:48.51px;height:19.8px;border:1px solid black;\"></div>");
            var elementHandle = await page.QuerySelectorAsync("div");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-fractional.png");
        }

        [UpstreamTest("EH-087 should work for an element with an offset")]
        public async Task ShouldWorkForAnElementWithAnOffset(IPage page)
        {
            await page.SetContentAsync("<div style=\"position:absolute; top: 10.3px; left: 20.4px;width:50.3px;height:20.2px;border:1px solid black;\"></div>");
            var elementHandle = await page.QuerySelectorAsync("div");
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-fractional-offset.png");
        }

        [UpstreamTest("EH-088 should wait for element to stop moving")]
        public async Task ShouldWaitForElementToStopMoving(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            await elementHandle.EvaluateAsync("e => e.classList.add('animation')");
            await Rafraf(page);
            var screenshot = await elementHandle.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-element-bounding-box.png");
        }

        [UpstreamTest("EH-089 should take screenshot of disabled button")]
        public async Task ShouldTakeScreenshotOfDisabledButton(IPage page)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.SetContentAsync("<button disabled>Click me</button>");
            var button = await page.QuerySelectorAsync("button");
            var screenshot = await button.ScreenshotAsync();
            Assert.That(screenshot, Is.InstanceOf<byte[]>());
        }

        [UpstreamTest("EH-090 path option should create subdirectories")]
        public async Task PathOptionShouldCreateSubdirectories(IPage page, Server server, TestInfo testInfo)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var outputPath = testInfo.OutputPath(Path.Combine("these", "are", "directories", "screenshot.png"));
            await elementHandle.ScreenshotAsync(new() { Path = outputPath });
            ExpectToMatchSnapshot(await File.ReadAllBytesAsync(outputPath), "screenshot-element-bounding-box.png");
        }

        [UpstreamTest("EH-091 should prefer type over extension")]
        public async Task ShouldPreferTypeOverExtension(IPage page, Server server, TestInfo testInfo)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            var outputPath = testInfo.OutputPath("file.png");
            var buffer = await elementHandle.ScreenshotAsync(new() { Path = outputPath, Type = ScreenshotType.Jpeg });
            Assert.That(new[] { buffer[0], buffer[1], buffer[2] }, Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF }));
        }

        [UpstreamTest("EH-092 should not issue resize event")]
        public async Task ShouldNotIssueResizeEvent(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/grid.html");
            var resizeTriggered = false;
            await page.ExposeFunctionAsync("resize", () =>
            {
                resizeTriggered = true;
            });
            await page.EvaluateAsync(@"() => {
      window.addEventListener('resize', () => window.resize());
    }");
            var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
            await elementHandle.ScreenshotAsync();
            Assert.That(resizeTriggered, Is.False);
        }
    }
}
