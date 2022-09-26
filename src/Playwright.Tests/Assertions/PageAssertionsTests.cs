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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Assertions;

public class PageAssertionsTests : PageTestEx
{
    [PlaywrightTest("playwright-test/playwright.expect.misc.spec.ts", "should support toHaveTitle")]
    public async Task ShouldSupportToHaveTitleAsync()
    {
        await Page.SetContentAsync("<title>  Hello     world</title>");
        await Expect(Page).ToHaveTitleAsync("Hello  world");

        await Page.SetContentAsync("<title>Bye</title>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(Page).ToHaveTitleAsync("Hello", new() { Timeout = 100 }));
        StringAssert.Contains("Page title expected to be 'Hello'", exception.Message);
        StringAssert.Contains("But was: 'Bye'", exception.Message);
        StringAssert.Contains("PageAssertions.ToHaveTitleAsync with timeout 100ms", exception.Message);

        await Page.SetContentAsync("<title>Foo Bar Kek</title>");
        await Expect(Page).ToHaveTitleAsync(new Regex("^Foo .* Kek$"));
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(Page).ToHaveTitleAsync(new Regex("nooo"), new() { Timeout = 100 }));

        await Page.SetContentAsync("<title>Bye</title>");
        await Expect(Page).Not.ToHaveTitleAsync("Hello");
    }

    [PlaywrightTest("playwright-test/playwright.expect.misc.spec.ts", "should support toHaveURL")]
    public async Task ShouldSupportToHaveURLAsync()
    {
        await Page.GotoAsync("data:text/html,<div>A</div>");
        await Expect(Page).ToHaveURLAsync("data:text/html,<div>A</div>");

        await Page.GotoAsync("data:text/html,<div>B</div>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(Page).ToHaveURLAsync("wrong", new() { Timeout = 1000 }));
        StringAssert.Contains("Page URL expected to be 'wrong'", exception.Message);
        StringAssert.Contains("But was: 'data:text/html,<div>B</div>'", exception.Message);
        StringAssert.Contains("PageAssertions.ToHaveURLAsync with timeout 1000ms", exception.Message);

        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page).ToHaveURLAsync(new Regex(".*empty.html"));
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(Page).ToHaveURLAsync(new Regex("nooo"), new() { Timeout = 1000 }));

        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page).ToHaveURLAsync(Server.Prefix + "/empty.html");
        await Expect(Page).Not.ToHaveURLAsync(Server.Prefix + "/foobar.html");

        var page = await Browser.NewPageAsync(new() { BaseURL = Server.Prefix });
        try
        {
            await page.GotoAsync("/empty.html");
            await Expect(page).ToHaveURLAsync("/empty.html");
            await Expect(page).Not.ToHaveURLAsync("/kek.html");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
