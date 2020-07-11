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

namespace PlaywrightSharp.Tests.Playwright
{
    ///<playwright-file>fixtures.spec.js</playwright-file>
    ///<playwright-describe>Fixtures</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class FixturesTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public FixturesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>dumpio option should work with webSocket option</playwright-it>
        [Fact(Skip = "It will always be with websockets")]
        public void DumpioOptionShouldWorkWithWebSocketOption() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should dump browser process stderr</playwright-it>
        [Fact(Skip = "We don't need to test this.")]
        public void ShouldDumpBrowserProcessStderr()
        {
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser when the node process closes</playwright-it>
        [Fact(Skip = "We don't need to test this.")]
        public void ShouldCloseTheBrowserWhenTheConnectedProcessCloses()
        {
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should report browser close signal</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldReportBrowserCloseSignal() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should report browser close signal 2</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldReportBrowserCloseSignal2() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGINT</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGINT() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGTERM</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGTERM() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser on SIGHUP</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGHUP() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should kill the browser on double SIGINT</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnDoubleSIGINT() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should kill the browser on SIGINT + SIGTERM</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGINTAndSIGTERM() { }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should kill the browser on SIGTERM + SIGINT</playwright-it>
        [Fact(Skip = "We don't have a good way to get close signals in .NET")]
        public void ShouldCloseTheBrowserOnSIGTERMAndSIGINT() { }
    }
}
