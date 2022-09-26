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

public class BrowserContextCSPTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP meta tag")]
    public async Task ShouldBypassCSPMetatag()
    {
        // Make sure CSP prohibits addScriptTag.
        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/csp.html");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }));
            TestUtils.AssertCSPError(exception.Message);
            Assert.Null(await page.EvaluateAsync("window.__injected"));
        }
        // By-pass CSP and try one more time.
        await using (var context = await Browser.NewContextAsync(new() { BypassCSP = true }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/csp.html");
            await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
            Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));
        }
    }

    [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP header")]
    public async Task ShouldBypassCSPHeader()
    {
        // Make sure CSP prohibits addScriptTag.
        Server.SetCSP("/empty.html", "default-src 'self'");

        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }));
            TestUtils.AssertCSPError(exception.Message);
            Assert.Null(await page.EvaluateAsync("window.__injected"));
        }

        // By-pass CSP and try one more time.
        await using (var context = await Browser.NewContextAsync(new() { BypassCSP = true }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
            Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));
        }
    }

    [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass after cross-process navigation")]
    public async Task ShouldBypassAfterCrossProcessNavigation()
    {
        await using var context = await Browser.NewContextAsync(new() { BypassCSP = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/csp.html");
        await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
        Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));

        await page.GotoAsync(Server.CrossProcessPrefix + "/csp.html");
        await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
        Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));
    }

    [PlaywrightTest("browsercontext-csp.spec.ts", "should bypass CSP in iframes as well")]
    public async Task ShouldBypassCSPInIframesAsWell()
    {
        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            // Make sure CSP prohibits addScriptTag in an iframe.
            var frame = await FrameUtils.AttachFrameAsync(page, "frame1", Server.Prefix + "/csp.html");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => frame.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }));
            TestUtils.AssertCSPError(exception.Message);
            Assert.Null(await frame.EvaluateAsync<int?>("() => window.__injected"));
        }

        // By-pass CSP and try one more time.
        await using (var context = await Browser.NewContextAsync(new() { BypassCSP = true }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            // Make sure CSP prohibits addScriptTag in an iframe.
            var frame = await FrameUtils.AttachFrameAsync(page, "frame1", Server.Prefix + "/csp.html");
            await frame.AddScriptTagAsync(new() { Content = "window.__injected = 42;" }).ContinueWith(_ => Task.CompletedTask);
            Assert.AreEqual(42, await frame.EvaluateAsync<int?>("() => window.__injected"));

        }
    }
}
