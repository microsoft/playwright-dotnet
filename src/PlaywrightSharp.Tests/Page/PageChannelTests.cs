using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page channel</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageChannelTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageChannelTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page channel</playwright-describe>
        ///<playwright-it>page should be client stub</playwright-it>
        [Fact(Skip = "Skip CHANNEL")]
        public void PageShouldBeClientStub()
        {
        }
    }
}
