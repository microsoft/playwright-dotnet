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

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should accept a string")]
        [Fact(Skip = "We don't this test")]
        public void ShouldAcceptAString()
        {
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should work when resolved right before execution context disposal")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should poll on interval")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPollOnInterval()
        {
            int polling = 100;
            var timeDelta = await Page.WaitForFunctionAsync(@"() => {
                if (!window.__startTime) {
                    window.__startTime = Date.now();
                    return false;
                }
                return Date.now() - window.__startTime;
            }", polling: polling);
            int value = (await timeDelta.GetJsonValueAsync<int>());

            Assert.True(value >= polling);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should avoid side effects after timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAvoidSideEffectsAfterTimeout()
        {
            int counter = 0;
            Page.Console += (sender, e) => ++counter;

            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForFunctionAsync(
                @"() => {
                  window.counter = (window.counter || 0) + 1;
                  console.log(window.counter);
                }",
                polling: 1,
                timeout: 1000));

            int savedCounter = counter;
            await Page.WaitForTimeoutAsync(2000);

            Assert.Contains("Timeout 1000ms exceeded", exception.Message);
            Assert.Equal(savedCounter, counter);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should throw on polling:mutation")]
        [Fact(Skip = "We don't need to test this")]
        public void ShouldThrowOnPollingMutation()
        {
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should poll on raf")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPollOnRaf()
        {
            var watchdog = Page.WaitForFunctionAsync(
                "() => window.__FOO === 'hit'",
                polling: Polling.Raf);
            await Page.EvaluateAsync("window.__FOO = 'hit'");
            await watchdog;
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should fail with predicate throwing on first call")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithPredicateThrowingOnFirstCall()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.WaitForFunctionAsync("() => { throw new Error('oh my'); }"));
            Assert.Contains("oh my", exception.Message);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should fail with predicate throwing sometimes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should fail with ReferenceError on wrong page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithReferenceErrorOnWrongPage()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.WaitForFunctionAsync("() => globalVar === 123"));
            Assert.Contains("globalVar", exception.Message);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should work with strict CSP policy")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should throw on bad polling value")]
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowOnBadPollingValue()
        {
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should throw negative polling interval")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowNegativePollingInterval()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.WaitForFunctionAsync("() => !!document.body", -10));

            Assert.Contains("Cannot poll with non-positive interval", exception.Message);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should return the success value as a JSHandle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheSuccessValueAsAJSHandle()
            => Assert.Equal(5, await (await Page.WaitForFunctionAsync("() => 5")).GetJsonValueAsync<int>());

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should return the window as a success value")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheWindowAsASuccessValue()
            => Assert.NotNull(await Page.WaitForFunctionAsync("() => window"));

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should accept ElementHandle arguments")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.WaitForFunctionAsync("false", timeout: 10));

            Assert.Contains("Timeout 10ms exceeded", exception.Message);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should respect default timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.DefaultTimeout = 1;
            var exception = await Assert.ThrowsAsync<TimeoutException>(()
                => Page.WaitForFunctionAsync("false"));

            Assert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should disable timeout when its set to 0")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDisableTimeoutWhenItsSetTo0()
        {
            var watchdog = Page.WaitForFunctionAsync(
                @"() => {
                    window.__counter = (window.__counter || 0) + 1;
                    return window.__injected;
                }",
                polling: 10,
                timeout: 0);
            await Page.WaitForFunctionAsync("() => window.__counter > 10");
            await Page.EvaluateAsync("window.__injected = true");
            await watchdog;
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should survive cross-process navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should survive navigations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSurviveNavigations()
        {
            var watchdog = Page.WaitForFunctionAsync("() => window.__done");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.GoToAsync(TestConstants.ServerUrl + "/consolelog.html");
            await Page.EvaluateAsync("() => window.__done = true");
            await watchdog;
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should work with multiline body")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithMultilineBody()
        {
            var result = await Page.WaitForFunctionAsync(@"
                (() => true)()
            ");

            Assert.True(await result.GetJsonValueAsync<bool>());
        }

        [PlaywrightTest("waittask.spec.js", "Frame.waitForFunction", "should wait for predicate with arguments")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public Task ShouldWaitForPredicateWithArguments()
            => Page.WaitForFunctionAsync(@"({arg1, arg2}) => arg1 + arg2 === 3", new { arg1 = 1, arg2 = 2 });
    }
}
