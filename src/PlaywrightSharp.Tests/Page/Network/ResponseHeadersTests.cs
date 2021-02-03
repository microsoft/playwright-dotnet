using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.headers</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ResponseHeadersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("network.spec.js", "Response.headers", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
