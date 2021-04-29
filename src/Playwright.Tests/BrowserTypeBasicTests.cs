using System.IO;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserTypeBasicTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public BrowserTypeBasicTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsertype-basic.spec.ts", "browserType.executablePath should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void BrowserTypeExecutablePathShouldWork() => Assert.True(File.Exists(BrowserType.ExecutablePath));

        [PlaywrightTest("browsertype-basic.spec.ts", "browserType.name should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void BrowserTypeNameShouldWork()
            => Assert.Equal(
                TestConstants.Product switch
                {
                    TestConstants.WebkitProduct => "webkit",
                    TestConstants.FirefoxProduct => "firefox",
                    TestConstants.ChromiumProduct => "chromium",
                    _ => null
                },
                BrowserType.Name);
    }
}
