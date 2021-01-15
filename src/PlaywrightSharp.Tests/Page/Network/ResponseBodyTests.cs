using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.body</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ResponseBodyTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseBodyTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("network.spec.js", "Response.body", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            Assert.Equal(imageBuffer, await response.GetBodyAsync());
        }

        [PlaywrightTest("network.spec.js", "Response.body", "should work with compression")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCompression()
        {
            Server.EnableGzip("/pptr.png");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            Assert.Equal(imageBuffer, await response.GetBodyAsync());
        }
    }
}
