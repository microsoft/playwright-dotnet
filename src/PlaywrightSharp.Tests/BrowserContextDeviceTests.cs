using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Emulation
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextDeviceTests : PlaywrightSharpBrowserBaseTest
    {
        private readonly BrowserContextOptions _iPhone;

        /// <inheritdoc/>
        public BrowserContextDeviceTests(ITestOutputHelper output) : base(output)
        {
            _iPhone = Playwright.Devices["iPhone 6"];
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should work")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldWork()
        {
            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        [PlaywrightTest("browsercontext-device.spec.ts", "should support clicking")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldSupportClicking()
        {
            await using var context = await Browser.NewContextAsync(_iPhone);
            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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

            await page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            var element = await page.QuerySelectorAsync("#button-91");
            await element.ClickAsync();
            Assert.Equal("clicked", await element.GetTextContentAsync());
        }
    }
}
