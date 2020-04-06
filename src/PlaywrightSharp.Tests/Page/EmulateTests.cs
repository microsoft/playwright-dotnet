using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>Page.emulate</playwright-describe>
    public class EmulateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public EmulateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulate</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var page = await NewPageAsync(new BrowserContextOptions
            {
                Viewport = TestConstants.IPhone.ViewPort,
                UserAgent = TestConstants.IPhone.UserAgent
            });

            Assert.Equal(375, await page.EvaluateAsync<int>("window.innerWidth"));
            Assert.Contains("iPhone", await page.EvaluateAsync<string>("navigator.userAgent"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulate</playwright-describe>
        ///<playwright-it>should support clicking</playwright-it>
        [Fact]
        public async Task ShouldSupportClicking()
        {
            var page = await NewPageAsync(new BrowserContextOptions
            {
                Viewport = TestConstants.IPhone.ViewPort,
                UserAgent = TestConstants.IPhone.UserAgent
            });

            await page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.style.marginTop = '200px'", button);
            await button.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }
    }
}
