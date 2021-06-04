using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>page-screenshot.spec.ts</playwright-file>

    [Parallelizable(ParallelScope.Self)]
    public class PageScreenshotTests : PageTestEx
    {
        [PlaywrightTest("page-screenshot.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should clip rect")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldClipRect()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync(new()
            {
                Clip = new Clip
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100
                }
            }
            );
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-rect.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should clip rect with fullPage")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldClipRectWithFullPage()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            await Page.EvaluateAsync("() => window.scrollBy(150, 200)");
            byte[] screenshot = await Page.ScreenshotAsync(new()
            {
                FullPage = true,
                Clip = new Clip
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100,
                }
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-rect.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should clip elements to the viewport")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldClipElementsToTheViewport()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync(new()
            {
                Clip = new Clip
                {
                    X = 50,
                    Y = 450,
                    Width = 1000,
                    Height = 100,
                }
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-offscreen-clip.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should throw on clip outside the viewport")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnClipOutsideTheViewport()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.ScreenshotAsync(new()
            {
                Clip = new Clip
                {
                    X = 50,
                    Y = 650,
                    Width = 100,
                    Height = 100,
                }
            }));

            StringAssert.Contains("Clipped area is either empty or outside the resulting image", exception.Message);
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should run in parallel")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInParallel()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");

            var tasks = new List<Task<byte[]>>();
            for (int i = 0; i < 3; ++i)
            {
                tasks.Add(Page.ScreenshotAsync(new()
                {
                    Clip = new Clip
                    {
                        X = 50 * i,
                        Y = 0,
                        Width = 50,
                        Height = 50
                    }
                }));
            }

            await TaskUtils.WhenAll(tasks);
            Assert.True(ScreenshotHelper.PixelMatch("grid-cell-1.png", tasks[0].Result));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should take fullPage screenshots")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTakeFullPageScreenshots()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            byte[] screenshot = await Page.ScreenshotAsync(new() { FullPage = true });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-grid-fullpage.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should restore viewport after fullPage screenshot")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRestoreViewportAfterFullPageScreenshot()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            await Page.ScreenshotAsync(new() { FullPage = true });

            Assert.AreEqual(500, Page.ViewportSize.Width);
            Assert.AreEqual(500, Page.ViewportSize.Height);
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should run in parallel in multiple pages")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunInParallelInMultiplePages()
        {
            int n = 5;
            var pageTasks = new List<Task<IPage>>();
            for (int i = 0; i < n; i++)
            {
                async Task<IPage> Func()
                {
                    var page = await Context.NewPageAsync();
                    await page.GotoAsync(Server.Prefix + "/grid.html");
                    return page;
                }

                pageTasks.Add(Func());
            }

            await TaskUtils.WhenAll(pageTasks);

            var screenshotTasks = new List<Task<byte[]>>();
            for (int i = 0; i < n; i++)
            {
                screenshotTasks.Add(pageTasks[i].Result.ScreenshotAsync(new()
                {
                    Clip = new Clip
                    {
                        X = 50 * (i % 2),
                        Y = 0,
                        Width = 50,
                        Height = 50
                    }
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
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldAllowTransparency()
        {
            await Page.SetViewportSizeAsync(50, 150);
            await Page.GotoAsync(Server.EmptyPage);
            byte[] screenshot = await Page.ScreenshotAsync(new() { OmitBackground = true });

            Assert.True(ScreenshotHelper.PixelMatch("transparent.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should render white background on jpeg file")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRenderWhiteBackgroundOnJpegFile()
        {
            await Page.SetViewportSizeAsync(100, 100);
            await Page.GotoAsync(Server.EmptyPage);
            byte[] screenshot = await Page.ScreenshotAsync(new()
            {
                OmitBackground = true,
                Type = ScreenshotType.Jpeg,
            });
            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with odd clip size on Retina displays")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithOddClipSizeOnRetinaDisplays()
        {
            byte[] screenshot = await Page.ScreenshotAsync(new()
            {
                Clip = new Clip
                {
                    X = 0,
                    Y = 0,
                    Width = 11,
                    Height = 11
                }
            });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-odd-size.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewport()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/overflow.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-mobile.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport and clip")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewportAndClip()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/overflow.html");
            byte[] screenshot = await page.ScreenshotAsync(new()
            {
                Clip = new Clip
                {
                    X = 10,
                    Y = 10,
                    Width = 100,
                    Height = 150
                }
            });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-mobile-clip.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport and fullPage")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewportAndFullPage()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                IsMobile = true,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/overflow-large.html");
            byte[] screenshot = await page.ScreenshotAsync(new() { FullPage = true });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-mobile-fullpage.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work for canvas")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForCanvas()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/screenshots/canvas.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-canvas.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work for webgl")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkForWebgl()
        {
            await Page.SetViewportSizeAsync(640, 480);
            await Page.GotoAsync(Server.Prefix + "/screenshots/webgl.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-webgl.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work for translateZ")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForTranslateZ()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/screenshots/translateZ.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-translateZ.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work while navigating")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWhileNavigating()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/redirectloop1.html");

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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDeviceScaleFactor()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize
                {
                    Width = 320,
                    Height = 480,
                },
                DeviceScaleFactor = 2,
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/grid.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-device-scale-factor.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "should work with iframe in shadow")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithiFrameInShadow()
        {
            await using var context = await Browser.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/grid-iframe-in-shadow.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-iframe.png", screenshot));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldWork()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "screenshot.png");
            await Page.ScreenshotAsync(new() { Path = outputPath });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", outputPath));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should create subdirectories")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldCreateSubdirectories()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "screenshot.png");
            await Page.ScreenshotAsync(new() { Path = outputPath });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", outputPath));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should detect joeg")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldDetectJpeg()
        {
            await Page.SetViewportSizeAsync(100, 100);
            await Page.GotoAsync(Server.EmptyPage);
            using var tmpDir = new TempDirectory();
            string outputPath = Path.Combine(tmpDir.Path, "screenshot.jpg");
            await Page.ScreenshotAsync(new() { Path = outputPath, OmitBackground = true });

            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", outputPath));
        }

        [PlaywrightTest("page-screenshot.spec.ts", "path option should throw for unsupported mime type")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PathOptionShouldThrowForUnsupportedMimeType()
        {
            var exception = await PlaywrightAssert.ThrowsAsync<ArgumentException>(() => Page.ScreenshotAsync(new() { Path = "file.txt" }));
            StringAssert.Contains("path: unsupported mime type \"text/plain\"", exception.Message);
        }
    }
}
