using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium.Launcher
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>launcher</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LauncherTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LauncherTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>launcher</playwright-describe>
        ///<playwright-it>should throw with remote-debugging-pipe argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowWithRemoteDebuggingPipeArgument()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { "--remote-debugging-pipe" };

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => BrowserType.LaunchServerAsync(options));
            Assert.Contains("Playwright manages remote debugging connection itself", exception.Message);
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>launcher</playwright-describe>
        ///<playwright-it>should not throw with remote-debugging-port argument</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotThrowWithRemoteDebuggingPortArgument()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = new[] { "--remote-debugging-port=0" };

            await using var browser = await BrowserType.LaunchServerAsync(options);
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>launcher</playwright-describe>
        ///<playwright-it>should open devtools when "devtools: true" option is given</playwright-it>
        [Fact(Skip = "Ignore USES_HOOKS")]
        public void ShouldOpenDevtoolsWhenDevtoolsTrueOptionIsGiven() { }
    }
}
