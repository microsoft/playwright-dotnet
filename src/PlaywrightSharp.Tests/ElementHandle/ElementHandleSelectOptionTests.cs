using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.selectOption</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleSelectOptionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleSelectOptionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.selectOption</playwright-describe>
        ///<playwright-it>should select single option</playwright-it>
        [Retry]
        public async Task ShouldSelectSingleOption()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/select.html");
            var select = await Page.QuerySelectorAsync("select");
            await select.SelectOptionAsync("blue");

            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onInput"));
            Assert.Equal(new[] { "blue" }, await Page.EvaluateAsync<string[]>("() => result.onChange"));
        }
    }
}
