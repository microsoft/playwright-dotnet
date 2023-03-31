/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests;

public class PageRunAndWaitForRequestTests : PageTestEx
{
    [PlaywrightTest("page-wait-for-request.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var request = await Page.RunAndWaitForRequestAsync(() => Page.EvaluateAsync(@"() => {
                  fetch('/digits/1.png');
                  fetch('/digits/2.png');
                  fetch('/digits/3.png');
                }"), Server.Prefix + "/digits/2.png");

        Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
    }

    [PlaywrightTest("page-wait-for-request.spec.ts", "should work with predicate")]
    public async Task ShouldWorkWithPredicate()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var request = await Page.RunAndWaitForRequestAsync(() => Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }"), e => e.Url == Server.Prefix + "/digits/2.png");

        Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
    }

    [PlaywrightTest("page-wait-for-request.spec.ts", "should respect timeout")]
    public Task ShouldRespectTimeout()
    {
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(
            () => Page.RunAndWaitForRequestAsync(() => Task.CompletedTask, _ => false, new() { Timeout = 1 }));
    }

    [PlaywrightTest("page-wait-for-request.spec.ts", "should respect default timeout")]
    public async Task ShouldRespectDefaultTimeout()
    {
        Page.SetDefaultTimeout(1);
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(
            () => Page.RunAndWaitForRequestAsync(() => Task.CompletedTask, _ => false));
        StringAssert.Contains(exception.Message, "Timeout 1ms exceeded while waiting for event \"Request\"");
    }

    [PlaywrightTest("page-wait-for-request.spec.ts", "should exception preempt timeout")]
    public Task ShouldExceptionPreemptTimeout()
    {
        return PlaywrightAssert.ThrowsAsync<InvalidOperationException>(
            () => Page.RunAndWaitForRequestAsync(() => throw new InvalidOperationException("Custom exception"),
                _ => false, new() { Timeout = 1 }));
    }

    [PlaywrightTest("page-wait-for-request.spec.ts", "should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var request = await Page.RunAndWaitForRequestAsync(() => Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)"), Server.Prefix + "/digits/2.png", new() { Timeout = 0 });

        Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
    }

    [PlaywrightTest("page-wait-for-request.spec.ts", "should work with url match")]
    public async Task ShouldWorkWithUrlMatch()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var request = await Page.RunAndWaitForRequestAsync(() => Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                }"), new Regex(@"/digits/\d.png"));

        Assert.AreEqual(Server.Prefix + "/digits/1.png", request.Url);
    }
}
