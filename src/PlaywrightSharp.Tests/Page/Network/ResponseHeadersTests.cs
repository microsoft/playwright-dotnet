using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.headers</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ResponseHeadersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.headers</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            Server.SetRoute("/empty.html", (context) =>
            {
                context.Response.Headers["foo"] = "bar";
                return Task.CompletedTask;
            });

            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Contains("bar", response.Headers["foo"]);
        }
    }
}
