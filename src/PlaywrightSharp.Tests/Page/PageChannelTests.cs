using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("page.spec.js", "Page channel", "page should be client stub")]
        [Fact(Skip = "Skip CHANNEL")]
        public void PageShouldBeClientStub()
        {
        }
    }
}
