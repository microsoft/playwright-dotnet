using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("fixtures.spec.js", "StackTrace", "should report browser close signal")]
        [Fact(Skip = "We don't need to test stacktrace")]
        public void CallerFilePath() { }
    }
}
