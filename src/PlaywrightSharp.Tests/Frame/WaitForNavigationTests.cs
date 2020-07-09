using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Frame.waitForNavigation</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class WaitForNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForNavigation</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.FirstChildFrame();
            var waitForNavigationResult = frame.WaitForNavigationAsync();

            await Task.WhenAll(
                waitForNavigationResult,
                frame.EvaluateAsync("url => window.location.href = url", TestConstants.ServerUrl + "/grid.html")
            );
            var response = await waitForNavigationResult;
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Contains("grid.html", response.Url);
            Assert.Same(frame, response.Frame);
            Assert.Contains("/frames/one-frame.html", Page.Url);
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForNavigation</playwright-describe>
        ///<playwright-it>should fail when frame detaches</playwright-it>
        [Retry]
        public async Task ShouldFailWhenFrameDetaches()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.FirstChildFrame();
            Server.SetRoute("/empty.html", context => Task.Delay(10000));
            var waitForNavigationResult = frame.WaitForNavigationAsync();
            await Task.WhenAll(
                Server.WaitForRequest("/empty.html"),
                frame.EvaluateAsync($"() => window.location = '{TestConstants.EmptyPage}'"));

            await Page.QuerySelectorEvaluateAsync("iframe", "frame => frame.remove()");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => waitForNavigationResult);
            Assert.Contains("frame was detached", exception.Message);
        }
    }
}
