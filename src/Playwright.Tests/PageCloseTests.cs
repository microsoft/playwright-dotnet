using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageCloseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageCloseTests(ITestOutputHelper output) : base(output)
        {
        }

    }
}
