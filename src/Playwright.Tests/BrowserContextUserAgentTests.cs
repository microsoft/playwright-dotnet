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

public class BrowserContextUserAgentTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-user-agent.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            StringAssert.Contains("Mozilla", await page.EvaluateAsync<string>("() => navigator.userAgent"));
        }

        await using (var context = await Browser.NewContextAsync(new() { UserAgent = "foobar" }))
        {
            var page = await context.NewPageAsync();

            var (userAgent, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString()),
                page.GotoAsync(Server.EmptyPage)
            );
            Assert.AreEqual("foobar", userAgent);
        }
    }

    [PlaywrightTest("browsercontext-user-agent.spec.ts", "should work for subframes")]
    public async Task ShouldWorkForSubframes()
    {
        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            StringAssert.Contains("Mozilla", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        await using (var context = await Browser.NewContextAsync(new() { UserAgent = "foobar" }))
        {
            var page = await context.NewPageAsync();

            var (userAgent, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest<string>("/empty.html", (request) => request.Headers["user-agent"]),
                FrameUtils.AttachFrameAsync(page, "frame1", Server.EmptyPage));

            Assert.AreEqual("foobar", userAgent);
        }
    }

    [PlaywrightTest("browsercontext-user-agent.spec.ts", "should emulate device user-agent")]
    public async Task ShouldEmulateDeviceUserAgent()
    {
        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/mobile.html");
            CollectionAssert.DoesNotContain("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        await using (var context = await Browser.NewContextAsync(new() { UserAgent = "iPhone" }))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/mobile.html");
            StringAssert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }
    }

    [PlaywrightTest("browsercontext-user-agent.spec.ts", "should make a copy of default options")]
    public async Task ShouldMakeACopyOfDefaultOptions()
    {
        var options = new BrowserNewContextOptions()
        {
            UserAgent = "foobar"
        };

        await using var context = await Browser.NewContextAsync(options);
        options.UserAgent = "wrong";
        var page = await context.NewPageAsync();

        var (userAgent, _) = await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", request => request.Headers["User-Agent"].ToString()),
            page.GotoAsync(Server.EmptyPage)
        );
        Assert.AreEqual("foobar", userAgent);
    }
}
