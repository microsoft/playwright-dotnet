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
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>Page.pdf</playwright-describe>
    public class PdfTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PdfTests(ITestOutputHelper output) : base(output)
        {
            DefaultOptions = TestConstants.GetDefaultBrowserOptions();
            DefaultOptions.Args = new[] { "--site-per-process" };
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Page.pdf</playwright-describe>
        ///<playwright-it>should be able to save file</playwright-it>
        [Fact]
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
