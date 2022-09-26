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

public class PageClickTimeout1Tests : PageTestEx
{
    [PlaywrightTest("page-click-timeout-1.spec.ts", "should timeout waiting for button to be enabled")]
    public async Task ShouldTimeoutWaitingForButtonToBeEnabled()
    {
        await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" disabled><span>Click target</span></button>");
        var clickTask = Page.ClickAsync("text=Click target", new() { Timeout = 3000 });
        Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => clickTask);

        StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
        StringAssert.Contains("element is not enabled - waiting", exception.Message);
    }
}
