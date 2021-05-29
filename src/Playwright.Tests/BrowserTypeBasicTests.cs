using System.IO;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserTypeBasicTests : PlaywrightTestEx
    {
        [PlaywrightTest("browsertype-basic.spec.ts", "browserType.executablePath should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void BrowserTypeExecutablePathShouldWork() => Assert.True(File.Exists(BrowserType.ExecutablePath));

        [PlaywrightTest("browsertype-basic.spec.ts", "browserType.name should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void BrowserTypeNameShouldWork()
            => Assert.AreEqual(
                TestConstants.BrowserName switch
                {
                    "webkit" => "webkit",
                    "firefox" => "firefox",
                    "chromium" => "chromium",
                    _ => null
                },
                BrowserType.Name);
    }
}
