using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/pdf.spec.js</playwright-file>
    ///<playwright-describe>Page.pdf</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class PdfTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public PdfTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/pdf.spec.js</playwright-file>
        ///<playwright-describe>Page.pdf</playwright-describe>
        ///<playwright-it>should be able to save file</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToSaveFile()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = options.Args.Prepend("--site-per-process").ToArray();
            await using var browser = await Playwright.LaunchAsync(options);
            var page = await browser.DefaultContext.NewPageAsync();

            string outputFile = Path.Combine(BaseDirectory, "output.pdf");
            var fileInfo = new FileInfo(outputFile);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            await page.GetPdfAsync(outputFile);
            fileInfo = new FileInfo(outputFile);
            Assert.True(new FileInfo(outputFile).Length > 0);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }
    }
}
