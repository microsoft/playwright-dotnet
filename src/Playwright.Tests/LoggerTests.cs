using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LoggerTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LoggerTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("logger.spec.ts", "should log")]
        [Fact(Skip = "We don't test CHANNEL")]
        public void ShouldLog()
        {
        }

        [PlaywrightTest("logger.spec.ts", "should log context-level")]
        [Fact(Skip = "We don't test CHANNEL")]
        public void ShouldLogContextLevel()
        {
        }
    }
}
