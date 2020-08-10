using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Response.buffer</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class ResponseBufferTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResponseBufferTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.buffer</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            Assert.Equal(imageBuffer, await response.GetBodyAsync());
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Response.buffer</playwright-describe>
        ///<playwright-it>should work with compression</playwright-it>
        [Fact]
        public async Task ShouldWorkWithCompression()
        {
            Server.EnableGzip("/pptr.png");
            var response = await Page.GoToAsync(TestConstants.ServerUrl + "/pptr.png");
            byte[] imageBuffer = File.ReadAllBytes(TestUtils.GetWebServerFile("pptr.png"));
            Assert.Equal(imageBuffer, await response.GetBodyAsync());
        }
    }
}
