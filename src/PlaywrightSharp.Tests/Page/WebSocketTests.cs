using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    // All these tests are bypassed on Playwright
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>WebSocket</playwright-describe>
    public class WebSocketTests : PlaywrightSharpPageBaseTest
    {
        internal WebSocketTests(ITestOutputHelper output) : base(output)
        {
        }

    }
}
