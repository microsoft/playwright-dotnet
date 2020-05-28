using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Frame.waitForFunction</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
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
        [Retry]
        public async Task ShouldWorkWhenResolvedRightBeforeExecutionContextDisposal()
        {
            await Page.EvaluateOnNewDocumentAsync("() => window.__RELOADED = true");
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
        [Retry]
        public async Task ShouldPollOnInterval()
        {
            int polling = 100;
            var timeDelta = await Page.WaitForFunctionAsync(@"() => {
                if (!window.__startTime) {
                    window.__startTime = Date.now();
                    return false;
                }
                return Date.now() - window.__startTime;
            }", new WaitForFunctionOptions { PollingInterval = polling });
            int value = (await timeDelta.GetJsonValueAsync<int>());

            Assert.True(value >= polling);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should poll on mutation</playwright-it>
        [Retry]
        public async Task ShouldPollOnMutation()
        {
            bool success = false;
            var watchdog = Page.WaitForFunctionAsync("() => window.__FOO === 'hit'",
                new WaitForFunctionOptions { Polling = WaitForFunctionPollingOption.Mutation })
                .ContinueWith(_ => success = true);
            await Page.EvaluateAsync("window.__FOO = 'hit'");
            Assert.False(success);
            await Page.EvaluateAsync("document.body.appendChild(document.createElement('div'))");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should poll on raf</playwright-it>
        [Retry]
        public async Task ShouldPollOnRaf()
        {
            var watchdog = Page.WaitForFunctionAsync("() => window.__FOO === 'hit'",
                new WaitForFunctionOptions { Polling = WaitForFunctionPollingOption.Raf });
            await Page.EvaluateAsync("window.__FOO = 'hit'");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should work with strict CSP policy</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWorkWithStrictCSPPolicy()
        {
            Server.SetCSP("/empty.html", "script-src " + TestConstants.ServerUrl);
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Task.WhenAll(
                Page.WaitForFunctionAsync("() => window.__FOO === 'hit'", new WaitForFunctionOptions
                {
                    Polling = WaitForFunctionPollingOption.Raf
                }),
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
        [Retry]
        public async Task ShouldThrowNegativePollingInterval()
        {
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()
                => Page.WaitForFunctionAsync("() => !!document.body", new WaitForFunctionOptions { PollingInterval = -10 }));

            Assert.Contains("Cannot poll with non-positive interval", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should return the success value as a JSHandle</playwright-it>
        [Retry]
        public async Task ShouldReturnTheSuccessValueAsAJSHandle()
            => Assert.Equal(5, await (await Page.WaitForFunctionAsync("() => 5")).GetJsonValueAsync<int>());

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should return the window as a success value</playwright-it>
        [Retry]
        public async Task ShouldReturnTheWindowAsASuccessValue()
            => Assert.NotNull(await Page.WaitForFunctionAsync("() => window"));

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should accept ElementHandle arguments</playwright-it>
        [Retry]
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
        [Retry]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.WaitForFunctionAsync("false", new WaitForFunctionOptions { Timeout = 10 }));

            Assert.Contains("waiting for function failed: timeout", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should respect default timeout</playwright-it>
        [Retry]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.DefaultTimeout = 1;
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.WaitForFunctionAsync("false"));

            Assert.Contains("waiting for function failed: timeout", exception.Message);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should disable timeout when its set to 0</playwright-it>
        [Retry]
        public async Task ShouldDisableTimeoutWhenItsSetTo0()
        {
            var watchdog = Page.WaitForFunctionAsync(@"() => {
                window.__counter = (window.__counter || 0) + 1;
                return window.__injected;
            }", new WaitForFunctionOptions { Timeout = 0, PollingInterval = 10 });
            await Page.WaitForFunctionAsync("() => window.__counter > 10");
            await Page.EvaluateAsync("window.__injected = true");
            await watchdog;
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForFunction</playwright-describe>
        ///<playwright-it>should survive cross-process navigation</playwright-it>
        [Retry]
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
        [Retry]
        public async Task ShouldSurviveNavigations()
        {
            var watchdog = Page.WaitForFunctionAsync("() => window.__done");
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.GoToAsync(TestConstants.ServerUrl + "/consolelog.html");
            await Page.EvaluateAsync("() => window.__done = true");
            await watchdog;
        }
    }
}
