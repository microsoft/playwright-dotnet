using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextTimezoneIdTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextTimezoneIdTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await using var browser = await Playwright[TestConstants.Product].LaunchDefaultAsync();

            const string func = "() => new Date(1479579154987).toString()";
            await using (var context = await browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "America/Jamaica" }))
            {
                var page = await context.NewPageAsync();
                string result = await page.EvaluateAsync<string>(func);
                Assert.Equal(
                    "Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)",
                    result);
            }

            await using (var context = await browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "Pacific/Honolulu" }))
            {
                var page = await context.NewPageAsync();
                Assert.Equal(
                    "Sat Nov 19 2016 08:12:34 GMT-1000 (Hawaii-Aleutian Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }

            await using (var context = await browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "America/Buenos_Aires" }))
            {
                var page = await context.NewPageAsync();
                Assert.Equal(
                    "Sat Nov 19 2016 15:12:34 GMT-0300 (Argentina Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }

            await using (var context = await browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "Europe/Berlin" }))
            {
                var page = await context.NewPageAsync();
                Assert.Equal(
                    "Sat Nov 19 2016 19:12:34 GMT+0100 (Central European Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }
        }

        [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should throw for invalid timezone IDs")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForInvalidTimezoneId()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "Foo/Bar" }))
            {
                var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => context.NewPageAsync());
                Assert.Contains("Invalid timezone ID: Foo/Bar", exception.Message);
            }

            await using (var context = await Browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "Baz/Qux" }))
            {
                var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => context.NewPageAsync());
                Assert.Contains("Invalid timezone ID: Baz/Qux", exception.Message);
            }
        }

        [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should work for multiple pages sharing same process")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMultiplePagesSharingSameProcess()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { TimezoneId = "Europe/Moscow" });

            var page = await context.NewPageAsync();
            await page.GotoAsync(TestConstants.EmptyPage);

            await TaskUtils.WhenAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await TaskUtils.WhenAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));
        }
    }
}
