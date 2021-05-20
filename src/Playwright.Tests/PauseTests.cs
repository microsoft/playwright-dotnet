using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PauseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PauseTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task ShouldNotFail()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.PauseAsync();
        }
    }
}
