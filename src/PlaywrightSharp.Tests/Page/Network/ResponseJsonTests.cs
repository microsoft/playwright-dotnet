using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.json</playwright-describe>
    public class ResponseJsonTests : PlaywrightSharpPageBaseTest
    {
        internal ResponseJsonTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.json</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/simple.json");
            Assert.Equal(JsonDocument.Parse("{foo: 'bar'}"), await response.GetJsonAsync());
        }
    }
}
