using System.IO;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.executablePath</playwright-describe>
    [Collection(TestConstants.TestFixtureCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class ExecutablePathTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public ExecutablePathTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.executablePath</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public void ShouldRejectAllPromisesWhenBrowserIsClosed() => Assert.True(File.Exists(BrowserType.ExecutablePath));
    }
}
