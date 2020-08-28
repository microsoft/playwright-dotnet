using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium.Launcher
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext</playwright-describe>
        ///<playwright-it>should not create pages automatically</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotCreatePagesAutomatically()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var browserSession = await browser.NewBrowserCDPSessionAsync();

            var targets = new List<JsonElement?>();

            browserSession.MessageReceived += (sender, e) =>
            {
                if (e.Method == "Target.targetCreated" && e.Params.Value.GetProperty("targetInfo").GetProperty("type").GetString() != "browser")
                {
                    targets.Add(e.Params);
                }
            };

            await browserSession.SendAsync("Target.setDiscoverTargets", new { discover = true });
            await browser.NewContextAsync();
            await browser.CloseAsync();
            Assert.Empty(targets);
        }
    }
}
