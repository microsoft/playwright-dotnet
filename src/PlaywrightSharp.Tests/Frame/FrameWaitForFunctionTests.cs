using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Frame.waitForFunction</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FrameWaitForFunctionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public FrameWaitForFunctionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should accept a string</playwright-it>
        [Fact(Skip = "We don't this test")]
        public void ShouldAcceptAString()
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should work when resolved right before execution context disposal</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWhenResolvedRightBeforeExecutionContextDisposal()
        {
            await Page.AddInitScriptAsync("() => window.__RELOADED = true");
            await Page.WaitForFunctionAsync(@"() =>
            {
                if (!window.__RELOADED)
                    window.location.reload();
                return true;
            }");
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should poll on interval</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPollOnInterval()
        {
            int polling = 100;
            var timeDelta = await Page.WaitForFunctionAsync(@"() => {
                if (!window.__startTime) {
                    window.__startTime = Date.now();
                    return false;
                }
                return Date.now() - window.__startTime;
            }", pollingInterval: polling);
            int value = (await timeDelta.GetJsonValueAsync<int>());

            Assert.True(value >= polling);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should avoid side effects after timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAvoidSideEffectsAfterTimeout()
        {
            int counter = 0;
            Page.Console += (sender, e) => ++counter;

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForFunctionAsync(
                @"() => {
                  window.counter = (window.counter || 0) + 1;
                  console.log(window.counter);
                }",
                pollingInterval: 1,
                timeout: 1000));

            int savedCounter = counter;
            await Page.WaitForTimeoutAsync(2000);

            Assert.Contains("Timeout 1000ms exceeded", exception.Message);
            Assert.Equal(savedCounter, counter);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should throw on polling:mutation</playwright-it>
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowOnPollingMutation()
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should poll on raf</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldPollOnRaf()
        {
            var watchdog = Page.WaitForFunctionAsync(
                "() => window.__FOO === 'hit'",
                polling: Polling.Raf);
            await Page.EvaluateAsync("window.__FOO = 'hit'");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should fail with predicate throwing on first call</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFailWithPredicateThrowingOnFirstCall()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.WaitForFunctionAsync("() => { throw new Error('oh my'); }"));
            Assert.Contains("oh my", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should fail with predicate throwing sometimes</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFailWithPredicateThrowingSometimes()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.WaitForFunctionAsync(@"() => {
              window.counter = (window.counter || 0) + 1;
              if (window.counter === 3)
                throw new Error('Bad counter!');
              return window.counter === 5 ? 'result' : false;
            }"));
            Assert.Contains("Bad counter!", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should fail with ReferenceError on wrong page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFailWithReferenceErrorOnWrongPage()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.WaitForFunctionAsync("() => globalVar === 123"));
            Assert.Contains("globalVar", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should work with strict CSP policy</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithStrictCSPPolicy()
        {
            Server.SetCSP("/empty.html", "script-src " + TestConstants.ServerUrl);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await TaskUtils.WhenAll(
                Page.WaitForFunctionAsync(
                    "() => window.__FOO === 'hit'",
                    polling: Polling.Raf),
                Page.EvaluateAsync("window.__FOO = 'hit'"));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should throw on bad polling value</playwright-it>
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowOnBadPollingValue()
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should throw negative polling interval</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowNegativePollingInterval()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.WaitForFunctionAsync("() => !!document.body", pollingInterval: -10));

            Assert.Contains("Cannot poll with non-positive interval", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should return the success value as a JSHandle</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnTheSuccessValueAsAJSHandle()
            => Assert.Equal(5, await (await Page.WaitForFunctionAsync("() => 5")).GetJsonValueAsync<int>());

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should return the window as a success value</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnTheWindowAsASuccessValue()
            => Assert.NotNull(await Page.WaitForFunctionAsync("() => window"));

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should accept ElementHandle arguments</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.WaitForFunctionAsync("false", timeout: 10));

            Assert.Contains("Timeout 10ms exceeded", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should respect default timeout</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.DefaultTimeout = 1;
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.WaitForFunctionAsync("false"));

            Assert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should disable timeout when its set to 0</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldDisableTimeoutWhenItsSetTo0()
        {
            var watchdog = Page.WaitForFunctionAsync(@"() => {
                window.__counter = (window.__counter || 0) + 1;
                return window.__injected;
            }",
            timeout: 0,
            pollingInterval: 10);
            await Page.WaitForFunctionAsync("() => window.__counter > 10");
            await Page.EvaluateAsync("window.__injected = true");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should survive cross-process navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSurviveCrossProcessNavigation()
        {
            bool fooFound = false;
            var waitForFunction = Page.WaitForFunctionAsync("window.__FOO === 1")
                .ContinueWith(_ => fooFound = true);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.False(fooFound);
            await Page.ReloadAsync();
            Assert.False(fooFound);
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/grid.html");
            Assert.False(fooFound);
            await Page.EvaluateAsync("window.__FOO = 1");
            await waitForFunction;
            Assert.True(fooFound);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should survive navigations</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSurviveNavigations()
        {
            var watchdog = Page.WaitForFunctionAsync("() => window.__done");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.GoToAsync(TestConstants.ServerUrl + "/consolelog.html");
            await Page.EvaluateAsync("() => window.__done = true");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should work with multiline body</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithMultilineBody()
        {
            var result = await Page.WaitForFunctionAsync(@"
                (() => true)()
            ");

            Assert.True(await result.GetJsonValueAsync<bool>());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should wait for predicate with arguments</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public Task ShouldWaitForPredicateWithArguments()
            => Page.WaitForFunctionAsync(@"({arg1, arg2}) => arg1 + arg2 === 3", new { arg1 = 1, arg2 = 2 });
    }
}
