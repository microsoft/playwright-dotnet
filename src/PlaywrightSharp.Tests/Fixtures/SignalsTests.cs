using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Fixtures
{
    ///<playwright-file>fixtures.spec.js</playwright-file>
    ///<playwright-describe>Signals</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class SignalsTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public SignalsTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should report browser close signal")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal() { }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should report browser close signal 2")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal2() { }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should close the browser on SIGINT")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should close the browser on SIGTERM")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should close the browser on SIGHUP")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should close the browser on double SIGINT")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        [PlaywrightTest("fixtures.spec.js", "Fixtures", "should close the browser on SIGINT + SIGERM")]
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }
    }
}
