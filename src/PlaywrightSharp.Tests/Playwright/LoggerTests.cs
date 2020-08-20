using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
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

        ///<playwright-file>logger.spec.js</playwright-file>
        ///<playwright-describe>Logger</playwright-describe>
        ///<playwright-it>should log</playwright-it>
        [Fact(Skip = "We don't test CHANNEL")]
        public void ShouldLog()
        {
        }

        ///<playwright-file>logger.spec.js</playwright-file>
        ///<playwright-describe>Logger</playwright-describe>
        ///<playwright-it>should log context-level</playwright-it>
        [Fact(Skip = "We don't test CHANNEL")]
        public void ShouldLogContextLevel()
        {
        }

    }
}
