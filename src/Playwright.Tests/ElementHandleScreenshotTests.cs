/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using SixLabors.ImageSharp;

namespace Microsoft.Playwright.Tests;

///<playwright-file>elementhandle-screenshot.spec.ts</playwright-file>
public class ElementHandleScreenshotTests : PageTestEx
{
    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.EvaluateAsync("window.scrollBy(50, 100)");
        var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take into account padding and border")]
    public async Task ShouldTakeIntoAccountPaddingAndBorder()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync(@"
                <div style=""height: 14px"">oooo</div>
                <style>div {
                    border: 2px solid blue;
                    background: green;
                    width: 50px;
                    height: 50px;
                }
                </style>
                <div id=""d""></div>");
        var elementHandle = await Page.QuerySelectorAsync("div#d");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-padding-border.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should capture full element when larger than viewport in parallel")]
    public async Task ShouldCaptureFullElementWhenLargerThanViewportInParallel()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync(@"
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
        var elementHandles = await Page.QuerySelectorAllAsync("div.to-screenshot");
        var screenshotTasks = elementHandles.Select(e => e.ScreenshotAsync()).ToArray();
        await TaskUtils.WhenAll(screenshotTasks);

        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-viewport.png", screenshotTasks.ElementAt(2).Result));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should capture full element when larger than viewport")]
    public async Task ShouldCaptureFullElementWhenLargerThanViewport()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync(@"
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
                <div class=""to-screenshot""></div>");

        var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-larger-than-viewport.png", screenshot));
        await TestUtils.VerifyViewportAsync(Page, 500, 500);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should scroll element into view")]
    public async Task ShouldScrollElementIntoView()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync(@"
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
                <div class=""to-screenshot""></div>");
        var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-scrolled-into-view.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should scroll 15000px into view")]
    public async Task ShouldScroll15000pxIntoView()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync(@"
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
                <div class=""to-screenshot""></div>");
        var elementHandle = await Page.QuerySelectorAsync("div.to-screenshot");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-scrolled-into-view.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work with a rotated element")]
    public async Task ShouldWorkWithARotatedElement()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync(@"
                <div style='position: absolute;
                top: 100px;
                left: 100px;
                width: 100px;
                height: 100px;
                background: green;
                transform: rotateZ(200deg); '>&nbsp;</div>
            ");
        var elementHandle = await Page.QuerySelectorAsync("div");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-rotate.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should fail to screenshot a detached element")]
    public async Task ShouldFailToScreenshotADetachedElement()
    {
        await Page.SetContentAsync("<h1>remove this</h1>");
        var elementHandle = await Page.QuerySelectorAsync("h1");
        await Page.EvaluateAsync("element => element.remove()", elementHandle);

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => elementHandle.ScreenshotAsync());
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should timeout waiting for visible")]
    public async Task ShouldTimeoutWaitingForVisible()
    {
        await Page.SetContentAsync(@"<div style='width: 50px; height: 0'></div>");
        var elementHandle = await Page.QuerySelectorAsync("div");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => elementHandle.ScreenshotAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
        StringAssert.Contains("element is not visible", exception.Message);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should wait for visible")]
    public async Task ShouldWaitForVisible()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
        await elementHandle.EvaluateAsync("e => e.style.visibility = 'hidden'");
        var task = elementHandle.ScreenshotAsync();

        for (int i = 0; i < 10; i++)
        {
            await Page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(f))");
        }
        Assert.False(task.IsCompleted);
        await elementHandle.EvaluateAsync("e => e.style.visibility = 'visible'");

        byte[] screenshot = await task;
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work for an element with fractional dimensions")]
    public async Task ShouldWorkForAnElementWithFractionalDimensions()
    {
        await Page.SetContentAsync("<div style=\"width:48.51px;height:19.8px;border:1px solid black;\"></div>");
        var elementHandle = await Page.QuerySelectorAsync("div");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work with a mobile viewport")]
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
        await page.GotoAsync(Server.Prefix + "/grid.html");
        await page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
        byte[] screenshot = await elementHandle.ScreenshotAsync();

        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-mobile.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work with device scale factor")]
    [Skip(SkipAttribute.Targets.Firefox)]
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
        await page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(3)");
        byte[] screenshot = await elementHandle.ScreenshotAsync();

        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-mobile-dsf.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should work for an element with an offset")]
    public async Task ShouldWorkForAnElementWithAnOffset()
    {
        await Page.SetContentAsync("<div style=\"position:absolute; top: 10.3px; left: 20.4px;width:50.3px;height:20.2px;border:1px solid black;\"></div>");
        var elementHandle = await Page.QuerySelectorAsync("div");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-fractional-offset.png", screenshot));
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take screenshots when default viewport is null")]
    public async Task ShouldTakeScreenshotsWhenDefaultViewportIsNull()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = ViewportSize.NoViewport
        });
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<div style='height: 10000px; background: red'></div>");
        var windowSize = await page.EvaluateAsync<ViewportSize>("() => ({ width: window.innerWidth * window.devicePixelRatio, height: window.innerHeight * window.devicePixelRatio })");
        var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");

        byte[] screenshot = await page.ScreenshotAsync();
        Assert.NotNull(screenshot);
        var decoded = Image.Load(screenshot);
        Assert.AreEqual(windowSize.Width, decoded.Width);
        Assert.AreEqual(windowSize.Height, decoded.Height);

        var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
        Assert.AreEqual(sizeBefore.Width, sizeAfter.Width);
        Assert.AreEqual(sizeBefore.Height, sizeAfter.Height);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take fullPage screenshots when default viewport is null")]
    public async Task ShouldTakeFullPageScreenshotsWhenDefaultViewportIsNull()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = ViewportSize.NoViewport
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/grid.html");
        var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");

        byte[] screenshot = await page.ScreenshotAsync(new() { FullPage = true });
        Assert.NotNull(screenshot);

        var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
        Assert.AreEqual(sizeBefore.Width, sizeAfter.Width);
        Assert.AreEqual(sizeBefore.Height, sizeAfter.Height);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should restore default viewport after fullPage screenshot")]
    public async Task ShouldRestoreDefaultViewportAfterFullPageScreenshot()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 456, Height = 789 },
        });
        var page = await context.NewPageAsync();
        await TestUtils.VerifyViewportAsync(page, 456, 789);
        byte[] screenshot = await page.ScreenshotAsync(new() { FullPage = true });
        Assert.NotNull(screenshot);

        await TestUtils.VerifyViewportAsync(page, 456, 789);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take element screenshot when default viewport is null and restore back")]
    public async Task ShouldTakeElementScreenshotWhenDefaultViewportIsNullAndRestoreBack()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = ViewportSize.NoViewport,
        });
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
                <div class=""to-screenshot""></div>");

        var sizeBefore = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
        var elementHandle = await page.QuerySelectorAsync("div.to-screenshot");
        byte[] screenshot = await elementHandle.ScreenshotAsync();
        Assert.NotNull(screenshot);

        var sizeAfter = await page.EvaluateAsync<ViewportSize>("() => ({ width: document.body.offsetWidth, height: document.body.offsetHeight })");
        Assert.AreEqual(sizeBefore.Width, sizeAfter.Width);
        Assert.AreEqual(sizeBefore.Height, sizeAfter.Height);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "should take screenshot of disabled button")]
    public async Task ShouldTakeScreenshotOfDisabledButton()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync("<button disabled>Click me</button>");
        var button = await Page.QuerySelectorAsync("button");
        byte[] screenshot = await button.ScreenshotAsync();
        Assert.NotNull(screenshot);
    }

    [PlaywrightTest("elementhandle-screenshot.spec.ts", "path option should create subdirectories")]
    public async Task PathOptionShouldCreateSubdirectories()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(3)");
        using var tmpDir = new TempDirectory();
        string outputPath = Path.Combine(tmpDir.Path, "these", "are", "directories", "screenshot.png");
        await elementHandle.ScreenshotAsync(new() { Path = outputPath });
        Assert.True(ScreenshotHelper.PixelMatch("screenshot-element-bounding-box.png", outputPath));
    }
}
