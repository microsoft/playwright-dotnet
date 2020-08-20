using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserType
{
    ///<playwright-file>proxy.spec.js</playwright-file>
    ///<playwright-describe>Socks Proxy</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class SocksProxyTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public SocksProxyTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>proxy.spec.js</playwright-file>
        ///<playwright-describe>Socks Proxy</playwright-describe>
        ///<playwright-it>should use proxy</playwright-it>
        [Fact(Skip = "We don't need to test socks implementation in our library")]
        public void ShouldUseProxy()
        {
        }
    }
}
