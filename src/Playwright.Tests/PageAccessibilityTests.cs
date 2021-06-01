using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageAccessibilityTests : PageTestEx
    {
        [PlaywrightTest("page-accessibility.spec.ts", "should work with regular text")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRegularText()
        {
            await Page.SetContentAsync("<div>Hello World</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            StringAssert.Contains("Hello World", JsonSerializer.Serialize(snapshot));
        }
    }
}
