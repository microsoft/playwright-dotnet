using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/headful.spec.js</playwright-file>
    ///<playwright-describe>ChromiumHeadful</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class HeadfulTests : PlaywrightSharpBrowserBaseTest
    {
        readonly LaunchOptions _extensionOptions;

        /// <inheritdoc/>
        public HeadfulTests(ITestOutputHelper output) : base(output)
        {
            string extensionPath = TestUtils.GetWebServerFile("simple-extension");

            _extensionOptions = TestConstants.GetDefaultBrowserOptions();
            _extensionOptions.Headless = false;
            _extensionOptions.Args = new[]
            {
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}"
            };
        }

        ///<playwright-file>chromium/headful.spec.js</playwright-file>
        ///<playwright-describe>ChromiumHeadful</playwright-describe>
        ///<playwright-it>target.page() should return a background_page</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task TargetPageShouldReturnABackgroundPage()
        {
            await using var browserWithExtension = await BrowserType.LaunchAsync(_extensionOptions);
            var backgroundPageTarget = await browserWithExtension.WaitForTargetAsync(target => target.Type == TargetType.BackgroundPage);
            var page = await backgroundPageTarget.GetPageAsync();

            Assert.Equal(6, await page.EvaluateAsync<int>("() => 2 * 3"));
            Assert.Equal(42, await page.EvaluateAsync<int>("() => window.MAGIC"));
        }

        ///<playwright-file>chromium/headful.spec.js</playwright-file>
        ///<playwright-describe>ChromiumHeadful</playwright-describe>
        ///<playwright-it>OOPIF: should report google.com frame</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldOpenDevtoolsWhenDevtoolsTrueOptionIsGiven()
        {
            var headfulOptions = TestConstants.GetHeadfulOptions();
            headfulOptions.Devtools = true;
            await using var browser = await BrowserType.LaunchAsync(headfulOptions);
            var context = await browser.NewContextAsync();
            await TaskUtils.WhenAll(
                context.NewPageAsync(),
                browser.WaitForTargetAsync(target => target.Url.Contains("devtools://")));
        }
    }
}
