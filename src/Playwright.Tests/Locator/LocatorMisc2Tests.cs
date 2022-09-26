/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

namespace Microsoft.Playwright.Tests.Locator;

public class LocatorMisc2Tests : PageTestEx
{

    [PlaywrightTest("locator-misc-2.spec.ts", "should press")]
    public async Task ShouldPress()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.Locator("input").PressAsync("h");
        Assert.AreEqual("h", await Page.EvaluateAsync<string>("input => input.value", await Page.QuerySelectorAsync("input")));
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should scroll into view")]
    public async Task ShouldScrollIntoView()
    {
        await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");

        for (int i = 0; i < 11; ++i)
        {
            var button = Page.Locator($"#btn{i}");
            var before = await button.EvaluateAsync<int>("button => { return button.getBoundingClientRect().right - window.innerWidth; }");
            Assert.AreEqual(10 * i, before);

            await button.ScrollIntoViewIfNeededAsync();

            var after = await button.EvaluateAsync<int>("button => { return button.getBoundingClientRect().right - window.innerWidth; }");
            Assert.IsTrue(after <= 0);
            await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should scroll zero-sized element into view")]
    public async Task ShouldScrollZeroSizedElementIntoView()
    {
        await Page.SetContentAsync(@"
            <style>
            html,body { margin: 0; padding: 0; }
            ::-webkit-scrollbar { display: none; }
            * { scrollbar-width: none; }
            </style>
            <div style=""height: 2000px; text-align: center; border: 10px solid blue;"">
            <h1>SCROLL DOWN</h1>
            </div>
            <div id=lazyload style=""font-size:75px; background-color: green;""></div>
            <script>
            const lazyLoadElement = document.querySelector('#lazyload');
            const observer = new IntersectionObserver((entries) => {
                if (entries.some(entry => entry.isIntersecting)) {
                lazyLoadElement.textContent = 'LAZY LOADED CONTENT';
                lazyLoadElement.style.height = '20px';
                observer.disconnect();
                }
            });
            observer.observe(lazyLoadElement);
            </script>");
        var bb = await Page.Locator("#lazyload").BoundingBoxAsync();
        Assert.AreEqual(0, bb.X);
        Assert.AreEqual(2020, bb.Y);
        Assert.AreEqual(1280, bb.Width);
        Assert.AreEqual(0, bb.Height);
        await Page.Locator("#lazyload").ScrollIntoViewIfNeededAsync();
        await Expect(Page.Locator("#lazyload")).ToHaveTextAsync("LAZY LOADED CONTENT");
        bb = await Page.Locator("#lazyload").BoundingBoxAsync();
        Assert.AreEqual(0, bb.X);
        Assert.AreEqual(720, bb.Y);
        Assert.AreEqual(1280, bb.Width);
        Assert.AreEqual(20, bb.Height);
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should select textarea")]
    public async Task ShouldSelectTextarea()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");

        var textarea = Page.Locator("textarea");
        await textarea.EvaluateAsync<string>("textarea => textarea.value = 'some value'");

        await textarea.SelectTextAsync();
        if (TestConstants.IsFirefox)
        {
            Assert.AreEqual(0, await textarea.EvaluateAsync<int>("el => el.selectionStart"));
            Assert.AreEqual(10, await textarea.EvaluateAsync<int>("el => el.selectionEnd"));
        }
        else
        {
            Assert.AreEqual("some value", await textarea.EvaluateAsync<string>("() => window.getSelection().toString()"));
        }
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should type")]
    public async Task ShouldType()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.Locator("input").TypeAsync("hello");
        Assert.AreEqual("hello", await Page.EvalOnSelectorAsync<string>("input", "input => input.value"));
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should take screenshot")]
    public async Task ShouldTakeScreenshot()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");

        await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var element = Page.Locator(".box:nth-of-type(3)");
        await element.ScreenshotAsync();
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should take screenshot with mask")]
    public async Task ShouldTakeScreenshotWithMaskOption()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");

        await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var element = Page.Locator("body");
        await element.ScreenshotAsync(new()
        {
            Mask = new ILocator[] { Page.Locator(".box").Nth(3) },
        });
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should return bounding box")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldReturnBoundingBox()
    {
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");

        var element = Page.Locator(".box:nth-of-type(13)");
        var box = await element.BoundingBoxAsync();

        Assert.AreEqual(100, box.X);
        Assert.AreEqual(50, box.Y);
        Assert.AreEqual(50, box.Width);
        Assert.AreEqual(50, box.Height);
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should waitFor")]
    public async Task ShouldWaitFor()
    {
        await Page.SetContentAsync("<div></div>");
        var locator = Page.Locator("span");
        var task = locator.WaitForAsync();
        await Page.EvalOnSelectorAsync("div", "div => div.innerHTML = '<span>target</span>'");
        await task;
        await Expect(locator).ToHaveTextAsync("target");
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should waitFor hidden")]
    public async Task ShouldWaitForHidden()
    {
        await Page.SetContentAsync("<div><span></span></div>");
        var locator = Page.Locator("span");
        var task = locator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.EvalOnSelectorAsync("div", "div => div.innerHTML = ''");
        await task;
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "should combine visible with other selectors")]
    public async Task ShouldCombineVisibleWithOtherSelectors()
    {
        await Page.SetContentAsync("<div><div class=\"item\" style=\"display: none\">Hidden data0</div><div class=\"item\">visible data1</div><div class=\"item\" style=\"display: none\">Hidden data1</div><div class=\"item\">visible data2</div><div class=\"item\" style=\"display: none\">Hidden data1</div><div class=\"item\">visible data3</div></div>");
        var locator = Page.Locator(".item >> visible=true").Nth(1);
        await Expect(locator).ToHaveTextAsync("visible data2");
        await Expect(Page.Locator(".item >> visible=true >> text=data3")).ToHaveTextAsync("visible data3");
    }

    [PlaywrightTest("locator-misc-2.spec.ts", "locator.count should work with deleted Map in main world")]
    public async Task LocatorCountShouldWorkWithDeletedMapInMainWorld()
    {
        await Page.EvaluateAsync("Map = 1");
        await Page.Locator("#searchResultTableDiv .x-grid3-row").CountAsync();
        await Expect(Page.Locator("#searchResultTableDiv .x-grid3-row")).ToHaveCountAsync(0);
    }
}
