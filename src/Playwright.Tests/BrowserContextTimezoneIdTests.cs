using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextTimezoneIdTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await using var browser = await Playwright[TestConstants.BrowserName].LaunchAsync();

            const string func = "() => new Date(1479579154987).toString()";
            await using (var context = await browser.NewContextAsync(new() { TimezoneId = "America/Jamaica" }))
            {
                var page = await context.NewPageAsync();
                string result = await page.EvaluateAsync<string>(func);
                Assert.AreEqual(
                    "Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)",
                    result);
            }

            await using (var context = await browser.NewContextAsync(new() { TimezoneId = "Pacific/Honolulu" }))
            {
                var page = await context.NewPageAsync();
                Assert.AreEqual(
                    "Sat Nov 19 2016 08:12:34 GMT-1000 (Hawaii-Aleutian Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }

            await using (var context = await browser.NewContextAsync(new() { TimezoneId = "America/Buenos_Aires" }))
            {
                var page = await context.NewPageAsync();
                Assert.AreEqual(
                    "Sat Nov 19 2016 15:12:34 GMT-0300 (Argentina Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }

            await using (var context = await browser.NewContextAsync(new() { TimezoneId = "Europe/Berlin" }))
            {
                var page = await context.NewPageAsync();
                Assert.AreEqual(
                    "Sat Nov 19 2016 19:12:34 GMT+0100 (Central European Standard Time)",
                    await page.EvaluateAsync<string>(func));
            }
        }

        [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should throw for invalid timezone IDs")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForInvalidTimezoneId()
        {
            await using (var context = await Browser.NewContextAsync(new() { TimezoneId = "Foo/Bar" }))
            {
                var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => context.NewPageAsync());
                StringAssert.Contains("Invalid timezone ID: Foo/Bar", exception.Message);
            }

            await using (var context = await Browser.NewContextAsync(new() { TimezoneId = "Baz/Qux" }))
            {
                var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => context.NewPageAsync());
                StringAssert.Contains("Invalid timezone ID: Baz/Qux", exception.Message);
            }
        }

        [PlaywrightTest("browsercontext-timezone-id.spec.ts", "should work for multiple pages sharing same process")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForMultiplePagesSharingSameProcess()
        {
            await using var context = await Browser.NewContextAsync(new() { TimezoneId = "Europe/Moscow" });

            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            await TaskUtils.WhenAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));

            await TaskUtils.WhenAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => window.open(url)", Server.EmptyPage));
        }
    }
}
