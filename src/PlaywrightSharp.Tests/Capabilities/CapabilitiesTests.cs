using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Capabilities
{
    ///<playwright-file>capabilities.spec.js</playwright-file>
    ///<playwright-describe>Capabilities</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class CapabilitiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public CapabilitiesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>capabilities.spec.js</playwright-file>
        ///<playwright-describe>Capabilities</playwright-describe>
        ///<playwright-it>Web Assembly should work</playwright-it>
        [Retry]
        public async Task WebAssemblyShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/wasm/table2.html");
            Assert.Equal("42, 83", await Page.EvaluateAsync<string>("() => loadTable()"));
        }
    }
}
