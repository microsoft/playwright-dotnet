using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>browser.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class BrowserTests : BrowserTestEx
    {
        [PlaywrightTest("browser.spec.ts", "should create new page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCreateNewPage()
        {
            var browser = await Playwright[TestConstants.BrowserName].LaunchAsync();
            var page1 = await browser.NewPageAsync();
            Assert.That(browser.Contexts, Has.Count.EqualTo(1));

            var page2 = await browser.NewPageAsync();
            Assert.AreEqual(2, browser.Contexts.Count);

            await page1.CloseAsync();
            Assert.That(browser.Contexts, Has.Count.EqualTo(1));

            await page2.CloseAsync();
        }

        [PlaywrightTest("browser.spec.ts", "should throw upon second create new page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowUponSecondCreateNewPage()
        {
            var page = await Browser.NewPageAsync();
            var ex = Assert.ThrowsAsync<PlaywrightException>(async () => await page.Context.NewPageAsync());
            await page.CloseAsync();
            StringAssert.Contains("Please use Browser.NewContextAsync()", ex.Message);
        }

        [PlaywrightTest("browser.spec.ts", "version should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void VersionShouldWork()
        {
            string version = Browser.Version;

            if (TestConstants.IsChromium)
            {
                Assert.That(version, Does.Match(new Regex("\\d+\\.\\d+\\.\\d+\\.\\d+")));
            }
            else
            {
                Assert.That(version, Does.Match(new Regex("\\d+\\.\\d+")));
            }
        }
    }
}
