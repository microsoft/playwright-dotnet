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

using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests.Locator;


public class LocatorFrameTests : PageTestEx
{
    public async Task RouteIFrame(IPage page)
    {
        await page.RouteAsync("**/empty.html", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Body = "<iframe src=\"/iframe.html\"></iframe>",
                ContentType = "text/html"
            });
        });
        await page.RouteAsync("**/iframe.html", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Body = @"
                    <html>
                    <div>
                        <button>Hello iframe</button>
                        <iframe src=""iframe-2.html""></iframe>
                    </div>
                    <span>1</span>
                    <span>2</span>
                    </html>",
                ContentType = "text/html"
            });
        });
        await page.RouteAsync("**/iframe-2.html", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Body = "<html><button>Hello nested iframe</button></html>",
                ContentType = "text/html"
            });
        });
    }

    [PlaywrightTest("locator-frame.spec.ts", "should work for iframe")]
    public async Task ShouldWorkForIFrame()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.AreEqual(await button.InnerTextAsync(), "Hello iframe");
        await button.ClickAsync();
    }

    [PlaywrightTest("locator-frame.spec.ts", "should work for nested iframe")]
    public async Task ShouldWorkForNestedIFrame()
    {
        await RouteIFrame(Page);
        await Page.GotoAsync(Server.EmptyPage);
        var button = Page.FrameLocator("iframe").FrameLocator("iframe").Locator("button");
        await button.WaitForAsync();
        Assert.AreEqual(await button.InnerTextAsync(), "Hello nested iframe");
        await button.ClickAsync();
    }
}
