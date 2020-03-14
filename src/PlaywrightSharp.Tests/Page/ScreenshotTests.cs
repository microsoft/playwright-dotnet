using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>screenshot.spec.js</playwright-file>
    ///<playwright-describe>Page.screenshot</playwright-describe>
    public class ScreenshotTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ScreenshotTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>screenshot.spec.js</playwright-file>
        ///<playwright-describe>Page.screenshot</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.SetViewportAsync(new Viewport
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
        ///<playwright-it>should clicp rect</playwright-it>
        [Fact]
        public async Task ShouldClipRect()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                Clip = new Clip
                {
                    X = 50,
                    Y = 100,
                    Width = 150,
                    Height = 100
                }
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-clip-rect.png", screenshot));
        }

        [Fact]
        public async Task ShouldClipElementsToTheViewport()
        {
            await Page.SetViewportAsync(new Viewport { Width = 500, Height = 500 });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                Clip = new Clip
                {

                    X = 50,
                    Y = 600,
                    Width = 100,
                    Height = 100
                }
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-offscreen-clip.png", screenshot));
        }

        [Fact]
        public async Task ShouldRunInParallel()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");

            var tasks = new List<Task<byte[]>>();
            for (var i = 0; i < 3; ++i)
            {
                tasks.Add(Page.ScreenshotAsync(new ScreenshotOptions
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

            await Task.WhenAll(tasks);
            Assert.True(ScreenshotHelper.PixelMatch("grid-cell-1.png", tasks[0].Result));
        }

        [Fact]
        public async Task ShouldTakeFullPageScreenshots()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                FullPage = true
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-grid-fullPage.png", screenshot));
        }

        [Fact]
        public async Task ShouldRunInParallelInMultiplePages()
        {
            var n = 2;
            var PageTasks = new List<Task<Page>>();
            for (var i = 0; i < n; i++)
            {
                async Task<Page> func()
                {
                    var Page = await Context.NewPageAsync();
                    await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
                    return Page;
                }

                PageTasks.Add(func());
            }

            await Task.WhenAll(PageTasks);

            var screenshotTasks = new List<Task<byte[]>>();
            for (var i = 0; i < n; i++)
            {
                screenshotTasks.Add(PageTasks[i].Result.ScreenshotAsync(new ScreenshotOptions
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

            await Task.WhenAll(screenshotTasks);

            for (var i = 0; i < n; i++)
            {
                Assert.True(ScreenshotHelper.PixelMatch($"grid-cell-{i}.png", screenshotTasks[i].Result));
            }

            var closeTasks = new List<Task>();
            for (var i = 0; i < n; i++)
            {
                closeTasks.Add(PageTasks[i].Result.CloseAsync());
            }

            await Task.WhenAll(closeTasks);
        }

        [Fact]
        public async Task ShouldAllowTransparency()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 100,
                Height = 100
            });
            await Page.GoToAsync(TestConstants.EmptyPage);
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                OmitBackground = true
            });

            Assert.True(ScreenshotHelper.PixelMatch("transparent.png", screenshot));
        }

        [Fact]
        public async Task ShouldRenderWhiteBackgroundOnJpegFile()
        {
            await Page.SetViewportAsync(new Viewport { Width = 100, Height = 100 });
            await Page.GoToAsync(TestConstants.EmptyPage);
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                OmitBackground = true,
                Type = ScreenshotType.Jpeg
            });
            Assert.True(ScreenshotHelper.PixelMatch("white.jpg", screenshot));
        }

        [Fact]
        public async Task ShouldWorkWithOddClipSizeOnRetinaDisplays()
        {
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
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

        [Fact]
        public async Task ShouldReturnBase64()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var screenshot = await Page.ScreenshotBase64Async();

            Assert.True(ScreenshotHelper.PixelMatch("screenshot-sanity.png", Convert.FromBase64String(screenshot)));
        }

        [Fact]
        public void ShouldInferScreenshotTypeFromName()
        {
            Assert.Equal(ScreenshotType.Jpeg, ScreenshotOptions.GetScreenshotTypeFromFile("Test.jpg"));
            Assert.Equal(ScreenshotType.Jpeg, ScreenshotOptions.GetScreenshotTypeFromFile("Test.jpe"));
            Assert.Equal(ScreenshotType.Jpeg, ScreenshotOptions.GetScreenshotTypeFromFile("Test.jpeg"));
            Assert.Equal(ScreenshotType.Png, ScreenshotOptions.GetScreenshotTypeFromFile("Test.png"));
            Assert.Null(ScreenshotOptions.GetScreenshotTypeFromFile("Test.exe"));
        }

        [Fact]
        public async Task ShouldWorkWithQuality()
        {
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var screenshot = await Page.ScreenshotAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Jpeg,
                FullPage = true,
                Quality = 100
            });
            Assert.True(ScreenshotHelper.PixelMatch("screenshot-grid-fullPage.png", screenshot));
        }
    }
}
