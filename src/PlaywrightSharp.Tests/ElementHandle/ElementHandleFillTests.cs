using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.fill</playwright-describe>
    public class ElementHandleFillTests : PlaywrightSharpPageBaseTest
    {
        internal ElementHandleFillTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.fill</playwright-describe>
        ///<playwright-it>should fill input</playwright-it>
        [Fact]
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
        [Fact]
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
