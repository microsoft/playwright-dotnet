using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({javaScriptEnabled})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextPagesTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextPagesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.pages()</playwright-describe>
        ///<playwright-it>should return all of the pages</playwright-it>
        [Fact]
        public async Task ShouldReturnAllOfThePages()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var second = await context.NewPageAsync();

            Assert.Equal(2, context.Pages.Length);
            Assert.Contains(page, context.Pages);
            Assert.Contains(second, context.Pages);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.pages()</playwright-describe>
        ///<playwright-it>should close all belonging pages once closing context</playwright-it>
        [Fact]
        public async Task ShouldCloseAllBelongingPagesOnceClosingContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.NewPageAsync();

            Assert.Single(context.Pages);

            await context.CloseAsync();

            Assert.Empty(context.Pages);
        }
    }
}
