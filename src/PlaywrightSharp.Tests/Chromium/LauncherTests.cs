using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LauncherTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LauncherTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("chromium/launcher.spec.ts", "should return background pages")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReturnBackgroundPages()
        {
            using var userDataDir = new TempDirectory();
            string extensionPath = TestUtils.GetWebServerFile("simple-extension");
            string[] args = new[] {
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}",
            };

            await using var context = await BrowserType.LaunchDefaultPersistentContext(userDataDir.Path, args, null, headless: false);
            var backgroundPage = ((IChromiumBrowserContext)context).BackgroundPages.Any()
                ? ((IChromiumBrowserContext)context).BackgroundPages.First()
                : (await context.WaitForEventAsync(ContextEvent.BackgroundPage)).Page;

            Assert.NotNull(backgroundPage);
            Assert.Contains(backgroundPage, ((IChromiumBrowserContext)context).BackgroundPages);
            Assert.DoesNotContain(backgroundPage, context.Pages);
        }

        [PlaywrightTest("chromium/launcher.spec.ts", "should not create pages automatically")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotCreatePagesAutomatically()
        {
            await using var browser = await BrowserType.LaunchDefaultAsync();
            var browserSession = await ((IChromiumBrowser)browser).NewBrowserCDPSessionAsync();

            var targets = new List<JsonElement?>();

            browserSession.MessageReceived += (_, e) =>
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
