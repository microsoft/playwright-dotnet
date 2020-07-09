using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    /*
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.defaultArguments</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class DefaultArgumentsTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public DefaultArgumentsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.defaultArguments</playwright-describe>
        ///<playwright-it>should return the default arguments</playwright-it>
        [Retry]
        public void ShouldReturnTheDefaultArguments()
        {
            if (TestConstants.IsChromium)
            {
                Assert.Contains("--no-first-run", BrowserType.GetDefaultArgs());
            }

            Assert.Contains(TestConstants.IsFirefox ? "-headless" : "--headless", BrowserType.GetDefaultArgs());
            Assert.DoesNotContain(
                TestConstants.IsFirefox ? "-headless" : "--headless",
                BrowserType.GetDefaultArgs(new BrowserArgOptions { Headless = false }));

            Assert.Contains(
                TestConstants.IsFirefox ? "foo" : "--user-data-dir=\"foo\"",
                BrowserType.GetDefaultArgs(new BrowserArgOptions { UserDataDir = "foo" }));

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.defaultArguments</playwright-describe>
        ///<playwright-it>should filter out ignored default arguments</playwright-it>
        [Retry]
        public async Task ShouldFilterOutIgnoredDefaultArguments()
        {
            string[] defaultArgsWithoutUserDataDir = BrowserType.GetDefaultArgs(TestConstants.GetDefaultBrowserOptions());

            var withUserDataDirOptions = TestConstants.GetDefaultBrowserOptions();
            withUserDataDirOptions.UserDataDir = "fake-profile";
            string[] defaultArgsWithUserDataDir = BrowserType.GetDefaultArgs(withUserDataDirOptions);

            var launchOptions = TestConstants.GetDefaultBrowserOptions();
            launchOptions.UserDataDir = "fake-profile";
            launchOptions.IgnoreDefaultArgs = true;
            launchOptions.IgnoredDefaultArgs = defaultArgsWithUserDataDir.Where(x => !defaultArgsWithoutUserDataDir.Contains(x)).ToArray();

            using var browserApp = await BrowserType.LaunchBrowserAppAsync(launchOptions);

            Assert.DoesNotContain("fake-profile", browserApp.Process.StartInfo.Arguments);
        }
    }
    */
}
