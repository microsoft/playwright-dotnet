using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
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
