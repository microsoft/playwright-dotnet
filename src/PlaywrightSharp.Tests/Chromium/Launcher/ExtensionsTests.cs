using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium.Launcher
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>extensions</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ExtensionsTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public ExtensionsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>extensions</playwright-describe>
        ///<playwright-it>should return background pages</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReturnBackgroundPages()
        {
            using var userDataDir = new TempDirectory();
            string extensionPath = TestUtils.GetWebServerFile("simple-extension");
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Headless = false;
            options.Args = new[] {
                $"--disable-extensions-except={extensionPath}",
                $"--load-extension={extensionPath}",
            };

            await using var context = await BrowserType.LaunchPersistentContextAsync(userDataDir.Path, options);
            var backgroundPage = ((IChromiumBrowserContext)context).BackgroundPages.Any()
                ? ((IChromiumBrowserContext)context).BackgroundPages.First()
                : (await context.WaitForEventAsync(ContextEvent.BackgroundPage)).Page;

            Assert.NotNull(backgroundPage);
            Assert.Contains(backgroundPage, ((IChromiumBrowserContext)context).BackgroundPages);
            Assert.DoesNotContain(backgroundPage, context.Pages);
        }
    }
}
