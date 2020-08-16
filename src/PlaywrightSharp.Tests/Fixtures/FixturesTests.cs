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
    ///<playwright-describe>Fixtures</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class FixturesTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public FixturesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>fixtures.spec.js</playwright-file>
        ///<playwright-describe>Fixtures</playwright-describe>
        ///<playwright-it>should close the browser when the node process closes</playwright-it>
        [Fact(Skip = "We don't need to test process handling")]
        public void ShouldCloseTheBrowserWhenTheNodeProcessCloses() { }
    }
}
