using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests.Firefox
{
    ///<playwright-file>firefox/launcher.spec.ts</playwright-file>
    ///<playwright-describe>launcher</playwright-describe>
    [Parallelizable(ParallelScope.Self)]
    public class FirefoxLauncherTests : PlaywrightTestEx
    {
        [PlaywrightTest("firefox/launcher.spec.ts", "should pass firefox user preferences")]
        [Test, SkipBrowserAndPlatform(skipChromium: true, skipWebkit: true)]
        public async Task ShouldPassFirefoxUserPreferences()
        {
            var firefoxUserPrefs = new Dictionary<string, object>
            {
                ["network.proxy.type"] = 1,
                ["network.proxy.http"] = "127.0.0.1",
                ["network.proxy.http_port"] = 333,
            };

            await using var browser = await BrowserType.LaunchAsync(new BrowserTypeLaunchOptions { FirefoxUserPrefs = firefoxUserPrefs });
            var page = await browser.NewPageAsync();
            var exception = await AssertThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://example.com"));

            StringAssert.Contains("NS_ERROR_PROXY_CONNECTION_REFUSED", exception.Message);
        }
    }
}
