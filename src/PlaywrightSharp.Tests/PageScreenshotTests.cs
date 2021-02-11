using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>page-screenshot.spec.ts</playwright-file>

    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should clip rect")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClipRect()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync(
                new Rect
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100
                }
            );
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-rect.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should clip rect with fullPage")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClipRectWithFullPage()
        {
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 500, Height = 500 });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.EvaluateAsync("() => window.scrollBy(150, 200)");
            byte[] screenshot = await Page.ScreenshotAsync(
                fullPage: true,
                clip: new Rect
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100,
                });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-rect.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should clip elements to the viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClipElementsToTheViewport()
        {
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 500, Height = 500 });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync(
                new Rect
                {
                    X = 50,
                    Y = 450,
                    Width = 1000,
                    Height = 100,
                });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-offscreen-clip.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should throw on clip outside the viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnClipOutsideTheViewport()
        {
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 500, Height = 500 });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => Page.ScreenshotAsync(
                new Rect
                {
                    X = 50,
                    Y = 650,
                    Width = 100,
                    Height = 100,
                }));

            Assert.Contains("Clipped area is either empty or outside the resulting image", exception.Message);
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should run in parallel")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInParallel()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");

            var tasks = new List<Task<byte[]>>();
            for (int i = 0; i < 3; ++i)
            {
                tasks.Add(Page.ScreenshotAsync(
                    new Rect
                    {
                        X = 50 * i,
                        Y = 0,
                        Width = 50,
                        Height = 50
                    }));
            }

            await TaskUtils.WhenAll(tasks);
            Assert.True(ScreenshotHelper.PixelMatch("grid-cell-1.png", tasks[0].Result));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should take fullPage screenshots")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeFullPageScreenshots()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync(true);
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-grid-fullpage.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should restore viewport after fullPage screenshot")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRestoreViewportAfterFullPageScreenshot()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.ScreenshotAsync(true);

            Assert.Equal(500, Page.ViewportSize.Width);
            Assert.Equal(500, Page.ViewportSize.Height);
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should run in parallel in multiple pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInParallelInMultiplePages()
        {
            int n = 5;
            var pageTasks = new List<Task<IPage>>();
            for (int i = 0; i < n; i++)
            {
                async Task<IPage> Func()
                {
                    var page = await Context.NewPageAsync();
                    await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
                    return page;
                }

                pageTasks.Add(Func());
            }

            await TaskUtils.WhenAll(pageTasks);

            var screenshotTasks = new List<Task<byte[]>>();
            for (int i = 0; i < n; i++)
            {
                screenshotTasks.Add(pageTasks[i].Result.ScreenshotAsync(
                    new Rect
                    {
                        X = 50 * (i % 2),
                        Y = 0,
                        Width = 50,
                        Height = 50
                    }));
            }

            await TaskUtils.WhenAll(screenshotTasks);

            for (int i = 0; i < n; i++)
            {
                Assert.True(ScreenshotHelper.PixelMatch($"grid-cell-{i % 2}.png", screenshotTasks[i].Result));
            }

            var closeTasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                closeTasks.Add(pageTasks[i].Result.CloseAsync());
            }

            await TaskUtils.WhenAll(closeTasks);
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should allow transparency")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldAllowTransparency()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 50,
                Height = 150
            });
            await Page.GoToAsync(TestConstants.EmptyPage);
            byte[] screenshot = await Page.ScreenshotAsync(omitBackground: true);

            Assert.True(ScreenshotHelper.PixelMatch("transparent.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should render white background on jpeg file")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRenderWhiteBackgroundOnJpegFile()
        {
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 100, Height = 100 });
            await Page.GoToAsync(TestConstants.EmptyPage);
            byte[] screenshot = await Page.ScreenshotAsync(
                omitBackground: true,
                type: ScreenshotFormat.Jpeg);
            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with odd clip size on Retina displays")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithOddClipSizeOnRetinaDisplays()
        {
            byte[] screenshot = await Page.ScreenshotAsync(
                new Rect
                {
                    X = 0,
                    Y = 0,
                    Width = 11,
                    Height = 11
                });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-odd-size.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewport()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/overflow.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-mobile.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport and clip")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewportAndClip()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/overflow.html");
            byte[] screenshot = await page.ScreenshotAsync(
                new Rect
                {
                    X = 10,
                    Y = 10,
                    Width = 100,
                    Height = 150
                });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-mobile-clip.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport and fullPage")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewportAndFullPage()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/overflow-large.html");
            byte[] screenshot = await page.ScreenshotAsync(true);

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-mobile-fullpage.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work for canvas")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCanvas()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/screenshots/canvas.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-canvas.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work for webgl")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkForWebgl()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 640,
                Height = 480
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/screenshots/webgl.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-webgl.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work for translateZ")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForTranslateZ()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/screenshots/translateZ.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-translateZ.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work while navigating")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhileNavigating()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/redirectloop1.html");

            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    await Page.ScreenshotAsync();
                }
                catch (Exception ex) when (ex.Message.Contains("Cannot take a screenshot while page is navigating"))
                {
                }
            }
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with device scale factor")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDeviceScaleFactor()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                DeviceScaleFactor = 2,
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-device-scale-factor.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with iframe in shadow")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithiFrameInShadow()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid-iframe-in-shadow.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-iframe.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldWork()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "screenshot.png");
            await Page.ScreenshotAsync(outputPath);

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", outputPath));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should create subdirectories")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldCreateSubdirectories()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "screenshot.png");
            await Page.ScreenshotAsync(outputPath);

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", outputPath));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should detect joeg")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldDetectJpeg()
        {
            await Page.SetViewportSizeAsync(100, 100);
            await Page.GoToAsync(TestConstants.EmptyPage);
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "screenshot.jpg");
            await Page.ScreenshotAsync(outputPath, omitBackground: true);

            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", outputPath));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should throw for unsupported mime type")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldThrowForUnsupportedMimeType()
        {
            var exception = await Assert.ThrowsAnyAsync<ArgumentException>(() => Page.ScreenshotAsync("file.txt"));
            Assert.Contains("path: unsupported mime type \"text/plain\"", exception.Message);
        }
    }
}
