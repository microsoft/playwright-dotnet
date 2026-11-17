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

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-screenshot.spec.ts.</summary>
public class PageScreenshotTests
{
    public class PageScreenshot
    {
        [BeforeEach]
        public static void Skip(string browserName, bool headless) => It.Skip(browserName == "firefox" && !headless, "Firefox headed produces a different image.");

        [BeforeEach]
        public static void Skip2(bool isAndroid) => It.Skip(isAndroid, "Different viewport");

        [UpstreamTest("SS-001 should work @smoke")]
        public async Task ShouldWork(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-sanity.png");
        }

        [UpstreamTest("SS-002 should not capture blinking caret by default")]
        [Fixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
        public async Task ShouldNotCaptureBlinkingCaretByDefault(IPage page, Server server, string browserName)
        {
            await page.SetContentAsync($@"
      <!-- Refer to stylesheet from other origin. Accessing this
           stylesheet rules will throw.
      -->
      <link rel=stylesheet href=""{server.CrossProcessPrefix + "/injectedstyle.css"}"">
      <!-- make life harder: define caret color in stylesheet -->
      <style>
        div {{
          caret-color: #000 !important;
        }}
      </style>
      <div contenteditable=""true""></div>
    ");
            var div = page.Locator("div");
            await div.TypeAsync("foo bar");
            var screenshot = await div.ScreenshotAsync();
            for (var i = 0; i < 10; ++i)
            {
                // Caret blinking time is set to 500ms.
                // Try to capture variety of screenshots to make
                // sure we don't capture blinking caret.
                await Task.Delay(150);
                var newScreenshot = await div.ScreenshotAsync();
                Assert.That(newScreenshot.SequenceEqual(screenshot), Is.EqualTo(true));
            }
        }

        [UpstreamTest("SS-003 should capture blinking caret if explicitly asked for")]
        [Fixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
        public async Task ShouldCaptureBlinkingCaretIfExplicitlyAskedFor(IPage page, Server server, string browserName)
        {
            It.Skip(browserName == "firefox", "browser-level screenshot API in firefox does not capture caret");

            await page.SetContentAsync($@"
      <!-- Refer to stylesheet from other origin. Accessing this
           stylesheet rules will throw.
      -->
      <link rel=stylesheet href=""{server.CrossProcessPrefix + "/injectedstyle.css"}"">
      <!-- make life harder: define caret color in stylesheet -->
      <style>
        div {{
          caret-color: #000 !important;
        }}
      </style>
      <div contenteditable=""true""></div>
    ");
            var div = page.Locator("div");
            await div.TypeAsync("foo bar");
            var screenshot = await div.ScreenshotAsync();
            var hasDifferentScreenshots = false;
            for (var i = 0; !hasDifferentScreenshots && i < 10; ++i)
            {
                // Caret blinking time is set to 500ms.
                // Try to capture variety of screenshots to make
                // sure we capture blinking caret.
                await Task.Delay(150);
                var newScreenshot = await div.ScreenshotAsync(new() { Caret = ScreenshotCaret.Initial });
                hasDifferentScreenshots = !newScreenshot.SequenceEqual(screenshot);
            }
            Assert.That(hasDifferentScreenshots, Is.EqualTo(true));
        }

        [UpstreamTest("SS-004 should capture blinking caret in shadow dom")]
        [Fixme("dotnet: Locator.WithElementAsync makes a negative timeout from the default timeout 0, and the call passes only when it ends before the timer of the driver")]
        public async Task ShouldCaptureBlinkingCaretInShadowDom(IPage page, string browserName)
        {
            await page.AddScriptTagAsync(new()
            {
                Content = @"
      class CustomElementContainer extends HTMLElement {
        #shadowRoot;
        constructor() {
          super();
          this.#shadowRoot = this.attachShadow({ mode: 'open' });
          this.#shadowRoot.innerHTML = '<custom-element-input-wrapper><input type=""text""/></custom-element-input-wrapper>';
        }
      }
      class CustomElementInputWrapper extends HTMLElement {
        #shadowRoot;
        constructor() {
          super();
          this.#shadowRoot = this.attachShadow({ mode: 'open' });
          this.#shadowRoot.innerHTML = '<style>:host { all: initial; }</style><slot/>';
        }
      }
      customElements.define('custom-element-input-wrapper', CustomElementInputWrapper);
      customElements.define('custom-element-container', CustomElementContainer);

      const container = document.createElement('custom-element-container');
      document.body.appendChild(container);",
            });

            var input = page.Locator("input");
            // TODO: click fails in webkit
            await input.FocusAsync();

            var screenshot = await input.ScreenshotAsync();
            var hasDifferentScreenshots = false;
            for (var i = 0; !hasDifferentScreenshots && i < 10; ++i)
            {
                // Caret blinking time is set to 500ms.
                // Try to capture variety of screenshots to make
                // sure we capture blinking caret.
                await Task.Delay(150);
                var newScreenshot = await input.ScreenshotAsync(new() { Caret = ScreenshotCaret.Hide });
                hasDifferentScreenshots = !newScreenshot.SequenceEqual(screenshot);
            }
            Assert.That(hasDifferentScreenshots, Is.EqualTo(false));
        }

        [UpstreamTest("SS-005 should clip rect")]
        public async Task ShouldClipRect(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new()
            {
                Clip = new()
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100,
                },
            });
            ExpectToMatchSnapshot(screenshot, "screenshot-clip-rect.png");
        }

        [UpstreamTest("SS-006 should clip rect with fullPage")]
        public async Task ShouldClipRectWithFullPage(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => window.scrollBy(150, 200)");
            var screenshot = await page.ScreenshotAsync(new()
            {
                FullPage = true,
                Clip = new()
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100,
                },
            });
            ExpectToMatchSnapshot(screenshot, "screenshot-clip-rect.png");
        }

        [UpstreamTest("SS-007 should clip elements to the viewport")]
        public async Task ShouldClipElementsToTheViewport(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new()
            {
                Clip = new()
                {
                    X = 50,
                    Y = 450,
                    Width = 1000,
                    Height = 100,
                },
            });
            ExpectToMatchSnapshot(screenshot, "screenshot-offscreen-clip.png");
        }

        [UpstreamTest("SS-008 should throw on clip outside the viewport")]
        public async Task ShouldThrowOnClipOutsideTheViewport(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshotError = await Catch(() => page.ScreenshotAsync(new()
            {
                Clip = new()
                {
                    X = 50,
                    Y = 650,
                    Width = 100,
                    Height = 100,
                },
            }));
            Assert.That(screenshotError.Message, Does.Contain("Clipped area is either empty or outside the resulting image"));
        }

        [UpstreamTest("SS-009 should throw on a negative clip size")]
        public async Task ShouldThrowOnANegativeClipSize(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshotError = await Catch(() => page.ScreenshotAsync(new()
            {
                Clip = new()
                {
                    X = 50,
                    Y = 50,
                    Width = -100,
                    Height = 100,
                },
            }));
            Assert.That(screenshotError.Message, Does.Contain("Expected options.clip.width to be greater than 0"));
        }

        [UpstreamTest("SS-010 should run in parallel")]
        public async Task ShouldRunInParallel(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var promises = new List<Task<byte[]>>();
            for (var i = 0; i < 3; ++i)
            {
                promises.Add(page.ScreenshotAsync(new()
                {
                    Clip = new()
                    {
                        X = 50 * i,
                        Y = 0,
                        Width = 50,
                        Height = 50,
                    },
                }));
            }
            var screenshots = await PromiseAll(promises);
            ExpectToMatchSnapshot(screenshots[1], "grid-cell-1.png");
        }

        [UpstreamTest("SS-011 should take fullPage screenshots")]
        public async Task ShouldTakeFullPageScreenshots(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new()
            {
                FullPage = true,
            });
            ExpectToMatchSnapshot(screenshot, "screenshot-grid-fullpage.png");
        }

        [UpstreamTest("SS-097 should take fullPage screenshots of SVG documents")]
        public async Task ShouldTakeFullPageScreenshotsOfSVGDocuments(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            server.SetRoute("/full-page.svg", (request, response) =>
            {
                response.SetHeader("Content-Type", "image/svg+xml");
                response.End("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"800\" height=\"1000\"><rect width=\"800\" height=\"1000\" fill=\"orange\"/></svg>");
            });
            await page.GotoAsync(server.Prefix + "/full-page.svg");
            // .NET: PNG.sync.read(buffer)
            var svgScreenshot = ImageComparator.Decode("image/png", await page.ScreenshotAsync(new() { FullPage = true }));
            Assert.That(svgScreenshot.Width, Is.EqualTo(800));
            Assert.That(svgScreenshot.Height, Is.EqualTo(1000));
            Assert.That(svgScreenshot.Data[^4..], Is.EqualTo(new byte[] { 255, 165, 0, 255 }));
        }

        [UpstreamTest("SS-012 should take fullPage screenshots and mask elements outside of it")]
        public async Task ShouldTakeFullPageScreenshotsAndMaskElementsOutsideOfIt(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new()
            {
                FullPage = true,
                Mask = new[] { page.Locator(".box").Nth(144) },
            });
            ExpectToMatchSnapshot(screenshot, "screenshot-grid-fullpage-mask-outside-viewport.png");
        }

        [UpstreamTest("SS-013 should restore viewport after fullPage screenshot")]
        public async Task ShouldRestoreViewportAfterFullPageScreenshot(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var screenshot = await page.ScreenshotAsync(new() { FullPage = true });
            Assert.That(screenshot, Is.InstanceOf<byte[]>());
            await VerifyViewport(page, 500, 500);
        }

        [UpstreamTest("SS-014 should allow transparency")]
        public async Task ShouldAllowTransparency(IPage page, string browserName, bool isBidi)
        {
            It.Fixme(browserName == "firefox" || isBidi);

            await page.SetViewportSizeAsync(300, 300);
            await page.SetContentAsync(@"
      <style>
        body { margin: 0 }
        div { width: 300px; height: 100px; }
      </style>
      <div style=""background:black""></div>
      <div style=""background:white""></div>
      <div style=""background:transparent""></div>
    ");
            var screenshot = await page.ScreenshotAsync(new() { OmitBackground = true });
            ExpectToMatchSnapshot(screenshot, "transparent.png");
        }

        [UpstreamTest("SS-015 should render white background on jpeg file")]
        public async Task ShouldRenderWhiteBackgroundOnJpegFile(IPage page, Server server, bool isElectron)
        {
            await page.SetViewportSizeAsync(300, 300);
            await page.GotoAsync(server.EmptyPage);
            var screenshot = await page.ScreenshotAsync(new() { OmitBackground = true, Type = ScreenshotType.Jpeg });
            ExpectToMatchSnapshot(screenshot, "white.jpg");
        }

        [UpstreamTest("SS-016 should produce a valid webp screenshot")]
        public async Task ShouldProduceAValidWebpScreenshot(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(300, 300);
            await page.GotoAsync(server.EmptyPage);
            await page.EvaluateAsync("() => (document.body.style.background = 'rgb(255, 0, 0)')");
            var screenshot = await page.ScreenshotAsync(new() { Type = ScreenshotType.Webp });
            ExpectToMatchSnapshot(screenshot, "red.webp");
        }

        [UpstreamTest("SS-017 path option should detect webp")]
        [Fixme("dotnet: ScreenshotAsync does not detect webp from the path, it throws ArgumentException 'path: unsupported mime type \"application/octet-stream\"'")]
        public async Task PathOptionShouldDetectWebp(IPage page, Server server, TestInfo testInfo)
        {
            await page.SetViewportSizeAsync(300, 300);
            await page.GotoAsync(server.EmptyPage);
            await page.EvaluateAsync("() => (document.body.style.background = 'rgb(255, 0, 0)')");
            var outputPath = testInfo.OutputPath("screenshot.webp");
            var screenshot = await page.ScreenshotAsync(new() { Path = outputPath });
            ExpectToMatchSnapshot(await File.ReadAllBytesAsync(outputPath), "red.webp");
            ExpectToMatchSnapshot(screenshot, "red.webp");
        }

        [UpstreamTest("SS-018 quality option should work for webp")]
        public async Task QualityOptionShouldWorkForWebp(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/grid.html");
            var lowQuality = await page.ScreenshotAsync(new() { Type = ScreenshotType.Webp, Quality = 0 });
            var highQuality = await page.ScreenshotAsync(new() { Type = ScreenshotType.Webp, Quality = 100 });
            Assert.That(lowQuality.Length, Is.LessThan(highQuality.Length));
        }

        [UpstreamTest("SS-019 webp screenshots should be lossless by default")]
        public async Task WebpScreenshotsShouldBeLosslessByDefault(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/grid.html");
            // .NET: utils.isLosslessWebp(buffer)
            Assert.That(Image.Identify(await page.ScreenshotAsync(new() { Type = ScreenshotType.Webp })).Metadata.GetWebpMetadata().FileFormat == WebpFileFormatType.Lossless, Is.EqualTo(true));
            Assert.That(Image.Identify(await page.ScreenshotAsync(new() { Type = ScreenshotType.Webp, Quality = 80 })).Metadata.GetWebpMetadata().FileFormat == WebpFileFormatType.Lossless, Is.EqualTo(false));
        }

        [UpstreamTest("SS-020 should allow transparency with webp")]
        public async Task ShouldAllowTransparencyWithWebp(IPage page, string browserName, bool isBidi)
        {
            It.Skip(isBidi, "transparency is not supported via WebDriver BiDi");
            It.Fixme(browserName == "firefox");

            await page.SetViewportSizeAsync(300, 300);
            await page.SetContentAsync(@"
      <style>
        body { margin: 0 }
        div { width: 300px; height: 100px; }
      </style>
      <div style=""background:black""></div>
      <div style=""background:white""></div>
      <div style=""background:transparent""></div>
    ");
            var screenshot = await page.ScreenshotAsync(new() { OmitBackground = true, Type = ScreenshotType.Webp });
            // .NET: utils.decodeWebp(screenshot)
            var (width, height, data) = ImageComparator.Decode("image/webp", screenshot);
            Assert.That(width, Is.EqualTo(300));
            Assert.That(height, Is.EqualTo(300));
            byte[] pixel(int x, int y) => data[(((y * width) + x) * 4)..((((y * width) + x) * 4) + 4)];
            Assert.That(pixel(150, 50), Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
            Assert.That(pixel(150, 150), Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
            Assert.That(pixel(150, 250)[3], Is.EqualTo(0));
        }

        [UpstreamTest("SS-021 quality option should throw for webp when out of range")]
        public async Task QualityOptionShouldThrowForWebpWhenOutOfRange(IPage page)
        {
            var error = await Catch(() => page.ScreenshotAsync(new() { Type = ScreenshotType.Webp, Quality = 101 }));
            Assert.That(error.Message, Does.Contain("Expected options.quality to be between 0 and 100"));
        }

        [UpstreamTest("SS-022 should work with odd clip size on Retina displays")]
        public async Task ShouldWorkWithOddClipSizeOnRetinaDisplays(IPage page, bool isElectron)
        {
            It.Skip(isElectron, "electron does not set device scale factor to 1");

            var screenshot = await page.ScreenshotAsync(new()
            {
                Clip = new()
                {
                    X = 0,
                    Y = 0,
                    Width = 11,
                    Height = 11,
                },
            });
            ExpectToMatchSnapshot(screenshot, "screenshot-clip-odd-size.png");
        }

        [UpstreamTest("SS-023 should work for canvas")]
        public async Task ShouldWorkForCanvas(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/screenshots/canvas.html");
            var screenshot = await page.ScreenshotAsync();
            // Allow 4 corners to be rendered differently on various platforms/browsers.
            ExpectToMatchSnapshot(screenshot, "screenshot-canvas.png", new() { MaxDiffPixels = 4 });
        }

        [UpstreamTest("SS-024 should capture canvas changes")]
        public async Task ShouldCaptureCanvasChanges(IPage page, bool isElectron)
        {
            It.Skip(isElectron);
            await page.GotoAsync("data:text/html,<canvas></canvas>");
            await page.EvaluateAsync(@"() => {
      const canvas = document.querySelector('canvas');
      canvas.width = 600;
      canvas.height = 600;
    }");

            async Task addLine(int step)
            {
                await page.EvaluateAsync(
                    @"n => {
        const canvas = document.querySelector('canvas');
        const ctx = canvas.getContext('2d');
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(0, n * 100);
        ctx.lineTo(300, n * 100);
        ctx.stroke();
      }",
                    step);
            }

            for (var i = 0; i < 3; i++)
            {
                await addLine(i);
                // With the slight delay WebKit stops reflecting changes in the screenshots on macOS.
                await Task.Delay(100);
                var screenshot = await page.ScreenshotAsync();
                ExpectToMatchSnapshot(screenshot, $"canvas-changes-{i}.png");
            }
        }

        [UpstreamTest("SS-025 should work for webgl")]
        public async Task ShouldWorkForWebgl(IPage page, Server server, string browserName, string platform, bool isElectron)
        {
            It.Skip(browserName == "webkit" && platform == "darwin" && RuntimeInformation.ProcessArchitecture == Architecture.X64, "WebGL is not available on Intel macOS - https://bugs.webkit.org/show_bug.cgi?id=278277");
            It.Skip(isElectron, "different rendering in electron");

            await page.SetViewportSizeAsync(640, 480);
            await page.GotoAsync(server.Prefix + "/screenshots/webgl.html");
            var screenshot = await page.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-webgl.png");
        }

        [UpstreamTest("SS-095 should capture webgl canvas after its drawing buffer is resized")]
        public async Task ShouldCaptureWebglCanvasAfterItsDrawingBufferIsResized(IPage page, string browserName, string platform)
        {
            It.Skip(browserName == "webkit" && platform == "darwin" && RuntimeInformation.ProcessArchitecture == Architecture.X64, "WebGL is not available on Intel macOS - https://bugs.webkit.org/show_bug.cgi?id=278277");

            await page.SetViewportSizeAsync(200, 200);
            await page.SetContentAsync("<body style=\"margin:0\"><div style=\"width:100px;height:100px;background:rgb(0,255,0)\"></div></body>");
            var expected = await page.ScreenshotAsync();

            await page.SetContentAsync(@"<body style=""margin:0""><canvas style=""width:100px;height:100px""></canvas><script>
      const gl = document.querySelector('canvas').getContext('webgl');
      window.draw = size => {
        gl.canvas.width = size;
        gl.canvas.height = size;
        gl.viewport(0, 0, size, size);
        gl.clearColor(0, 1, 0, 1);
        gl.clear(gl.COLOR_BUFFER_BIT);
      };
      draw(100);
    </script></body>");
            await Rafraf(page);
            await page.EvaluateAsync("() => window.draw(50)");
            await Rafraf(page);
            Assert.That(ComparePNGs(await page.ScreenshotAsync(), expected), Is.Null);
        }

        [UpstreamTest("SS-026 should work for translateZ")]
        public async Task ShouldWorkForTranslateZ(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/screenshots/translateZ.html");
            var screenshot = await page.ScreenshotAsync();
            ExpectToMatchSnapshot(screenshot, "screenshot-translateZ.png");
        }

        [UpstreamTest("SS-027 should work while navigating")]
        public async Task ShouldWorkWhileNavigating(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/redirectloop1.html");
            for (var i = 0; i < 10; i++)
            {
                byte[] screenshot;
                try
                {
                    screenshot = await page.ScreenshotAsync(new() { FullPage = true });
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Cannot take a screenshot while page is navigating", StringComparison.Ordinal))
                    {
                        screenshot = Encoding.UTF8.GetBytes("");
                    }
                    else
                    {
                        throw;
                    }
                }
                Assert.That(screenshot, Is.InstanceOf<byte[]>());
            }
        }

        [UpstreamTest("SS-028 should work with iframe in shadow")]
        public async Task ShouldWorkWithIframeInShadow(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid-iframe-in-shadow.html");
            ExpectToMatchSnapshot(await page.ScreenshotAsync(), "screenshot-iframe.png");
        }

        [UpstreamTest("SS-029 path option should work")]
        public async Task PathOptionShouldWork(IPage page, Server server, TestInfo testInfo)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var outputPath = testInfo.OutputPath("screenshot.png");
            await page.ScreenshotAsync(new() { Path = outputPath });
            ExpectToMatchSnapshot(await File.ReadAllBytesAsync(outputPath), "screenshot-sanity.png");
        }

        [UpstreamTest("SS-030 path option should create subdirectories")]
        public async Task PathOptionShouldCreateSubdirectories(IPage page, Server server, TestInfo testInfo)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            var outputPath = testInfo.OutputPath(Path.Combine("these", "are", "directories", "screenshot.png"));
            await page.ScreenshotAsync(new() { Path = outputPath });
            ExpectToMatchSnapshot(await File.ReadAllBytesAsync(outputPath), "screenshot-sanity.png");
        }

        [UpstreamTest("SS-031 path option should detect jpeg")]
        public async Task PathOptionShouldDetectJpeg(IPage page, Server server, bool isElectron, TestInfo testInfo)
        {
            await page.SetViewportSizeAsync(300, 300);
            await page.GotoAsync(server.EmptyPage);
            var outputPath = testInfo.OutputPath("screenshot.jpg");
            var screenshot = await page.ScreenshotAsync(new() { OmitBackground = true, Path = outputPath });
            ExpectToMatchSnapshot(await File.ReadAllBytesAsync(outputPath), "white.jpg");
            ExpectToMatchSnapshot(screenshot, "white.jpg");
            var upperCasePath = testInfo.OutputPath("screenshot.JPG");
            await page.ScreenshotAsync(new() { OmitBackground = true, Path = upperCasePath });
            ExpectToMatchSnapshot(await File.ReadAllBytesAsync(upperCasePath), "white.jpg");
        }

        [UpstreamTest("SS-032 path option should throw for unsupported mime type")]
        public async Task PathOptionShouldThrowForUnsupportedMimeType(IPage page)
        {
            var error = await Catch(() => page.ScreenshotAsync(new() { Path = "file.txt" }));
            Assert.That(error.Message, Does.Contain("path: unsupported mime type \"text/plain\""));
        }

        [UpstreamTest("SS-033 quality option should throw for png")]
        public async Task QualityOptionShouldThrowForPng(IPage page)
        {
            var error = await Catch(() => page.ScreenshotAsync(new() { Quality = 10 }));
            Assert.That(error.Message, Does.Contain("options.quality is unsupported for the png"));
        }

        [UpstreamTest("SS-034 zero quality option should throw for png")]
        public async Task ZeroQualityOptionShouldThrowForPng(IPage page)
        {
            var error = await Catch(() => page.ScreenshotAsync(new() { Quality = 0, Type = ScreenshotType.Png }));
            Assert.That(error.Message, Does.Contain("options.quality is unsupported for the png"));
        }

        [UpstreamTest("SS-035 quality option should work for jpeg")]
        public async Task QualityOptionShouldWorkForJpeg(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/grid.html");
            var zeroQuality = await page.ScreenshotAsync(new() { Type = ScreenshotType.Jpeg, Quality = 0 });
            var highQuality = await page.ScreenshotAsync(new() { Type = ScreenshotType.Jpeg, Quality = 100 });
            Assert.That(zeroQuality.Length, Is.LessThan(highQuality.Length));
        }

        [UpstreamTest("SS-036 should prefer type over extension")]
        public async Task ShouldPreferTypeOverExtension(IPage page, TestInfo testInfo)
        {
            var outputPath = testInfo.OutputPath("file.png");
            var buffer = await page.ScreenshotAsync(new() { Path = outputPath, Type = ScreenshotType.Jpeg });
            Assert.That(new[] { buffer[0], buffer[1], buffer[2] }, Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF }));
        }

        [UpstreamTest("SS-037 should not issue resize event")]
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
            await page.ScreenshotAsync();
            Assert.That(resizeTriggered, Is.False);
        }

        [UpstreamTest("SS-038 should work with Array deleted")]
        public async Task ShouldWorkWithArrayDeleted(IPage page, Server server)
        {
            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            await page.EvaluateAsync("() => delete window.Array");
            var screenshot = await page.ScreenshotAsync(new() { FullPage = true });
            ExpectToMatchSnapshot(screenshot, "screenshot-grid-fullpage.png");
        }

        [UpstreamTest("SS-039 should take fullPage screenshots during navigation")]
        public async Task ShouldTakeFullPageScreenshotsDuringNavigation(IPage page, Server server)
        {
            It.Slow();

            await page.SetViewportSizeAsync(500, 500);
            await page.GotoAsync(server.Prefix + "/grid.html");
            async Task reloadSeveralTimes()
            {
                for (var i = 0; i < 5; ++i)
                {
                    await page.ReloadAsync();
                }
            }
            async Task screenshotSeveralTimes()
            {
                for (var i = 0; i < 5; ++i)
                {
                    await page.ScreenshotAsync(new() { FullPage = true });
                }
            }
            await PromiseAll(
                reloadSeveralTimes(),
                screenshotSeveralTimes());
        }

        [UpstreamTest("SS-096 should not wait for unused font face with failed local() source")]
        public async Task ShouldNotWaitForUnusedFontFaceWithFailedLocalSource(IPage page, Server server, string browserName)
        {
            It.Fixme(browserName == "webkit", "Fixed upstream in https://commits.webkit.org/323883@main");

            await page.GotoAsync(server.EmptyPage);
            await page.SetContentAsync($@"
      <style>
      @font-face {{
        font-family: 'pwtest-iconfont';
        src: local('NoSuchFont'), url('{server.Prefix}/webfont/iconfont.woff2') format('woff2');
        unicode-range: U+0400-04FF;
      }}
      </style>
      <div style=""font-family: 'pwtest-iconfont'"">hello</div>
    ");
            await page.ScreenshotAsync();
            Assert.That(await page.EvaluateAsync<string>("() => document.fonts.status"), Is.EqualTo("loaded"));
        }

        public class MaskOption
        {
            [UpstreamTest("SS-040 should work")]
            public async Task ShouldWork(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                ExpectToMatchSnapshot(
                    await page.ScreenshotAsync(new()
                    {
                        Mask = new[] { page.Locator("div").Nth(5) },
                    }),
                    "mask-should-work.png");
            }

            [UpstreamTest("SS-041 should work with locator")]
            public async Task ShouldWorkWithLocator(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                var bodyLocator = page.Locator("body");
                ExpectToMatchSnapshot(
                    await bodyLocator.ScreenshotAsync(new()
                    {
                        Mask = new[] { page.Locator("div").Nth(5) },
                    }),
                    "mask-should-work-with-locator.png");
            }

            [UpstreamTest("SS-042 should work with elementhandle")]
            public async Task ShouldWorkWithElementhandle(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                var bodyHandle = await page.QuerySelectorAsync("body");
                ExpectToMatchSnapshot(
                    await bodyHandle.ScreenshotAsync(new()
                    {
                        Mask = new[] { page.Locator("div").Nth(5) },
                    }),
                    "mask-should-work-with-elementhandle.png");
            }

            [UpstreamTest("SS-043 should mask multiple elements")]
            public async Task ShouldMaskMultipleElements(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                ExpectToMatchSnapshot(
                    await page.ScreenshotAsync(new()
                    {
                        Mask = new[]
                        {
                            page.Locator("div").Nth(5),
                            page.Locator("div").Nth(12),
                        },
                    }),
                    "should-mask-multiple-elements.png");
            }

            [UpstreamTest("SS-044 should mask inside iframe")]
            public async Task ShouldMaskInsideIframe(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                await AttachFrame(page, "frame1", server.Prefix + "/grid.html");
                await page.AddStyleTagAsync(new() { Content = "iframe { border: none; }" });
                ExpectToMatchSnapshot(
                    await page.ScreenshotAsync(new()
                    {
                        Mask = new[]
                        {
                            page.Locator("div").Nth(5),
                            page.FrameLocator("#frame1").Locator("div").Nth(12),
                        },
                    }),
                    "should-mask-inside-iframe.png");
            }

            [UpstreamTest("SS-045 should mask in parallel")]
            public async Task ShouldMaskInParallel(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await AttachFrame(page, "frame1", server.Prefix + "/grid.html");
                await AttachFrame(page, "frame2", server.Prefix + "/grid.html");
                await page.AddStyleTagAsync(new() { Content = "iframe { border: none; }" });
                var screenshots = await PromiseAll(new[]
                {
                    page.ScreenshotAsync(new()
                    {
                        Mask = new[] { page.FrameLocator("#frame1").Locator("div").Nth(1) },
                    }),
                    page.ScreenshotAsync(new()
                    {
                        Mask = new[] { page.FrameLocator("#frame2").Locator("div").Nth(3) },
                    }),
                });
                ExpectToMatchSnapshot(screenshots[0], "should-mask-in-parallel-1.png");
                ExpectToMatchSnapshot(screenshots[1], "should-mask-in-parallel-2.png");
            }

            [UpstreamTest("SS-046 should remove mask after screenshot")]
            public async Task ShouldRemoveMaskAfterScreenshot(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                var screenshot1 = await page.ScreenshotAsync();
                await page.ScreenshotAsync(new()
                {
                    Mask = new[] { page.Locator("div").Nth(1) },
                });
                var screenshot2 = await page.ScreenshotAsync();
                Assert.That(screenshot1.SequenceEqual(screenshot2), Is.EqualTo(true));
            }

            [UpstreamTest("SS-047 should work when subframe has stalled navigation")]
            public async Task ShouldWorkWhenSubframeHasStalledNavigation(IPage page, Server server)
            {
                var routeReady = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
                await page.RouteAsync("**/subframe.html", route => routeReady.TrySetResult(route)); // Stalling subframe.

                await page.GotoAsync(server.EmptyPage);
                var done = page.SetContentAsync("<iframe src='/subframe.html'></iframe>");
                var route = await routeReady.Task;

                await page.ScreenshotAsync(new() { Mask = new[] { page.Locator("non-existent") } });
                await route.FulfillAsync(new() { Body = "" });
                await done;
            }

            [UpstreamTest("SS-048 should work when subframe used document.open after a weird url")]
            public async Task ShouldWorkWhenSubframeUsedDocumentOpenAfterAWeirdUrl(IPage page, Server server)
            {
                await page.GotoAsync(server.EmptyPage);
                await page.EvaluateAsync(@"() => {
        const iframe = document.createElement('iframe');
        iframe.src = 'javascript:hi';
        document.body.appendChild(iframe);
        iframe.contentDocument.open();
        iframe.contentDocument.write('Hello');
        iframe.contentDocument.close();
      }");
                await page.ScreenshotAsync(new() { Mask = new[] { page.Locator("non-existent") } });
            }

            [UpstreamTest("SS-049 should work when mask color is not pink #F0F")]
            public async Task ShouldWorkWhenMaskColorIsNotPinkF0F(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                ExpectToMatchSnapshot(
                    await page.ScreenshotAsync(new()
                    {
                        Mask = new[] { page.Locator("div").Nth(5) },
                        MaskColor = "#00FF00",
                    }),
                    "mask-color-should-work.png");
            }

            [UpstreamTest("SS-050 should hide elements based on attr")]
            public async Task ShouldHideElementsBasedOnAttr(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                await page.Locator("div").Nth(5).EvaluateAsync(@"element => {
        element.setAttribute('data-test-screenshot', 'hide');
      }");
                ExpectToMatchSnapshot(
                    await page.ScreenshotAsync(new()
                    {
                        Style = @"[data-test-screenshot=""hide""] {
          visibility: hidden;
        }",
                    }),
                    "hide-should-work.png");
                var visibility = await page.Locator("div").Nth(5).EvaluateAsync<string>("element => element.style.visibility");
                Assert.That(visibility, Is.EqualTo(""));
            }

            [UpstreamTest("SS-051 should remove elements based on attr")]
            public async Task ShouldRemoveElementsBasedOnAttr(IPage page, Server server)
            {
                await page.SetViewportSizeAsync(500, 500);
                await page.GotoAsync(server.Prefix + "/grid.html");
                await page.Locator("div").Nth(5).EvaluateAsync(@"element => {
        element.setAttribute('data-test-screenshot', 'remove');
      }");
                ExpectToMatchSnapshot(
                    await page.ScreenshotAsync(new()
                    {
                        Style = @"[data-test-screenshot=""remove""] {
          display: none;
        }",
                    }),
                    "remove-should-work.png");
                var display = await page.Locator("div").Nth(5).EvaluateAsync<string>("element => element.style.display");
                Assert.That(display, Is.EqualTo(""));
            }
        }
    }

    public class PageScreenshotAnimations
    {
        [UpstreamTest("SS-098 should preserve scroll animation progress during screenshots")]
        public async Task ShouldPreserveScrollAnimationProgressDuringScreenshots(IPage page)
        {
            await page.SetContentAsync(@"
      <style>
        @keyframes reveal { from { opacity: 0 } to { opacity: 1 } }
        body { timeline-scope: --progress; }
        #scroller { width: 100px; overflow-x: auto; scroll-timeline: --progress x; }
        #content { width: 200px; height: 20px; }
        #indicator { width: 40px; height: 40px; background: red;
          animation: reveal linear both; animation-timeline: --progress; }
      </style>
      <div id=""scroller""><div id=""content""></div></div>
      <div id=""indicator""></div>
    ");
            await Rafraf(page);
            var expected = await page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Allow });
            var actual = await page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled });
            Assert.That(ComparePNGs(actual, expected), Is.Null);
            Assert.That((await page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Allow })).SequenceEqual(expected), Is.EqualTo(true));
        }

        [UpstreamTest("SS-052 should not capture infinite css animation")]
        public async Task ShouldNotCaptureInfiniteCssAnimation(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/rotate-z.html");
            var div = page.Locator("div");
            var screenshot = await div.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            for (var i = 0; i < 10; ++i)
            {
                await Rafraf(page);
                var newScreenshot = await div.ScreenshotAsync(new()
                {
                    Animations = ScreenshotAnimations.Disabled,
                });
                Assert.That(newScreenshot.SequenceEqual(screenshot), Is.EqualTo(true));
            }
        }

        [UpstreamTest("SS-053 should not capture pseudo element css animation")]
        public async Task ShouldNotCapturePseudoElementCssAnimation(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/rotate-pseudo.html");
            var div = page.Locator("div");
            var screenshot = await div.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            for (var i = 0; i < 10; ++i)
            {
                await Rafraf(page);
                var newScreenshot = await div.ScreenshotAsync(new()
                {
                    Animations = ScreenshotAnimations.Disabled,
                });
                Assert.That(newScreenshot.SequenceEqual(screenshot), Is.EqualTo(true));
            }
        }

        [UpstreamTest("SS-054 should not capture css animations in shadow DOM")]
        public async Task ShouldNotCaptureCssAnimationsInShadowDOM(IPage page, Server server, bool isAndroid)
        {
            It.Skip(isAndroid, "Different viewport");
            await page.GotoAsync(server.Prefix + "/rotate-z-shadow-dom.html");
            var screenshot = await page.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            for (var i = 0; i < 4; ++i)
            {
                await Rafraf(page);
                var newScreenshot = await page.ScreenshotAsync(new()
                {
                    Animations = ScreenshotAnimations.Disabled,
                });
                Assert.That(newScreenshot.SequenceEqual(screenshot), Is.EqualTo(true));
            }
        }

        [UpstreamTest("SS-056 should resume infinite animations")]
        public async Task ShouldResumeInfiniteAnimations(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/rotate-z.html");
            await page.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            var buffer1 = await page.ScreenshotAsync();
            await Rafraf(page);
            var buffer2 = await page.ScreenshotAsync();
            Assert.That(ComparePNGs(buffer1, buffer2, new() { MaxDiffPixels = 50 }), Is.Not.Null);
        }

        [UpstreamTest("SS-057 should not capture infinite web animations")]
        public async Task ShouldNotCaptureInfiniteWebAnimations(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/web-animation.html");
            var div = page.Locator("div");
            var screenshot = await div.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            for (var i = 0; i < 10; ++i)
            {
                await Rafraf(page);
                var newScreenshot = await div.ScreenshotAsync(new()
                {
                    Animations = ScreenshotAnimations.Disabled,
                });
                Assert.That(newScreenshot.SequenceEqual(screenshot), Is.EqualTo(true));
            }
            // Should resume infinite web animation.
            var buffer1 = await page.ScreenshotAsync();
            await Rafraf(page);
            var buffer2 = await page.ScreenshotAsync();
            Assert.That(ComparePNGs(buffer1, buffer2, new() { MaxDiffPixels = 50 }), Is.Not.Null);
        }

        [UpstreamTest("SS-058 should fire transitionend for finite transitions")]
        public async Task ShouldFireTransitionendForFiniteTransitions(IPage page, Server server, string browserName, string platform)
        {
            await page.GotoAsync(server.Prefix + "/css-transition.html");
            var div = page.Locator("div");
            await div.EvaluateAsync(@"el => {
      el.addEventListener('transitionend', () => window['__TRANSITION_END'] = true, false);
    }");

            await It.Step("make sure transition is actually running", async () =>
            {
                // .NET: 'screenshot1' and 'screenshot2' are also locals of the test below: screenshot12 and screenshot22 are the ones of upstream.
                var screenshot12 = await page.ScreenshotAsync();
                await Rafraf(page);
                var screenshot22 = await page.ScreenshotAsync();
                Assert.That(screenshot12.SequenceEqual(screenshot22), Is.EqualTo(false));
            });

            // Make a screenshot that finishes all finite animations.
            var screenshot1 = await div.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            await Rafraf(page);
            // Make sure finite transition is not restarted.
            var screenshot2 = await div.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Allow });
            Assert.That(screenshot1.SequenceEqual(screenshot2), Is.EqualTo(true));

            Assert.That(await page.EvaluateAsync<bool>("() => window['__TRANSITION_END']"), Is.EqualTo(true));
        }

        [UpstreamTest("SS-059 should capture screenshots after layoutchanges in transitionend event")]
        public async Task ShouldCaptureScreenshotsAfterLayoutchangesInTransitionendEvent(IPage page, Server server, string browserName, string platform)
        {
            await page.GotoAsync(server.Prefix + "/css-transition.html");
            var div = page.Locator("div");
            await div.EvaluateAsync(@"el => {
      el.addEventListener('transitionend', () => {
        const time = window.builtins.Date.now();
        // Block main thread for 200ms, emulating heavy layout.
        while (window.builtins.Date.now() - time < 200) {}
        const h1 = document.createElement('h1');
        h1.textContent = 'woof-woof';
        document.body.append(h1);
      }, false);
    }");

            await It.Step("make sure transition is actually running", async () =>
            {
                // .NET: 'screenshot1' and 'screenshot2' are also locals of the test below: screenshot12 and screenshot22 are the ones of upstream.
                var screenshot12 = await page.ScreenshotAsync();
                await Rafraf(page);
                var screenshot22 = await page.ScreenshotAsync();
                Assert.That(screenshot12.SequenceEqual(screenshot22), Is.EqualTo(false));
            });

            // 1. Make a screenshot that finishes all finite animations
            //    and triggers layout.
            var screenshot1 = await page.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });

            // 2. Make a second screenshot after h1 is on screen.
            await Expect(page.Locator("h1")).ToBeVisibleAsync();
            await Expect(page.Locator("h1")).ToHaveTextAsync("woof-woof");
            var screenshot2 = await page.ScreenshotAsync();

            // 3. Make sure both screenshots are equal, meaning that
            //    first screenshot actually was taken after transitionend
            //    changed layout.
            Assert.That(screenshot1.SequenceEqual(screenshot2), Is.EqualTo(true));
        }

        [UpstreamTest("SS-060 should not change animation with playbackRate equal to 0")]
        public async Task ShouldNotChangeAnimationWithPlaybackRateEqualTo0(IPage page, Server server, bool isAndroid)
        {
            It.Skip(isAndroid, "Different viewport");
            await page.GotoAsync(server.Prefix + "/rotate-z.html");
            await page.EvaluateAsync(@"async () => {
      window.animation = document.getAnimations()[0];
      await window.animation.ready;
      window.animation.updatePlaybackRate(0);
      await window.animation.ready;
      window.animation.currentTime = 500;
    }");
            var screenshot1 = await page.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            await Rafraf(page);
            var screenshot2 = await page.ScreenshotAsync(new()
            {
                Animations = ScreenshotAnimations.Disabled,
            });
            Assert.That(screenshot1.SequenceEqual(screenshot2), Is.EqualTo(true));
            ExpectToEqual(
                await page.EvaluateAsync<object>(@"() => ({
      playbackRate: window.animation.playbackRate,
      currentTime: window.animation.currentTime,
    })"),
                new
                {
                    playbackRate = 0,
                    currentTime = 500,
                });
        }

        [UpstreamTest("SS-061 should trigger particular events for css transitions")]
        public async Task ShouldTriggerParticularEventsForCssTransitions(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/css-transition.html");
            var div = page.Locator("div");
            await div.EvaluateAsync(@"async el => {
      window._EVENTS = [];
      el.addEventListener('transitionend', () => {
        window._EVENTS.push('transitionend');
        console.log('transitionend');
      }, false);
      const animation = el.getAnimations()[0];
      animation.oncancel = () => window._EVENTS.push('oncancel');
      animation.onfinish = () => window._EVENTS.push('onfinish');
      animation.onremove = () => window._EVENTS.push('onremove');
      await animation.ready;
    }");
            await PromiseAll(
                page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled }),
                page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "transitionend" }));
            Assert.That(await page.EvaluateAsync<string[]>("() => window._EVENTS"), Is.EqualTo(new[]
            {
                "onfinish", "transitionend",
            }));
        }

        [UpstreamTest("SS-062 should trigger particular events for INfinite css animation")]
        public async Task ShouldTriggerParticularEventsForINfiniteCssAnimation(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/rotate-z.html");
            var div = page.Locator("div");
            await div.EvaluateAsync(@"async el => {
      window._EVENTS = [];
      el.addEventListener('animationcancel', () => {
        window._EVENTS.push('animationcancel');
        console.log('animationcancel');
      }, false);
      const animation = el.getAnimations()[0];
      animation.oncancel = () => window._EVENTS.push('oncancel');
      animation.onfinish = () => window._EVENTS.push('onfinish');
      animation.onremove = () => window._EVENTS.push('onremove');
      await animation.ready;
    }");
            await PromiseAll(
                page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled }),
                page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "animationcancel" }));
            Assert.That(await page.EvaluateAsync<string[]>("() => window._EVENTS"), Is.EqualTo(new[]
            {
                "oncancel", "animationcancel",
            }));
        }

        [UpstreamTest("SS-063 should trigger particular events for finite css animation")]
        public async Task ShouldTriggerParticularEventsForFiniteCssAnimation(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/rotate-z.html");
            var div = page.Locator("div");
            await div.EvaluateAsync(@"async el => {
      window._EVENTS = [];
      // Make CSS animation to be finite.
      el.style.setProperty('animation-iteration-count', '1000');
      el.addEventListener('animationend', () => {
        window._EVENTS.push('animationend');
        console.log('animationend');
      }, false);
      const animation = el.getAnimations()[0];
      animation.oncancel = () => window._EVENTS.push('oncancel');
      animation.onfinish = () => window._EVENTS.push('onfinish');
      animation.onremove = () => window._EVENTS.push('onremove');
      await animation.ready;
    }");
            // Ensure CSS animation is finite.
            Assert.That(await div.EvaluateAsync<bool>("async el => Number.isFinite(el.getAnimations()[0].effect.getComputedTiming().endTime)"), Is.EqualTo(true));
            await PromiseAll(
                page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "animationend" }),
                page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled }));
            Assert.That(await page.EvaluateAsync<string[]>("() => window._EVENTS"), Is.EqualTo(new[]
            {
                "onfinish", "animationend",
            }));
        }

        [UpstreamTest("SS-064 should wait for fonts to load")]
        public async Task ShouldWaitForFontsToLoad(IPage page, Server server, bool isWindows, bool isAndroid)
        {
            It.Fixme(isWindows, "This requires a windows-specific test expectations. https://github.com/microsoft/playwright/issues/12707");
            It.Skip(isAndroid, "Different viewport");
            await page.SetViewportSizeAsync(500, 500);
            var fontRequestPromise = new TaskCompletionSource<(ServerRequest request, ServerResponse response)>(TaskCreationOptions.RunContinuationsAsynchronously);
            // Stall font loading.
            server.SetRoute("/webfont/iconfont.woff2", (request, response) =>
            {
                fontRequestPromise.TrySetResult((request, response));
            });
            await page.GotoAsync(server.Prefix + "/webfont/webfont.html", new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded, // 'load' will not happen if webfont is pending
            });

            // Make sure screenshot times out while webfont is stalled.
            var error = await Catch(() => page.ScreenshotAsync(new() { Timeout = 200, }));
            Assert.That(error.Message, Does.Contain("waiting for fonts to load..."));
            Assert.That(error.Message, Does.Contain("Timeout 200ms exceeded"));

            var fontRequest = await fontRequestPromise.Task;
            server.ServeFile(fontRequest.request, fontRequest.response);
            var iconsScreenshot = await page.ScreenshotAsync();
            ExpectToMatchSnapshot(iconsScreenshot, "screenshot-web-font.png", new()
            {
                MaxDiffPixels = 50,
            });
        }
    }

    [UpstreamTest("SS-065 should throw if screenshot size is too large")]
    public async Task ShouldThrowIfScreenshotSizeIsTooLarge(IPage page, string browserName, bool isMac, bool isBidi)
    {
        var maxSize = browserName == "firefox" && isBidi ? 65535 : 32767;
        {
            await page.SetContentAsync($"<style>body {{margin: 0; padding: 0;}}</style><div style='min-height: {maxSize}px; background: red;'></div>");
            var result = await page.ScreenshotAsync(new() { FullPage = true });
            Assert.That(result, Is.Not.Null);
        }
        {
            await page.SetContentAsync($"<style>body {{margin: 0; padding: 0;}}</style><div style='min-height: {maxSize + 1}px; background: red;'></div>");
            var exception = await Catch(() => page.ScreenshotAsync(new() { FullPage = true }));
            if ((browserName == "firefox" && !isBidi) || (browserName == "webkit" && !isMac))
            {
                Assert.That(exception.Message, Does.Contain("Cannot take screenshot larger than 32767"));
            }
            else if (browserName == "firefox" && isBidi)
            {
                Assert.That(exception.Message, Does.Contain("65536 exceeds the maximum allowed screenshot height of 65535 pixels"));
            }
        }
    }
}
