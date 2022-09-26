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

public class BrowserContextDeviceTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-device.spec.ts", "should work")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWork()
    {
        await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/mobile.html");
        Assert.AreEqual(375, await page.EvaluateAsync<int>("window.innerWidth"));
        StringAssert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
    }

    [PlaywrightTest("browsercontext-device.spec.ts", "should support clicking")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldSupportClicking()
    {
        await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await page.QuerySelectorAsync("button");
        await button.EvaluateAsync("button => button.style.marginTop = '200px'", button);
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("browsercontext-device.spec.ts", "should scroll to click")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldScrollToClick()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 400,
                Height = 400,
            },
            DeviceScaleFactor = 1,
            IsMobile = true,
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        var element = await page.QuerySelectorAsync("#button-91");
        await element.ClickAsync();
        Assert.AreEqual("clicked", await element.TextContentAsync());
    }
}
