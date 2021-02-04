using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Playwright
{
    // I don't know yet if this will be a valid test for us, we will see when we implement it.
    ///<playwright-file>web.spec.js</playwright-file>
    ///<playwright-describe>Web SDK</playwright-describe>
    public class WebTests : PlaywrightSharpBaseTest
    {
        private IPage Page { get; set; }

        /// <inheritdoc/>
        public WebTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        [Fact(Skip = "It's node.js only")]
        public void InitializeAsync()
        {
        }

        [PlaywrightTest("web.spec.js", "Web SDK", "should navigate")]
        [Fact(Skip = "It's node.js only")]
        public void ShouldNavigate()
        {
        }

        [PlaywrightTest("web.spec.js", "Web SDK", "should receive events")]
        [Fact(Skip = "It's node.js only")]
        public void ShouldReceiveEvents()
        {
        }

        [PlaywrightTest("web.spec.js", "Web SDK", "should take screenshot")]
        [Fact(Skip = "It's node.js only")]
        public void ShouldTakeScreenshot()
        {
        }
    }
}
