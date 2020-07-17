using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext.exposeBinding</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextExposeBindingTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextExposeBindingTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.exposeBinding</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var context = await Browser.NewContextAsync();
            BindingSource bindingSource = null;

            await Context.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
            {
                bindingSource = source;
                return a * b;
            });

            var page = await context.NewPageAsync();
            int result = await page.EvaluateAsync<int>(@"async function() {
                return await add(5, 6);
            }");

            Assert.Same(context, bindingSource.Context);
            Assert.Same(page, bindingSource.Page);
            Assert.Same(page.MainFrame, bindingSource.Frame);

            Assert.Equal(11, result);
        }
    }
}
