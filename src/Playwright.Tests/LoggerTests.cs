using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class LoggerTests : PlaywrightTestEx
    {
        [PlaywrightTest("logger.spec.ts", "should log")]
        [Test, Ignore("We don't test CHANNEL")]
        public void ShouldLog()
        {
        }

        [PlaywrightTest("logger.spec.ts", "should log context-level")]
        [Test, Ignore("We don't test CHANNEL")]
        public void ShouldLogContextLevel()
        {
        }
    }
}
