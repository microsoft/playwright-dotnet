using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.name</playwright-describe>
    [Trait("Category", "chromium")]
    public class NameTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public NameTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.name</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public void ShouldRejectAllPromisesWhenBrowserIsClosed()
            => Assert.Equal(
                TestConstants.Product switch
                {
                    TestConstants.WebkitProduct => "webkit",
                    TestConstants.FirefoxProduct => "firefox",
                    TestConstants.ChromiumProduct => "chromium",
                    _ => null
                },
                Playwright.Name);
    }
}
