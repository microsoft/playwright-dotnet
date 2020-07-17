using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>screenshot.spec.js</playwright-file>
    ///<playwright-describe>Page.screenshot</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class ScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await page.ScreenshotAsync();
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should clip rect</playwright-it>
        [Retry]
        public async Task ShouldClipRect()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                Clip = new Rect
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100
                }
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-rect.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should clip elements to the viewport</playwright-it>
        [Retry]
        public async Task ShouldClipElementsToTheViewport()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                Clip = new Rect
                {
                    X = 50,
                    Y = 450,
                    Width = 1000,
                    Height = 100,
                }
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-offscreen-clip.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should throw on clip outside the viewport</playwright-it>
        [Retry]
        public async Task ShouldThrowOnClipOutsideTheViewport()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => page.ScreenshotAsync(new ScreenshotOptions
            {
                Clip = new Rect
                {
                    X = 50,
                    Y = 650,
                    Width = 100,
                    Height = 100,
                }
            }));

            Assert.Equal("Clipped area is either empty or outside the viewport", exception.Message);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should run in parallel</playwright-it>
        [Retry]
        public async Task ShouldRunInParallel()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");

            var tasks = new List<Task<byte[]>>();
            for (int i = 0; i < 3; ++i)
            {
                tasks.Add(Page.ScreenshotAsync(new ScreenshotOptions
                {
                    Clip = new Rect
                    {
                        X = 50 * i,
                        Y = 0,
                        Width = 50,
                        Height = 50
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.True(ScreenshotHelper.PixelMatch("grid-cell-1.png", tasks[0].Result));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should take fullPage screenshots</playwright-it>
        [Retry]
        public async Task ShouldTakeFullPageScreenshots()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                FullPage = true
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-grid-fullpage.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should restore viewport after fullPage screenshot</playwright-it>
        [Retry]
        public async Task ShouldRestoreViewportAfterFullPageScreenshot()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Page.ScreenshotAsync(new ScreenshotOptions
            {
                FullPage = true
            });

            Assert.Equal(500, (int)Page.Viewport.Width);
            Assert.Equal(500, (int)Page.Viewport.Height);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should run in parallel in multiple pages</playwright-it>
        [Retry]
        public async Task ShouldRunInParallelInMultiplePages()
        {
            int n = 2;
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

            await Task.WhenAll(pageTasks);

            var screenshotTasks = new List<Task<byte[]>>();
            for (int i = 0; i < n; i++)
            {
                screenshotTasks.Add(pageTasks[i].Result.ScreenshotAsync(new ScreenshotOptions
                {
                    Clip = new Rect
                    {
                        X = 50 * i,
                        Y = 0,
                        Width = 50,
                        Height = 50
                    }
                }));
            }

            await Task.WhenAll(screenshotTasks);

            for (int i = 0; i < n; i++)
            {
                Assert.True(ScreenshotHelper.PixelMatch($"grid-cell-{i}.png", screenshotTasks[i].Result));
            }

            var closeTasks = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                closeTasks.Add(pageTasks[i].Result.CloseAsync());
            }

            await Task.WhenAll(closeTasks);
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should allow transparency</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldAllowTransparency()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 50,
                    Height = 150,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                OmitBackground = true
            });

            Assert.True(ScreenshotHelper.PixelMatch("transparent.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should render white background on jpeg file</playwright-it>
        [Retry]
        public async Task ShouldRenderWhiteBackgroundOnJpegFile()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 100,
                    Height = 100,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            byte[] screenshot = await page.ScreenshotAsync(new ScreenshotOptions
            {
                OmitBackground = true,
                Type = ScreenshotFormat.Jpeg
            });
            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with odd clip size on Retina displays</playwright-it>
        [Retry]
        public async Task ShouldWorkWithOddClipSizeOnRetinaDisplays()
        {
            byte[] screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                Clip = new Rect
                {
                    X = 0,
                    Y = 0,
                    Width = 11,
                    Height = 11
                }
            });

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-odd-size.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should return base64</playwright-it>
        [Retry]
        public async Task ShouldReturnBase64()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            string screenshot = await page.ScreenshotBase64Async();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", Convert.FromBase64String(screenshot)));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work with a mobile viewport</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithAMobileViewport()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
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
        ///<playwright-it>should work for canvas</playwright-it>
        [Retry]
        public async Task ShouldWorkForCanvas()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/screenshots/canvas.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-canvas.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work for translateZ</playwright-it>
        [Retry]
        public async Task ShouldWorkForTranslateZ()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/screenshots/translateZ.html");
            byte[] screenshot = await page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-translateZ.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work for webgl</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkForWebgl()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 640,
                    Height = 480,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/screenshots/webgl.html");
            byte[] screenshot = await Page.ScreenshotAsync();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-webgl.png", screenshot));
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work while navigating</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWhileNavigating()
        {
            await using var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 500,
                    Height = 500,
                },
            });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/redirectloop1.html");

            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    await page.ScreenshotAsync();
                }
                catch (Exception ex) when (ex.Message.Contains("Cannot take a screenshot while page is navigating"))
                {
                }
            }
        }
    }
}
