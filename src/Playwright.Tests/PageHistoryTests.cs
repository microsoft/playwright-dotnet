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

public class PageHistoryTests : PageTestEx
{
    [PlaywrightTest("page-history.spec.ts", "page.goBack should work")]
    public async Task PageGobackShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.GotoAsync(Server.Prefix + "/grid.html");

        var response = await Page.GoBackAsync();
        Assert.True(response.Ok);
        StringAssert.Contains(Server.EmptyPage, response.Url);

        response = await Page.GoForwardAsync();
        Assert.True(response.Ok);
        StringAssert.Contains("/grid.html", response.Url);

        response = await Page.GoForwardAsync();
        Assert.Null(response);
    }

    [PlaywrightTest("page-history.spec.ts", "page.goBack should work with HistoryAPI")]
    public async Task PageGoBackShouldWorkWithHistoryAPI()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"
              history.pushState({ }, '', '/first.html');
              history.pushState({ }, '', '/second.html');
            ");
        Assert.AreEqual(Server.Prefix + "/second.html", Page.Url);

        await Page.GoBackAsync();
        Assert.AreEqual(Server.Prefix + "/first.html", Page.Url);
        await Page.GoBackAsync();
        Assert.AreEqual(Server.EmptyPage, Page.Url);
        await Page.GoForwardAsync();
        Assert.AreEqual(Server.Prefix + "/first.html", Page.Url);
    }

    [PlaywrightTest("page-history.spec.ts", "should work for file urls")]
    [Ignore("We need screenshots for this")]
    public void ShouldWorkForFileUrls()
    {
    }


    [PlaywrightTest("page-history.spec.ts", "page.reload should work")]
    public async Task PageReloadShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => window._foo = 10");
        await Page.ReloadAsync();
        Assert.Null(await Page.EvaluateAsync("() => window._foo"));
    }
}
