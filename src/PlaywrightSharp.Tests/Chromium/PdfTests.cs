using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/pdf.spec.js</playwright-file>
    ///<playwright-describe>Page.pdf</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PdfTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PdfTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/pdf.spec.js</playwright-file>
        ///<playwright-describe>Page.pdf</playwright-describe>
        ///<playwright-it>should be able to save file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeAbleToSaveFile()
        {
            string outputFile = Path.Combine(BaseDirectory, "output.pdf");
            var fileInfo = new FileInfo(outputFile);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            await Page.GetPdfAsync(outputFile);
            fileInfo = new FileInfo(outputFile);
            Assert.True(new FileInfo(outputFile).Length > 0);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }
    }
}
