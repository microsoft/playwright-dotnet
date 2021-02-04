using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>logger.spec.js</playwright-file>
    ///<playwright-describe>Logger</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class LoggerTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public LoggerTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("logger.spec.js", "Logger", "should log")]
        [Fact(Skip = "We don't test CHANNEL")]
        public void ShouldLog()
        {
        }

        [PlaywrightTest("logger.spec.js", "Logger", "should log context-level")]
        [Fact(Skip = "We don't test CHANNEL")]
        public void ShouldLogContextLevel()
        {
        }
    }
}
