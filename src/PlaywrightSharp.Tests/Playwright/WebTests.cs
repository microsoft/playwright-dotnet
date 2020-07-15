using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
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

        ///<playwright-file>web.spec.js</playwright-file>
        ///<playwright-describe>Web SDK</playwright-describe>
        ///<playwright-it>should navigate</playwright-it>
        [Fact(Skip = "It's node.js only")]
        public void ShouldNavigate()
        {
        }

        ///<playwright-file>web.spec.js</playwright-file>
        ///<playwright-describe>Web SDK</playwright-describe>
        ///<playwright-it>should receive events</playwright-it>
        [Fact(Skip = "It's node.js only")]
        public void ShouldReceiveEvents()
        {
        }

        ///<playwright-file>web.spec.js</playwright-file>
        ///<playwright-describe>Web SDK</playwright-describe>
        ///<playwright-it>should take screenshot</playwright-it>
        [Fact(Skip = "It's node.js only")]
        public void ShouldTakeScreenshot()
        {
        }
    }
}
