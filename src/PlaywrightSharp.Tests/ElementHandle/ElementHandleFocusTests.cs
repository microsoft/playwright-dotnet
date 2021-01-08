using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.focus</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleFocusTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleFocusTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.focus</playwright-describe>
        ///<playwright-it>should focus a button</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFocusAButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");

            Assert.False(await button.EvaluateAsync<bool?>("button => document.activeElement === button"));
            await button.FocusAsync();
            Assert.True(await button.EvaluateAsync<bool?>("button => document.activeElement === button"));
        }
    }
}
