/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

///<playwright-file>page-screenshot.spec.ts</playwright-file>

public class PageScreenshotTests : PageTestEx
{
    [PlaywrightTest("page-screenshot.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        byte[] screenshot = await Page.ScreenshotAsync();
        PlaywrightAssert.ToMatchSnapshot("screenshot-sanity.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should clip rect")]
    public async Task ShouldClipRect()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        byte[] screenshot = await Page.ScreenshotAsync(new()
        {
            Clip = new()
            {
                X = 50,
                Y = 100,
                Width = 150,
                Height = 100
            }
        }
        );
        PlaywrightAssert.ToMatchSnapshot("screenshot-clip-rect.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should clip rect with fullPage")]
    public async Task ShouldClipRectWithFullPage()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.EvaluateAsync("() => window.scrollBy(150, 200)");
        byte[] screenshot = await Page.ScreenshotAsync(new()
        {
            FullPage = true,
            Clip = new()
            {
                X = 50,
                Y = 100,
                Width = 150,
                Height = 100,
            }
        });
        PlaywrightAssert.ToMatchSnapshot("screenshot-clip-rect.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should clip elements to the viewport")]
    public async Task ShouldClipElementsToTheViewport()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        byte[] screenshot = await Page.ScreenshotAsync(new()
        {
            Clip = new()
            {
                X = 50,
                Y = 450,
                Width = 1000,
                Height = 100,
            }
        });
        PlaywrightAssert.ToMatchSnapshot("screenshot-offscreen-clip.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should throw on clip outside the viewport")]
    public async Task ShouldThrowOnClipOutsideTheViewport()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.ScreenshotAsync(new()
        {
            Clip = new()
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
    public async Task ShouldRunInParallel()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");

        var tasks = new List<Task<byte[]>>();
        for (int i = 0; i < 3; ++i)
        {
            tasks.Add(Page.ScreenshotAsync(new()
            {
                Clip = new()
                {
                    X = 50 * i,
                    Y = 0,
                    Width = 50,
                    Height = 50
                }
            }));
        }

        await TaskUtils.WhenAll(tasks);
        PlaywrightAssert.ToMatchSnapshot("grid-cell-1.png", tasks[0].Result);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should take fullPage screenshots")]
    public async Task ShouldTakeFullPageScreenshots()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        byte[] screenshot = await Page.ScreenshotAsync(new() { FullPage = true });
        PlaywrightAssert.ToMatchSnapshot("screenshot-grid-fullpage.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should restore viewport after fullPage screenshot")]
    public async Task ShouldRestoreViewportAfterFullPageScreenshot()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.ScreenshotAsync(new() { FullPage = true });

        Assert.AreEqual(500, Page.ViewportSize.Width);
        Assert.AreEqual(500, Page.ViewportSize.Height);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should run in parallel in multiple pages")]
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
                Clip = new()
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
            PlaywrightAssert.ToMatchSnapshot($"grid-cell-{i % 2}.png", screenshotTasks[i].Result);
        }

        var closeTasks = new List<Task>();
        for (int i = 0; i < n; i++)
        {
            closeTasks.Add(pageTasks[i].Result.CloseAsync());
        }

        await TaskUtils.WhenAll(closeTasks);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should allow transparency")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldAllowTransparency()
    {
        await Page.SetViewportSizeAsync(300, 300);
        await Page.SetContentAsync(@"
            <style>
                body { margin: 0 }
                div { width: 300px; height: 100px; }
            </style>
            <div style=""background:black""></div>
            <div style=""background:white""></div>
            <div style=""background:transparent""></div>
        ");
        byte[] screenshot = await Page.ScreenshotAsync(new() { OmitBackground = true });

        PlaywrightAssert.ToMatchSnapshot("transparent.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should render white background on jpeg file")]
    public async Task ShouldRenderWhiteBackgroundOnJpegFile()
    {
        await Page.SetViewportSizeAsync(100, 100);
        await Page.GotoAsync(Server.EmptyPage);
        byte[] screenshot = await Page.ScreenshotAsync(new()
        {
            OmitBackground = true,
            Type = ScreenshotType.Jpeg,
        });
        PlaywrightAssert.ToMatchSnapshot("white.jpg", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work with odd clip size on Retina displays")]
    public async Task ShouldWorkWithOddClipSizeOnRetinaDisplays()
    {
        byte[] screenshot = await Page.ScreenshotAsync(new()
        {
            Clip = new()
            {
                X = 0,
                Y = 0,
                Width = 11,
                Height = 11
            }
        });

        PlaywrightAssert.ToMatchSnapshot("screenshot-clip-odd-size.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithAMobileViewport()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 320,
                Height = 480,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/overflow.html");
        byte[] screenshot = await page.ScreenshotAsync();

        PlaywrightAssert.ToMatchSnapshot("screenshot-mobile.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport and clip")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithAMobileViewportAndClip()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
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
            Clip = new()
            {
                X = 10,
                Y = 10,
                Width = 100,
                Height = 150
            }
        });

        PlaywrightAssert.ToMatchSnapshot("screenshot-mobile-clip.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work with a mobile viewport and fullPage")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithAMobileViewportAndFullPage()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 320,
                Height = 480,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/overflow-large.html");
        byte[] screenshot = await page.ScreenshotAsync(new() { FullPage = true });

        PlaywrightAssert.ToMatchSnapshot("screenshot-mobile-fullpage.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work for canvas")]
    public async Task ShouldWorkForCanvas()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/screenshots/canvas.html");
        byte[] screenshot = await Page.ScreenshotAsync();

        PlaywrightAssert.ToMatchSnapshot("screenshot-canvas.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work for translateZ")]
    public async Task ShouldWorkForTranslateZ()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/screenshots/translateZ.html");
        byte[] screenshot = await Page.ScreenshotAsync();

        PlaywrightAssert.ToMatchSnapshot("screenshot-translateZ.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work while navigating")]
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
    public async Task ShouldWorkWithDeviceScaleFactor()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 320,
                Height = 480,
            },
            DeviceScaleFactor = 2,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/grid.html");
        byte[] screenshot = await page.ScreenshotAsync();

        PlaywrightAssert.ToMatchSnapshot("screenshot-device-scale-factor.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should work with iframe in shadow")]
    public async Task ShouldWorkWithiFrameInShadow()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 500,
                Height = 500,
            },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/grid-iframe-in-shadow.html");
        byte[] screenshot = await page.ScreenshotAsync();

        PlaywrightAssert.ToMatchSnapshot("screenshot-iframe.png", screenshot);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "path option should work")]
    public async Task PathOptionShouldWork()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        using var tmpDir = new TempDirectory();
        string outputPath = Path.Combine(tmpDir.Path, "screenshot.png");
        await Page.ScreenshotAsync(new() { Path = outputPath });

        PlaywrightAssert.ToMatchSnapshot("screenshot-sanity.png", outputPath);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "path option should create subdirectories")]
    public async Task PathOptionShouldCreateSubdirectories()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        using var tmpDir = new TempDirectory();
        string outputPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "screenshot.png");
        await Page.ScreenshotAsync(new() { Path = outputPath });

        PlaywrightAssert.ToMatchSnapshot("screenshot-sanity.png", outputPath);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "path option should detect joeg")]
    public async Task PathOptionShouldDetectJpeg()
    {
        await Page.SetViewportSizeAsync(100, 100);
        await Page.GotoAsync(Server.EmptyPage);
        using var tmpDir = new TempDirectory();
        string outputPath = Path.Combine(tmpDir.Path, "screenshot.jpg");
        await Page.ScreenshotAsync(new() { Path = outputPath, OmitBackground = true });

        PlaywrightAssert.ToMatchSnapshot("white.jpg", outputPath);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "path option should throw for unsupported mime type")]
    public async Task PathOptionShouldThrowForUnsupportedMimeType()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<ArgumentException>(() => Page.ScreenshotAsync(new() { Path = "file.txt" }));
        StringAssert.Contains("path: unsupported mime type \"text/plain\"", exception.Message);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should hide elements based on attr")]
    public async Task ShouldHideElementsBasedOnAttr()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.Locator("div").Nth(5).EvaluateAsync("element => element.setAttribute('data-test-screenshot', 'hide')");
        var screenshot = await Page.ScreenshotAsync(new() { Style = @"[data-test-screenshot=""hide""] {
            visibility: hidden;
        }" });
        PlaywrightAssert.ToMatchSnapshot("hide-should-work.png", screenshot);
        var visibility = await Page.Locator("div").Nth(5).EvaluateAsync<string>("element => element.style.visibility");
        Assert.AreEqual("", visibility);
    }

    [PlaywrightTest("page-screenshot.spec.ts", "should remove elements based on attr")]
    public async Task ShouldRemoveElementsBasedOnAttr()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.Locator("div").Nth(5).EvaluateAsync("element => element.setAttribute('data-test-screenshot', 'remove')");
        var screenshot = await Page.ScreenshotAsync(new() { Style = @"[data-test-screenshot=""remove""] {
            visibility: none;
        }" });
        PlaywrightAssert.ToMatchSnapshot("remove-should-work.png", screenshot);
        var display = await Page.Locator("div").Nth(5).EvaluateAsync<string>("element => element.style.display");
        Assert.AreEqual("", display);
    }
}
