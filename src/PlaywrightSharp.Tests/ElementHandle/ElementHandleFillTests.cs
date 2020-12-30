using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.fill</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleFillTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleFillTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.fill</playwright-describe>
        ///<playwright-it>should fill input</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillInput()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            var handle = await Page.QuerySelectorAsync("input");
            await handle.FillAsync("some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.fill</playwright-describe>
        ///<playwright-it>should fill input when Node is removed</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFillInputWhenNodeIsRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var handle = await Page.QuerySelectorAsync("input");
            await handle.FillAsync("some value");
            Assert.Equal("some value", await Page.EvaluateAsync<string>("() => result"));
        }
    }
}
