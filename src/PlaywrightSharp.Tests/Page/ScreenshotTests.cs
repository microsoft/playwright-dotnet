using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>screenshot.spec.js</playwright-file>
    ///<playwright-describe>Page.screenshot</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should clip rect</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should clip rect with fullPage</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should clip elements to the viewport</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should throw on clip outside the viewport</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should run in parallel</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should take fullPage screenshots</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should restore viewport after fullPage screenshot</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRestoreViewportAfterFullPageScreenshot()
        {
            await Page.SetViewportSizeAsync(new ViewportSize
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.ScreenshotAsync(true);

            Assert.Equal(500, Page.Viewport.Width);
            Assert.Equal(500, Page.Viewport.Height);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should run in parallel in multiple pages</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should allow transparency</playwright-it>
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should render white background on jpeg file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRenderWhiteBackgroundOnJpegFile()
        {
            await Page.SetViewportSizeAsync(new ViewportSize { Width = 100, Height = 100 });
            await Page.GoToAsync(TestConstants.EmptyPage);
            byte[] screenshot = await Page.ScreenshotAsync(
                omitBackground: true,
                type: ScreenshotFormat.Jpeg);
            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with odd clip size on Retina displays</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with a mobile viewport</playwright-it>
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with a mobile viewport and clip</playwright-it>
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with a mobile viewport and fullPage</playwright-it>
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work for canvas</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work for translateZ</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work for webgl</playwright-it>
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work while navigating</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with device scale factor</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
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

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>uld work with iframe in shadow</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
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
    }
}
