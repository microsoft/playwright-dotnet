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

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should report browser close signal</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should report browser close signal 2</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldReportBrowserCloseSignal2() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGINT</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGTERM</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGHUP</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on double SIGINT</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGINT + SIGERM</playwright-it>
        [Fact(Skip = "We don't need to test signals")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }
    }
}
