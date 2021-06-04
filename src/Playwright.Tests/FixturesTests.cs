using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>fixtures.spec.ts</playwright-file>
    ///<playwright-describe>Fixtures</playwright-describe>
    [Parallelizable(ParallelScope.Self)]
    public class FixturesTests : PlaywrightTestEx
    {
        [PlaywrightTest("fixtures.spec.ts", "should close the browser when the node process closes")]
        [Test, Ignore("We don't need to test process handling")]
        public void ShouldCloseTheBrowserWhenTheNodeProcessCloses() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should report browser close signal")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should report browser close signal 2")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal2() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGINT")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGTERM")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGHUP")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on double SIGINT")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        [PlaywrightTest("fixtures.spec.ts", "fixtures", "should close the browser on SIGINT + SIGERM")]
        [Test, Ignore("We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }

        [PlaywrightTest("fixtures.spec.ts", "should report browser close signal")]
        [Test, Ignore("We don't need to test stacktrace")]
        public void CallerFilePath() { }
    }
}
