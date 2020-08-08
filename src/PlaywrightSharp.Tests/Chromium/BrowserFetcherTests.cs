using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Unix;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/launcher.spec.js</playwright-file>
    ///<playwright-describe>BrowserFetcher</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class BrowserFetcherTests : PlaywrightSharpBaseTest, IDisposable
    {
        private readonly TempDirectory _downloadsFolder;

        /// <inheritdoc/>
        public BrowserFetcherTests(ITestOutputHelper output) : base(output)
        {
            _downloadsFolder = new TempDirectory();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _downloadsFolder.Dispose();
        }

        ///<playwright-file>chromium/launcher.spec.js</playwright-file>
        ///<playwright-describe>BrowserFetcher</playwright-describe>
        ///<playwright-it>should download and extract linux binary</playwright-it>
        [Fact(Skip = "We don't need to test this.")]
        public void ShouldDownloadAndExtractLinuxBinary()
        {
        }
    }
}
