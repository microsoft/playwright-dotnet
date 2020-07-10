using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    /*
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.connect</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class ConnectTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public ConnectTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.connect</playwright-describe>
        ///<playwright-it>should be able to reconnect to a browser</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldBeAbleToReconnectToADisconnectedBrowser()
        {
            using var browserApp = await BrowserType.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            using var browser = await BrowserType.ConnectAsync(browserApp.ConnectOptions);
            string url = TestConstants.ServerUrl + "/frames/nested-frames.html";
            var page = await browser.DefaultContext.NewPageAsync();
            await page.GoToAsync(url);

            await browser.DisconnectAsync();

            using var remote = await BrowserType.ConnectAsync(browserApp.ConnectOptions);

            var pages = (await remote.DefaultContext.GetPagesAsync()).ToList();
            var restoredPage = pages.FirstOrDefault(x => x.Url == url);
            Assert.NotNull(restoredPage);
            var frameDump = FrameUtils.DumpFrames(restoredPage.MainFrame);
            Assert.Equal(TestConstants.NestedFramesDumpResult, frameDump);
            int response = await restoredPage.EvaluateAsync<int>("7 * 8");
            Assert.Equal(56, response);

            await remote.DisconnectAsync();
        }
    }
    */
}
