using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.json</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class ResponseJsonTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseJsonTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.json</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("{\"foo\": \"bar\"}", (await response.GetJsonAsync()).RootElement.GetRawText());
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.json</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWorkWithGenerics()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("bar", (await response.GetJsonAsync<TestClass>()).Foo);
        }

        class TestClass
        {
            public string Foo { get; set; }
        }
    }
}
