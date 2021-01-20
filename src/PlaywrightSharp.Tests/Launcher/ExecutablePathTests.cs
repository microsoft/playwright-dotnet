using System.IO;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.executablePath</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ExecutablePathTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public ExecutablePathTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("launcher.spec.js", "browserType.executablePath", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void ShouldRejectAllPromisesWhenBrowserIsClosed() => Assert.True(File.Exists(BrowserType.ExecutablePath));
    }
}
