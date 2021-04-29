using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>fixtures.spec.ts</playwright-file>
    ///<playwright-describe>Fixtures</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FixturesTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public FixturesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("fixtures.spec.ts", "should close the browser when the node process closes")]
        [Fact(Skip = "We don't need to test process handling")]
        public void ShouldCloseTheBrowserWhenTheNodeProcessCloses() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should report browser close signal")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should report browser close signal 2")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal2() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGINT")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGTERM")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGHUP")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on double SIGINT")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGINT + SIGERM")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }

        [PlaywrightTest("fixtures.spec.ts", "should report browser close signal")]
        [Fact(Skip = "We don't need to test stacktrace")]
        public void CallerFilePath() { }
    }
}
