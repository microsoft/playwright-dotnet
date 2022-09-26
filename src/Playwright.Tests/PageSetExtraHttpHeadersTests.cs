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

public class PageSetExtraHTTPHeadersTests : PageTestEx
{
    [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Foo"] = "Bar"
        });

        var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
        await TaskUtils.WhenAll(Page.GotoAsync(Server.EmptyPage), headerTask);

        Assert.AreEqual("Bar", headerTask.Result);
    }

    [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work with redirects")]
    public async Task ShouldWorkWithRedirects()
    {
        Server.SetRedirect("/foo.html", "/empty.html");
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Foo"] = "Bar"
        });

        var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
        await TaskUtils.WhenAll(Page.GotoAsync(Server.Prefix + "/foo.html"), headerTask);

        Assert.AreEqual("Bar", headerTask.Result);
    }

    [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work with extra headers from browser context")]
    public async Task ShouldWorkWithExtraHeadersFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync();
        await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Foo"] = "Bar"
        });
        var page = await context.NewPageAsync();

        var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
        await TaskUtils.WhenAll(page.GotoAsync(Server.EmptyPage), headerTask);

        Assert.AreEqual("Bar", headerTask.Result);
    }

    [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should override extra headers from browser context")]
    public async Task ShouldOverrideExtraHeadersFromBrowserContext()
    {
        await using var context = await Browser.NewContextAsync();
        await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["fOo"] = "bAr",
            ["baR"] = "foO",
        });
        var page = await context.NewPageAsync();

        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["Foo"] = "Bar"
        });

        var headerTask = Server.WaitForRequest("/empty.html", request => (request.Headers["Foo"], request.Headers["baR"]));
        await TaskUtils.WhenAll(page.GotoAsync(Server.EmptyPage), headerTask);

        Assert.AreEqual(new string[] { "Bar" }, headerTask.Result.Item1.ToArray());
        Assert.AreEqual(new string[] { "foO" }, headerTask.Result.Item2.ToArray());
    }
}
