using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/session.spec.js</playwright-file>
    ///<playwright-describe>ChromiumBrowser.newBrowserCDPSession</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumBrowserNewBrowserSessionTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public ChromiumBrowserNewBrowserSessionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/session.spec.js</playwright-file>
        ///<playwright-describe>ChromiumBrowser.newBrowserCDPSession</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWork()
        {
            var session = await ((IChromiumBrowser)Browser).NewBrowserCDPSessionAsync();
            Assert.NotNull(await session.SendAsync<object>("Browser.getVersion"));

            bool gotEvent = false;
            session.MessageReceived += (sender, e) =>
            {
                if (e.Method == "Target.targetCreated")
                {
                    gotEvent = true;
                }
            };

            await session.SendAsync("Target.setDiscoverTargets", new { discover = true });
            Assert.True(gotEvent);
            await session.DetachAsync();
        }
    }
}
