using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.exposeBinding</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageExposeBindingTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageExposeBindingTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeBinding</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            BindingSource bindingSource = null;

            await Page.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
            {
                bindingSource = source;
                return a + b;
            });

            int result = await Page.EvaluateAsync<int>("async function () { return add(5, 6); }");

            Assert.Same(Context, bindingSource.Context);
            Assert.Same(Page, bindingSource.Page);
            Assert.Same(Page.MainFrame, bindingSource.Frame);
            Assert.Equal(11, result);
        }
    }
}
