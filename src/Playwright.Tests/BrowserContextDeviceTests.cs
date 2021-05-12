using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextDeviceTests : PlaywrightSharpBrowserBaseTest
    {
        private readonly BrowserContextOptions _iPhone = TestConstants.iPhone6;

        /// <inheritdoc/>
        public BrowserContextDeviceTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should work")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWork()
        {
            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();

            await page.GotoAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should support clicking")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportClicking()
        {
            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();

            await page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await page.QuerySelectorAsync("button");
            await button.EvaluateAsync("button => button.style.marginTop = '200px'", button);
            await button.ClickAsync();
            Assert.Equal("Clicked", await page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should scroll to click")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldScrollToClick()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 400,
                    Height = 400,
                },
                DeviceScaleFactor = 1,
                IsMobile = true,
            });
            var page = await context.NewPageAsync();

            await page.GotoAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            var element = await page.QuerySelectorAsync("#button-91");
            await element.ClickAsync();
            Assert.Equal("clicked", await element.TextContentAsync());
        }
    }
}
