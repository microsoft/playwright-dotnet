using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.defaultArguments</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class DefaultArgumentsTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public DefaultArgumentsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.defaultArguments</playwright-describe>
        ///<playwright-it>should return the default arguments</playwright-it>
        [Fact]
        public void ShouldReturnTheDefaultArguments()
        {
            if (TestConstants.IsChromium)
            {
                Assert.Contains("--no-first-run", Playwright.GetDefaultArgs());
            }

            Assert.Contains(TestConstants.IsFirefox ? "-headless" : "--headless", Playwright.GetDefaultArgs());
            Assert.DoesNotContain(
                TestConstants.IsFirefox ? "-headless" : "--headless",
                Playwright.GetDefaultArgs(new BrowserArgOptions { Headless = false }));

            Assert.DoesNotContain(
                TestConstants.IsFirefox ? "foo" : "--user-data-dir=foo",
                Playwright.GetDefaultArgs(new BrowserArgOptions { UserDataDir = "foo" }));

        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.defaultArguments</playwright-describe>
        ///<playwright-it>should filter out ignored default arguments</playwright-it>
        [Fact]
        public async Task ShouldFilterOutIgnoredDefaultArguments()
        {
            string[] defaultArgsWithoutUserDataDir = Playwright.GetDefaultArgs(TestConstants.DefaultBrowserOptions);

            var withUserDataDirOptions = TestConstants.DefaultBrowserOptions;
            withUserDataDirOptions.UserDataDir = "fake-profile";
            string[] defaultArgsWithUserDataDir = Playwright.GetDefaultArgs(withUserDataDirOptions);

            var launchOptions = TestConstants.DefaultBrowserOptions;
            launchOptions.UserDataDir = "fake-profile";
            launchOptions.IgnoreDefaultArgs = true;
            launchOptions.IgnoredDefaultArgs = defaultArgsWithUserDataDir.Where(x => !defaultArgsWithoutUserDataDir.Contains(x)).ToArray();

            using var browserApp = await Playwright.LaunchBrowserAppAsync(launchOptions);

            Assert.DoesNotContain("fake-profile", browserApp.Process.StartInfo.Arguments);
        }
    }
}
