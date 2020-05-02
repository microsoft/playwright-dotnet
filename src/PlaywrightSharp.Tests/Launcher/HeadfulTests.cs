using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>headful.spec.js</playwright-file>
    ///<playwright-describe>Headful</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class HeadfulTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public HeadfulTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-describe>Headful</playwright-describe>
        ///<playwright-it>should have default url when launching browser</playwright-it>
        [Fact]
        public async Task ShouldHaveDefaultUrlWhenLaunchingBrowser()
        {
            await using var browser = await Playwright.LaunchAsync(TestConstants.GetHeadfulOptions());
            string[] pages = (await browser.DefaultContext.GetPagesAsync()).Select(page => page.Url).ToArray();
            Assert.Equal(new[] { "about:blank" }, pages);
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-describe>Headful</playwright-describe>
        ///<playwright-it>headless should be able to read cookies written by headful</playwright-it>
        [Fact]
        public async Task HeadlessShouldBeAbleToReadCookiesWrittenByHeadful()
        {
            // "Too" complex for our skip attribute
            if ((TestConstants.IsWindows && TestConstants.IsChromium) || TestConstants.IsFirefox)
            {
                return;
            }
            using var userDataDir = new TempDirectory();

            // Write a cookie in headful chrome
            var headfulOptions = TestConstants.GetHeadfulOptions();
            headfulOptions.UserDataDir = userDataDir.Path;

            var headfulBrowser = await Playwright.LaunchAsync(headfulOptions);
            var headfulPage = await headfulBrowser.DefaultContext.NewPageAsync();
            await headfulPage.GoToAsync(TestConstants.EmptyPage);
            await headfulPage.EvaluateAsync("() => document.cookie = 'foo=true; expires=Fri, 31 Dec 9999 23:59:59 GMT'");
            await headfulBrowser.CloseAsync();

            // Read the cookie from headless chrome
            var headlessOptions = TestConstants.GetDefaultBrowserOptions();
            headlessOptions.UserDataDir = userDataDir.Path;

            var headlessBrowser = await Playwright.LaunchAsync(headlessOptions);
            var headlessPage = await headlessBrowser.DefaultContext.NewPageAsync();
            await headlessPage.GoToAsync(TestConstants.EmptyPage);
            string cookie = await headlessPage.EvaluateAsync<string>("() => document.cookie");
            await headlessBrowser.CloseAsync();

            Assert.Equal("foo=true", cookie);
        }

        ///<playwright-file>headful.spec.js</playwright-file>
        ///<playwright-describe>Headful</playwright-describe>
        ///<playwright-it>should close browser with beforeunload page</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldCloseBrowserWithBeforeunloadPage()
        {
            var headfulOptions = TestConstants.GetDefaultBrowserOptions();
            headfulOptions.Headless = false;
            await using var browser = await Playwright.LaunchAsync(headfulOptions);
            var page = await browser.DefaultContext.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers fire.
            await page.ClickAsync("body");
        }
    }
}
