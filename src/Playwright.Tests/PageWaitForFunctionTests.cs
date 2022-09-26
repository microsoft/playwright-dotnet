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

public class PageWaitForFunctionTests : PageTestEx
{
    [PlaywrightTest("page-wait-for-function.spec.ts", "should timeout")]
    public async Task ShouldTimeout()
    {
        var startTime = DateTime.Now;
        int timeout = 42;
        await Page.WaitForTimeoutAsync(timeout);
        Assert.True((DateTime.Now - startTime).TotalMilliseconds > timeout / 2);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should accept a string")]
    [Ignore("We don't this test")]
    public void ShouldAcceptAString()
    {
    }

    [PlaywrightTest("page-wait-for-function.spec.tsPageWaitForFunctionTests", "should work when resolved right before execution context disposal")]
    public async Task ShouldWorkWhenResolvedRightBeforeExecutionContextDisposal()
    {
        await Page.AddInitScriptAsync("window.__RELOADED = true");
        await Page.WaitForFunctionAsync(@"() =>
            {
                if (!window.__RELOADED)
                    window.location.reload();
                return true;
            }");
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should poll on interval")]
    public async Task ShouldPollOnInterval()
    {
        int polling = 100;
        var timeDelta = await Page.WaitForFunctionAsync(@"() => {
                if (!window.__startTime) {
                    window.__startTime = Date.now();
                    return false;
                }
                return Date.now() - window.__startTime;
            }", null, new() { PollingInterval = polling });
        int value = (await timeDelta.JsonValueAsync<int>());

        Assert.True(value >= polling);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should avoid side effects after timeout")]
    public async Task ShouldAvoidSideEffectsAfterTimeout()
    {
        int counter = 0;
        Page.Console += (_, _) => ++counter;

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForFunctionAsync(
            @"() => {
                  window.counter = (window.counter || 0) + 1;
                  console.log(window.counter);
                }",
            null, new() { PollingInterval = 1, Timeout = 1000 }));

        int savedCounter = counter;
        await Page.WaitForTimeoutAsync(2000);

        StringAssert.Contains("Timeout 1000ms exceeded", exception.Message);
        Assert.AreEqual(savedCounter, counter);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should throw on polling:mutation")]
    [Ignore("We don't need to test this")]
    public void ShouldThrowOnPollingMutation()
    {
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should poll on raf")]
    [Ignore("We don't support raf")]
    public void ShouldPollOnRaf()
    {
        /*
        var watchdog = Page.WaitForFunctionAsync(
            "() => window.__FOO === 'hit'",
            polling: Polling.Raf);
        await Page.EvaluateAsync("window.__FOO = 'hit'");
        await watchdog;
        */
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should fail with predicate throwing on first call")]
    public async Task ShouldFailWithPredicateThrowingOnFirstCall()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.WaitForFunctionAsync("() => { throw new Error('oh my'); }"));
        StringAssert.Contains("oh my", exception.Message);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should fail with predicate throwing sometimes")]
    public async Task ShouldFailWithPredicateThrowingSometimes()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.WaitForFunctionAsync(@"() => {
              window.counter = (window.counter || 0) + 1;
              if (window.counter === 3)
                throw new Error('Bad counter!');
              return window.counter === 5 ? 'result' : false;
            }"));
        StringAssert.Contains("Bad counter!", exception.Message);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should fail with ReferenceError on wrong page")]
    public async Task ShouldFailWithReferenceErrorOnWrongPage()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.WaitForFunctionAsync("() => globalVar === 123"));
        StringAssert.Contains("globalVar", exception.Message);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should work with strict CSP policy")]
    [Ignore("We don't support raf")]
    public void ShouldWorkWithStrictCSPPolicy()
    {
        /*
        Server.SetCSP("/empty.html", "script-src " + Server.Prefix);
        await Page.GotoAsync(Server.EmptyPage);
        await TaskUtils.WhenAll(
            Page.WaitForFunctionAsync(
                "() => window.__FOO === 'hit'",
                polling: Polling.Raf),
            Page.EvaluateAsync("window.__FOO = 'hit'"));
        */
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should throw on bad polling value")]
    [Ignore("We don't this test")]
    public void ShouldThrowOnBadPollingValue()
    {
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should throw negative polling interval")]
    public async Task ShouldThrowNegativePollingInterval()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(()
            => Page.WaitForFunctionAsync("() => !!document.body", null, new() { PollingInterval = -10 }));

        StringAssert.Contains("Cannot poll with non-positive interval", exception.Message);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should return the success value as a JSHandle")]
    public async Task ShouldReturnTheSuccessValueAsAJSHandle()
        => Assert.AreEqual(5, await (await Page.WaitForFunctionAsync("() => 5")).JsonValueAsync<int>());

    [PlaywrightTest("page-wait-for-function.spec.ts", "should return the window as a success value")]
    public async Task ShouldReturnTheWindowAsASuccessValue()
        => Assert.NotNull(await Page.WaitForFunctionAsync("() => window"));

    [PlaywrightTest("page-wait-for-function.spec.ts", "should accept ElementHandle arguments")]
    public async Task ShouldAcceptElementHandleArguments()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        bool resolved = false;
        var waitForFunction = Page.WaitForFunctionAsync("element => !element.parentElement", div)
            .ContinueWith(_ => resolved = true);
        Assert.False(resolved);
        await Page.EvaluateAsync("element => element.remove()", div);
        await waitForFunction;
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should respect timeout")]
    public async Task ShouldRespectTimeout()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForFunctionAsync("false", null, new() { Timeout = 10 }));

        StringAssert.Contains("Timeout 10ms exceeded", exception.Message);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should respect default timeout")]
    public async Task ShouldRespectDefaultTimeout()
    {
        Page.SetDefaultTimeout(1);
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForFunctionAsync("false"));

        StringAssert.Contains("Timeout 1ms exceeded", exception.Message);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should disable timeout when its set to 0")]
    public async Task ShouldDisableTimeoutWhenItsSetTo0()
    {
        var watchdog = Page.WaitForFunctionAsync(
            @"() => {
                    window.__counter = (window.__counter || 0) + 1;
                    return window.__injected;
                }",
            null,
            new()
            {
                PollingInterval = 10,
                Timeout = 0
            });
        await Page.WaitForFunctionAsync("() => window.__counter > 10");
        await Page.EvaluateAsync("window.__injected = true");
        await watchdog;
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should survive cross-process navigation")]
    public async Task ShouldSurviveCrossProcessNavigation()
    {
        bool fooFound = false;
        var waitForFunction = Page.WaitForFunctionAsync("window.__FOO === 1")
            .ContinueWith(_ => fooFound = true);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.False(fooFound);
        await Page.ReloadAsync();
        Assert.False(fooFound);
        await Page.GotoAsync(Server.CrossProcessPrefix + "/grid.html");
        Assert.False(fooFound);
        await Page.EvaluateAsync("window.__FOO = 1");
        await waitForFunction;
        Assert.True(fooFound);
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should survive navigations")]
    public async Task ShouldSurviveNavigations()
    {
        var watchdog = Page.WaitForFunctionAsync("() => window.__done");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.GotoAsync(Server.Prefix + "/consolelog.html");
        await Page.EvaluateAsync("() => window.__done = true");
        await watchdog;
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should work with multiline body")]
    public async Task ShouldWorkWithMultilineBody()
    {
        var result = await Page.WaitForFunctionAsync(@"
                (() => true)()
            ");

        Assert.True(await result.JsonValueAsync<bool>());
    }

    [PlaywrightTest("page-wait-for-function.spec.ts", "should wait for predicate with arguments")]
    public Task ShouldWaitForPredicateWithArguments()
        => Page.WaitForFunctionAsync(@"({arg1, arg2}) => arg1 + arg2 === 3", new { arg1 = 1, arg2 = 2 });
}
