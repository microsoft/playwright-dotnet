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

public class PageWaitForResponseTests : PageTestEx
{
    [PlaywrightTest("page-wait-for-response.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var task = Page.WaitForResponseAsync(Server.Prefix + "/digits/2.png");
        var (response, _) = await TaskUtils.WhenAll(
            task,
            Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
        );
        Assert.AreEqual(Server.Prefix + "/digits/2.png", response.Url);
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should respect timeout")]
    public Task ShouldRespectTimeout()
    {
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(
            () => Page.WaitForResponseAsync(_ => false, new()
            {
                Timeout = 1,
            }));
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should respect default timeout")]
    public Task ShouldRespectDefaultTimeout()
    {
        Page.SetDefaultTimeout(1);
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(
            () => Page.WaitForResponseAsync(_ => false));
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should work with predicate")]
    public async Task ShouldWorkWithPredicate()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var task = Page.WaitForResponseAsync(e => e.Url == Server.Prefix + "/digits/2.png");
        var (responseEvent, _) = await TaskUtils.WhenAll(
            task,
            Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
        );
        Assert.AreEqual(Server.Prefix + "/digits/2.png", responseEvent.Url);
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var task = Page.WaitForResponseAsync(Server.Prefix + "/digits/2.png", new() { Timeout = 0 });
        var (response, _) = await TaskUtils.WhenAll(
            task,
            Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
        );
        Assert.AreEqual(Server.Prefix + "/digits/2.png", response.Url);
    }
}
