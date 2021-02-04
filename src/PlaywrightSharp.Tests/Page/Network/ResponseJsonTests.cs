using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.json</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ResponseJsonTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseJsonTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("network.spec.js", "Response.json", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal("{\"foo\": \"bar\"}", (await response.GetJsonAsync()).RootElement.GetRawText());
        }

        [PlaywrightTest("network.spec.js", "Response.json", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
