using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    // All these tests are bypassed on Playwright
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>WebSocket</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class WebSocketTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WebSocketTests(ITestOutputHelper output) : base(output)
        {
        }
    }
}
