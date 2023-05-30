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

namespace Microsoft.Playwright.Tests;

public class PageRunAndWaitForRequestFinishedTests : PageTestEx
{
    [PlaywrightTest("page-wait-for-response.spec.ts", "should respect timeout")]
    public Task ShouldRespectTimeout()
    {
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(
            () => Page.RunAndWaitForRequestFinishedAsync(() => Task.CompletedTask, new() { Predicate = _ => false, Timeout = 1, }));
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should respect default timeout")]
    public Task ShouldRespectDefaultTimeout()
    {
        Page.SetDefaultTimeout(1);
        return PlaywrightAssert.ThrowsAsync<TimeoutException>(
            () => Page.RunAndWaitForRequestFinishedAsync(() => Task.CompletedTask, new() { Predicate = _ => false }));
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should exception preempt timeout")]
    public async Task ShouldExceptionPreemptTimeout()
    {
        await PlaywrightAssert.ThrowsAsync<InvalidOperationException>(
            () => Page.RunAndWaitForRequestFinishedAsync(() => throw new InvalidOperationException("Custom exception"),
                new() { Predicate = _ => false, Timeout = 1000, }));
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should exception preempt timeout")]
    public async Task ShouldDelayedExceptionPreemptTimeout()
    {
        await PlaywrightAssert.ThrowsAsync<InvalidOperationException>(
            () => Page.RunAndWaitForRequestFinishedAsync(
                () => Task.Delay(100).ContinueWith(t => throw new InvalidOperationException("Custom exception")),
                new() { Predicate = _ => false, Timeout = 2000 }));
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should work with predicate")]
    public async Task ShouldWorkWithPredicate()
    {
        using var cts = new CancellationTokenSource();
        await Page.GotoAsync(Server.EmptyPage);
        var request = await Page.RunAndWaitForRequestFinishedAsync(() => Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png').then((response) => response.blob());
                    fetch('/digits/2.png').then((response) => response.blob());
                    fetch('/digits/3.png').then((response) => response.blob());
                }"), new() { Predicate = e => e.Url == Server.Prefix + "/digits/2.png", CancellationToken = cts.Token });

        Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should work with no timeout")]
    public async Task ShouldWorkWithNoTimeout()
    {
        using var cts = new CancellationTokenSource();
        await Page.GotoAsync(Server.EmptyPage);
        var request = await Page.RunAndWaitForRequestFinishedAsync(() => Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png').then((response) => response.blob());
                    fetch('/digits/2.png').then((response) => response.blob());
                    fetch('/digits/3.png').then((response) => response.blob());
                }, 50)"), new() { Predicate = e => e.Url == Server.Prefix + "/digits/2.png", Timeout = 0, CancellationToken = cts.Token });

        Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
    }

    [PlaywrightTest("page-wait-for-response.spec.ts", "should cancel work")]
    public async Task ShouldCancelWorkWithDelay()
    {
        using var cts = new CancellationTokenSource();
        await Page.GotoAsync(Server.EmptyPage);

        await Page.SetContentAsync("""
        <button onclick="testCancellation(); false;" />
        <script>
        function testCancellation() {
            setTimeout(() => {
                    fetch('/digits/1.png').then((response) => response.blob());
                    fetch('/digits/2.png').then((response) => response.blob());
                    fetch('/digits/3.png').then((response) => response.blob());
                }, 5000);
        }
        </script>
        """);

        var requestTask = Page.RunAndWaitForRequestFinishedAsync(
            () => Page.EvaluateAsync(@"() => document.querySelector('button').click()"),
            new()
            {
                Predicate = e => e.Url == Server.Prefix + "/digits/2.png",
                Timeout = 8000,
                CancellationToken = cts.Token
            });
        cts.Cancel();

        var request = await requestTask;

        Assert.IsNull(request);
    }
}
