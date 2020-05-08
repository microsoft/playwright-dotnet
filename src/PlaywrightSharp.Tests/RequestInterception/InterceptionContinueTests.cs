using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Interception.continue</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class InterceptionContinueTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public InterceptionContinueTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Interception.continue</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Interception.continue</playwright-describe>
        ///<playwright-it>should amend HTTP headers</playwright-it>
        [Fact]
        public async Task ShouldAmendHTTPHeaders()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) =>
            {
                var headers = new Dictionary<string, string>(e.Request.Headers) { ["FOO"] = "bar" };
                await e.Request.ContinueAsync(new Payload { Headers = headers });
            };
            await Page.GoToAsync(TestConstants.EmptyPage);
            var requestTask = Server.WaitForRequest("/sleep.zzz", request => request.Headers["foo"]);
            await Task.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz')")
            );
            Assert.Equal("bar", requestTask.Result);
        }
    }
}
