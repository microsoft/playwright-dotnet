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

public class PageClickTimeout2Tests : PageTestEx
{
    [PlaywrightTest("page-click-timeout-2.spec.ts", "should timeout waiting for display:none to be gone")]
    public async Task ShouldTimeoutWaitingForDisplayNoneToBeGone()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.ClickAsync("button", new() { Timeout = 5000 }));

        StringAssert.Contains("Timeout 5000ms exceeded", exception.Message);
        StringAssert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
        StringAssert.Contains("element is not visible - waiting", exception.Message);
    }

    [PlaywrightTest("page-click-timeout-2.spec.ts", "should timeout waiting for visibility:hidden to be gone")]
    public async Task ShouldTimeoutWaitingForVisibilityHiddenToBeGone()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.ClickAsync("button", new() { Timeout = 5000 }));

        StringAssert.Contains("Timeout 5000ms exceeded", exception.Message);
        StringAssert.Contains("waiting for element to be visible, enabled and stable", exception.Message);
        StringAssert.Contains("element is not visible - waiting", exception.Message);
    }

}
