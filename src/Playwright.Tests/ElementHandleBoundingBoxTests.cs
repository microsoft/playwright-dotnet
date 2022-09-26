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

namespace Microsoft.Playwright.Tests;

public class ElementHandleBoundingBoxTests : PageTestEx
{
    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        var elementHandle = await Page.QuerySelectorAsync(".box:nth-of-type(13)");
        var box = await elementHandle.BoundingBoxAsync();
        AssertEqual(100, 50, 50, 50, box);
    }

    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should handle nested frames")]
    public async Task ShouldHandleNestedFrames()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        var nestedFrame = Page.Frames.First(frame => frame.Name == "dos");
        var elementHandle = await nestedFrame.QuerySelectorAsync("div");
        var box = await elementHandle.BoundingBoxAsync();
        AssertEqual(24, 224, 268, 18, box);
    }

    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should return null for invisible elements")]
    public async Task ShouldReturnNullForInvisibleElements()
    {
        await Page.SetContentAsync("<div style=\"display:none\">hi</div>");
        var element = await Page.QuerySelectorAsync("div");
        Assert.Null(await element.BoundingBoxAsync());
    }

    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should force a layout")]
    public async Task ShouldForceALayout()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync("<div style=\"width: 100px; height: 100px\">hello</div>");
        var elementHandle = await Page.QuerySelectorAsync("div");
        await Page.EvaluateAsync("element => element.style.height = '200px'", elementHandle);
        var box = await elementHandle.BoundingBoxAsync();
        AssertEqual(8, 8, 100, 200, box);
    }

    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work with SVG nodes")]
    public async Task ShouldWorkWithSVGNodes()
    {
        await Page.SetContentAsync(@"
                  <svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
                    <rect id=""theRect"" x=""30"" y=""50"" width=""200"" height=""300""></rect>
                  </svg>");
        var element = await Page.QuerySelectorAsync("#therect");
        var pwBoundingBox = await element.BoundingBoxAsync();
        var webBoundingBox = await Page.EvaluateAsync<ElementHandleBoundingBoxResult>(@"e => {
                    const rect = e.getBoundingClientRect();
                    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height};
                }", element);
        AssertEqual(webBoundingBox, pwBoundingBox);
    }

    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work with page scale")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkWithPageScale()
    {
        var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Height = 400,
                Width = 400,
            },
            IsMobile = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await page.QuerySelectorAsync("button");

        await button.EvaluateAsync(@"button => {
                document.body.style.margin = '0';
                  button.style.borderWidth = '0';
                  button.style.width = '200px';
                  button.style.height = '20px';
                  button.style.marginLeft = '17px';
                  button.style.marginTop = '23px';
            }");

        var box = await button.BoundingBoxAsync();
        Assert.AreEqual(17 * 100, Math.Round(box.X * 100));
        Assert.AreEqual(23 * 100, Math.Round(box.Y * 100));
        Assert.AreEqual(200 * 100, Math.Round(box.Width * 100));
        Assert.AreEqual(20 * 100, Math.Round(box.Height * 100));
    }

    [PlaywrightTest("elementhandle-bounding-box.spec.ts", "should work when inline box child is outside of viewport")]
    public async Task ShouldWorkWhenInlineBoxChildIsOutsideOfViewport()
    {
        await Page.SetContentAsync(@"
                <style>
                i {
                    position: absolute;
                    top: -1000px;
                }
                body {
                    margin: 0;
                    font-size: 12px;
                }
                </style>
                <span><i>woof</i><b>doggo</b></span>");

        var handle = await Page.QuerySelectorAsync("span");
        var box = await handle.BoundingBoxAsync();
        var webBoundingBox = await Page.EvaluateAsync<ElementHandleBoundingBoxResult>(@"e => {
                    const rect = e.getBoundingClientRect();
                    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height};
                }", handle);

        Assert.AreEqual(Math.Round(webBoundingBox.X * 100), Math.Round(box.X * 100));
        Assert.AreEqual(Math.Round(webBoundingBox.Y * 100), Math.Round(box.Y * 100));
        Assert.AreEqual(Math.Round(webBoundingBox.Width * 100), Math.Round(box.Width * 100));
        Assert.AreEqual(Math.Round(webBoundingBox.Height * 100), Math.Round(box.Height * 100));
    }

    private static void AssertEqual(float X, float Y, float Width, float Height, ElementHandleBoundingBoxResult box)
    {
        Assert.AreEqual(X, box.X);
        Assert.AreEqual(Y, box.Y);
        Assert.AreEqual(Width, box.Width);
        Assert.AreEqual(Height, box.Height);
    }

    private static void AssertEqual(ElementHandleBoundingBoxResult boxA, ElementHandleBoundingBoxResult boxB)
    {
        Assert.AreEqual(boxA.X, boxB.X);
        Assert.AreEqual(boxA.Y, boxB.Y);
        Assert.AreEqual(boxA.Width, boxB.Width);
        Assert.AreEqual(boxA.Height, boxB.Height);
    }
}
