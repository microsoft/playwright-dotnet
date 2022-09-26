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

public class BrowserContextTimezoneIdTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await using var browser = await Playwright[TestConstants.BrowserName].LaunchAsync();

        const string func = "() => new Date(1479579154987).toString()";
        await using (var context = await browser.NewContextAsync(new() { TimezoneId = "America/Jamaica" }))
        {
            var page = await context.NewPageAsync();
            string result = await page.EvaluateAsync<string>(func);
            Assert.AreEqual(
                "Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)",
                result);
        }

        await using (var context = await browser.NewContextAsync(new() { TimezoneId = "Pacific/Honolulu" }))
        {
            var page = await context.NewPageAsync();
            Assert.AreEqual(
                "Sat Nov 19 2016 08:12:34 GMT-1000 (Hawaii-Aleutian Standard Time)",
                await page.EvaluateAsync<string>(func));
        }

        await using (var context = await browser.NewContextAsync(new() { TimezoneId = "America/Buenos_Aires" }))
        {
            var page = await context.NewPageAsync();
            Assert.AreEqual(
                "Sat Nov 19 2016 15:12:34 GMT-0300 (Argentina Standard Time)",
                await page.EvaluateAsync<string>(func));
        }

        await using (var context = await browser.NewContextAsync(new() { TimezoneId = "Europe/Berlin" }))
        {
            var page = await context.NewPageAsync();
            Assert.AreEqual(
                "Sat Nov 19 2016 19:12:34 GMT+0100 (Central European Standard Time)",
                await page.EvaluateAsync<string>(func));
        }
    }

    [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should throw for invalid timezone IDs")]
    public async Task ShouldThrowForInvalidTimezoneId()
    {
        await using (var context = await Browser.NewContextAsync(new() { TimezoneId = "Foo/Bar" }))
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => context.NewPageAsync());
            StringAssert.Contains("Invalid timezone ID: Foo/Bar", exception.Message);
        }

        await using (var context = await Browser.NewContextAsync(new() { TimezoneId = "Baz/Qux" }))
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => context.NewPageAsync());
            StringAssert.Contains("Invalid timezone ID: Baz/Qux", exception.Message);
        }
    }

    [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should work for multiple pages sharing same process")]
    public async Task ShouldWorkForMultiplePagesSharingSameProcess()
    {
        await using var context = await Browser.NewContextAsync(new() { TimezoneId = "Europe/Moscow" });

        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);

        await TaskUtils.WhenAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

        await TaskUtils.WhenAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));
    }
}
