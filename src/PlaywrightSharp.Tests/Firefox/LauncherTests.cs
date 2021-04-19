using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Firefox
{
    ///<playwright-file>firefox/launcher.spec.ts</playwright-file>
    ///<playwright-describe>launcher</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LauncherTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LauncherTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("firefox/launcher.spec.ts", "should pass firefox user preferences")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipWebkit: true)]
        public async Task ShouldPassFirefoxUserPreferences()
        {
            var firefoxUserPrefs = new Dictionary<string, object>
            {
                ["network.proxy.type"] = 1,
                ["network.proxy.http"] = "127.0.0.1",
                ["network.proxy.http_port"] = 333,
            };

            await using var browser = await BrowserType.LaunchAsync(firefoxUserPrefs: firefoxUserPrefs);
            var page = await browser.NewPageAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync("http://example.com"));

            Assert.Contains("NS_ERROR_PROXY_CONNECTION_REFUSED", exception.Message);
        }
    }
}
