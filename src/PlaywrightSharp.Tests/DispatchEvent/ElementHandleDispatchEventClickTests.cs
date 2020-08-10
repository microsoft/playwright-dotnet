using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.DispatchEvent
{
    ///<playwright-file>dispatchevent.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.dispatchEvent(click)</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleDispatchEventClickTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleDispatchEventClickTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>dispatchevent.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.dispatchEvent(click)</playwright-describe>
        ///<playwright-it>should dispatch click event</playwright-it>
        [Fact]
        public async Task ShouldDispatchClickEvent()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await button.DispatchEventAsync("click");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }
    }
}
