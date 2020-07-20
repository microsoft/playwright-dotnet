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
    public class BrowserContextJavaScriptEnabledTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextJavaScriptEnabledTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({javaScriptEnabled})</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { JavaScriptEnabled = false }))
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync("data:text/html, <script>var something = 'forbidden'</script>");

                var exception = await Assert.ThrowsAnyAsync<Exception>(async () => await page.EvaluateAsync("something"));

                Assert.Contains(
                    TestConstants.IsWebKit ? "Can\'t find variable: something" : "something is not defined",
                    exception.Message);
            }

            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await page.GoToAsync("data:text/html, <script>var something = 'forbidden'</script>");
                Assert.Equal("forbidden", await page.EvaluateAsync<string>("something"));
            }
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({javaScriptEnabled})</playwright-describe>
        ///<playwright-it>should be able to navigate after disabling javascript</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToNavigateAfterDisablingJavascript()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { JavaScriptEnabled = false });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
        }
    }
}