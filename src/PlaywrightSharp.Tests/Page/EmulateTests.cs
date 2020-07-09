using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>Page.emulate</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class EmulateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public EmulateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulate</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var page = await NewPageAsync(new BrowserContextOptions
            {
                Viewport = TestConstants.IPhone.ViewPort,
                UserAgent = TestConstants.IPhone.UserAgent
            });

            await page.GoToAsync(TestConstants.ServerUrl + "/mobile.html");
            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulate</playwright-describe>
        ///<playwright-it>should support clicking</playwright-it>
        [Retry]
        public async Task ShouldSupportClicking()
        {
            var page = await NewPageAsync(new BrowserContextOptions
            {
                Viewport = TestConstants.IPhone.ViewPort,
                UserAgent = TestConstants.IPhone.UserAgent
            });

            await page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await page.QuerySelectorAsync("button");
            await page.EvaluateAsync("button => button.style.marginTop = '200px'", button);
            await button.ClickAsync();
            Assert.Equal("Clicked", await page.EvaluateAsync<string>("result"));
        }
    }
}
