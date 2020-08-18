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
    public class WaitForNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Frame.waitForNavigation</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.FirstChildFrame();
            var waitForNavigationResult = frame.WaitForNavigationAsync();

            await TaskUtils.WhenAll(
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
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldFailWhenFrameDetaches()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames[1];
            Server.SetRoute("/empty.html", context => Task.Delay(10000));

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => TaskUtils.WhenAll(
                frame.WaitForNavigationAsync(),
                frame.EvaluateAsync($"() => window.location = '{TestConstants.EmptyPage}'"),
                Page.EvaluateAsync($"setTimeout(() => document.querySelector(\"iframe\").remove())")));

            Assert.Contains("waiting for navigation until \"load\"", exception.Message);
            Assert.Contains("frame was detached", exception.Message);
        }
    }
}
