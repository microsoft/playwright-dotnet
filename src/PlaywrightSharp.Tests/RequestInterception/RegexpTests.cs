using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>regexp</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RegexpTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public RegexpTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("interception.spec.js", "glob", "should work with regular expression passed from a different context")]
        [Fact(Skip = "We don't need to test Regex contexts")]
        public void ShouldWorkWithRegularExpressionPassedFromADifferentContext()
        {
        }
    }
}
