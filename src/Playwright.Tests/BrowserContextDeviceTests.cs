using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextDeviceTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-device.spec.ts", "should work")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldWork()
        {
            await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/mobile.html");
            Assert.AreEqual(375, await page.EvaluateAsync<int>("window.innerWidth"));
            StringAssert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should support clicking")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldSupportClicking()
        {
            await using var context = await Browser.NewContextAsync(Playwright.Devices["iPhone 6"]);
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/input/button.html");
            var button = await page.QuerySelectorAsync("button");
            await button.EvaluateAsync("button => button.style.marginTop = '200px'", button);
            await button.ClickAsync();
            Assert.AreEqual("Clicked", await page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should scroll to click")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldScrollToClick()
        {
            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 400,
                    Height = 400,
                },
                DeviceScaleFactor = 1,
                IsMobile = true,
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(Server.Prefix + "/input/scrollable.html");
            var element = await page.QuerySelectorAsync("#button-91");
            await element.ClickAsync();
            Assert.AreEqual("clicked", await element.TextContentAsync());
        }
    }
}
