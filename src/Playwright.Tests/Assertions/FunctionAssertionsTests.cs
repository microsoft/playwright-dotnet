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

using System.Diagnostics;

namespace Microsoft.Playwright.Tests.Assertions;

public class FunctionAssertionsTests : PageTestEx
{
    [PlaywrightTest("playwright-test/expect-to-pass.spec.ts", "should retry predicate")]
    public async Task ShouldRetryPredicate()
    {
        var i = 0;
        await Expect(() =>
        {
            Assert.AreEqual(3, ++i);
        }).ToPassAsync();
        Assert.AreEqual(3, i);

        i = 0;
        await Expect(async () =>
        {
            await Task.Delay(50);
            Assert.AreEqual(3, ++i);
        }).ToPassAsync();
        Assert.AreEqual(3, i);
    }

    [PlaywrightTest]
    public async Task ShouldRetryPredicateWithStaticExpect()
    {
        var i = 0;
        await Microsoft.Playwright.Assertions.Expect(async () =>
        {
            await Task.Delay(50);
            Assert.AreEqual(3, ++i);
        }).ToPassAsync();
        Assert.AreEqual(3, i);
    }

    [PlaywrightTest("playwright-test/expect-to-pass.spec.ts", "should respect timeout")]
    public async Task ShouldRespectTimeout()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(() =>
        {
            Assert.AreEqual(2, 1);
        }).ToPassAsync(new() { Timeout = 100 }));
        StringAssert.Contains("Timeout 100ms exceeded while waiting on the predicate", exception.Message);
        StringAssert.Contains("But was:  1", exception.Message);
        Assert.IsInstanceOf<AssertionException>(exception.InnerException);
    }

    [PlaywrightTest("playwright-test/expect-to-pass.spec.ts", "should not fail when used with web-first assertion")]
    public async Task ShouldNotFailWhenUsedWithWebFirstAssertion()
    {
        var i = 0;
        await Expect(async () =>
        {
            if (++i < 3)
            {
                await Expect(Page.Locator("body")).ToHaveTextAsync("foo", new() { Timeout = 1 });
            }
        }).ToPassAsync();
        Assert.AreEqual(3, i);
    }

    [PlaywrightTest("playwright-test/expect-to-pass.spec.ts", "should respect interval")]
    public async Task ShouldRespectInterval()
    {
        var probes = 0;
        var stopwatch = Stopwatch.StartNew();
        await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(() =>
        {
            ++probes;
            Assert.AreEqual(2, 1);
        }).ToPassAsync(new() { Timeout = 1000, Intervals = [0, 10000] }));
        Assert.AreEqual(2, probes);
        Assert.Less(stopwatch.ElapsedMilliseconds, 5000);
    }

    [PlaywrightTest("playwright-test/expect-to-pass.spec.ts", "should use custom message")]
    public async Task ShouldUseCustomMessage()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(() =>
        {
            Assert.AreEqual(3, 1);
        }, "Custom message").ToPassAsync(new() { Timeout = 1 }));
        StringAssert.StartsWith("Custom message\n\n", exception.Message);

        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(async () =>
        {
            await Task.Yield();
            Assert.AreEqual(3, 1);
        }, "Custom message").ToPassAsync(new() { Timeout = 1 }));
        StringAssert.StartsWith("Custom message\n\n", exception.Message);
    }

    [PlaywrightTest]
    public async Task ShouldBeAbleToSetDefaultTimeout()
    {
        try
        {
            SetDefaultExpectTimeout(1111);
            SetDefaultExpectToPassTimeout(100);
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(() =>
            {
                Assert.AreEqual(2, 1);
            }).ToPassAsync());
            StringAssert.Contains("Timeout 100ms exceeded while waiting on the predicate", exception.Message);

            SetDefaultExpectToPassTimeout(30000);
            var i = 0;
            await Expect(async () =>
            {
                await Task.Delay(400);
                Assert.AreEqual(4, ++i);
            }).ToPassAsync();
        }
        finally
        {
            SetDefaultExpectTimeout(5000);
            SetDefaultExpectToPassTimeout(30000);
        }
    }

    [PlaywrightTest]
    public async Task ShouldUseTheLastIntervalForTheFollowingRetries()
    {
        var probes = 0;
        await Expect(() =>
        {
            Assert.AreEqual(5, ++probes);
        }).ToPassAsync(new() { Intervals = [1] });
        Assert.AreEqual(5, probes);
    }

    [PlaywrightTest]
    public async Task ShouldTimeOutWhileTheCallbackIsRunning()
    {
        var stopwatch = Stopwatch.StartNew();
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(() => Task.Delay(10000)).ToPassAsync(new() { Timeout = 100 }));
        Assert.AreEqual("Timeout 100ms exceeded while waiting on the predicate", exception.Message);
        Assert.Less(stopwatch.ElapsedMilliseconds, 5000);

        stopwatch.Restart();
        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightAssertionException>(() => Expect(() => Thread.Sleep(10000)).ToPassAsync(new() { Timeout = 100 }));
        Assert.AreEqual("Timeout 100ms exceeded while waiting on the predicate", exception.Message);
        Assert.Less(stopwatch.ElapsedMilliseconds, 5000);
    }

    [PlaywrightTest]
    public async Task ShouldRetryWithoutTimeout()
    {
        var i = 0;
        await Expect(async () =>
        {
            await Task.Yield();
            Assert.AreEqual(3, ++i);
        }).ToPassAsync(new() { Timeout = 0, Intervals = [10] });
        Assert.AreEqual(3, i);
    }
}
