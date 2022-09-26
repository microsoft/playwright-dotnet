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

using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class PageNavigationTests : PageTestEx
{
    [PlaywrightTest("page-navigation.spec.ts", "should work with _blank target")]
    public async Task ShouldWorkWithBlankTarget()
    {
        Server.SetRoute("/empty.html", ctx =>
        ctx.Response.WriteAsync($"<a href=\"{Server.EmptyPage}\" target=\"_blank\">Click me</a>"));
        await Page.GotoAsync(Server.EmptyPage);
        await Page.ClickAsync("\"Click me\"");
    }

    [PlaywrightTest("page-navigation.spec.ts", "should work with cross-process _blank target")]
    public async Task ShouldWorkWithCrossProcessBlankTarget()
    {
        Server.SetRoute("/empty.html", ctx =>
        ctx.Response.WriteAsync($"<a href=\"{Server.CrossProcessPrefix}/empty.html\" target=\"_blank\">Click me</a>"));
        await Page.GotoAsync(Server.EmptyPage);
        await Page.ClickAsync("\"Click me\"");
    }
}
