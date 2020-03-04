using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.connect</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class ConnectTests : PlaywrightSharpBrowserContextBaseTest
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
            using var browserApp = await Playwright.LaunchBrowserAppAsync(TestConstants.GetDefaultBrowserOptions());
            using var browser = await Playwright.ConnectAsync(browserApp.ConnectOptions);
            string url = TestConstants.ServerUrl + "/frames/nested-frames.html";
            var page = await browser.DefaultContext.NewPageAsync();
            await page.GoToAsync(url);

            await browser.DisconnectAsync();

            using var remote = await Playwright.ConnectAsync(browserApp.ConnectOptions);

            var pages = (await remote.DefaultContext.GetPagesAsync()).ToList();
            var restoredPage = pages.FirstOrDefault(x => x.Url == url);
            Assert.NotNull(restoredPage);
            var frameDump = FrameUtils.DumpFrames(restoredPage.MainFrame);
            Assert.Equal(TestConstants.NestedFramesDumpResult, frameDump);
            int response = await restoredPage.EvaluateAsync<int>("7 * 8");
            Assert.Equal(56, response);
        }
    }
}
