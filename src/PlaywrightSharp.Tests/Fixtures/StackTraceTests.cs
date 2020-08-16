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
    ///<playwright-describe>StackTrace</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class StackTraceTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public StackTraceTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>StackTrace</playwright-describe>
        ///<playwright-it>should report browser close signal</playwright-it>
        [Fact(Skip = "We don't need to test stacktrace")]
        public void CallerFilePath() { }
    }
}
