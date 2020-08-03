using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.check</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleCheckTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleCheckTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.check</playwright-describe>
        ///<playwright-it>should check the box</playwright-it>
        [Retry]
        public async Task ShouldCheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            var input = await Page.QuerySelectorAsync("input");
            await input.CheckAsync();
            Assert.True(await Page.EvaluateAsync<bool>("() => checkbox.checked"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.check</playwright-describe>
        ///<playwright-it>should uncheck the box</playwright-it>
        [Retry]
        public async Task ShouldUncheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            var input = await Page.QuerySelectorAsync("input");
            await input.UncheckAsync();
            Assert.False(await Page.EvaluateAsync<bool>("() => checkbox.checked"));
        }
    }
}
