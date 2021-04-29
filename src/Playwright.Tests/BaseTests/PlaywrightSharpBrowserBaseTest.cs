using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBaseTest"/>, this base class also creates a new Browser
    /// </summary>
    public class PlaywrightSharpBrowserBaseTest : PlaywrightSharpBaseTest
    {
        internal IBrowser Browser => PlaywrightSharpBrowserLoaderFixture.Browser;

        internal PlaywrightSharpBrowserBaseTest(ITestOutputHelper output) : base(output)
        {
        }
    }
}
