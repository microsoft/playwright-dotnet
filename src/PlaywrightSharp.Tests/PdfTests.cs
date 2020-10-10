using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>pdf.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PdfTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PdfTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>pdf.spec.js</playwright-file>
        ///<playwright-it>should be able to save file</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToSaveFile()
        {
            string outputFile = Path.Combine(BaseDirectory, "output.pdf");
            var fileInfo = new FileInfo(outputFile);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            await Page.GetPdfAsync(outputFile, format: PaperFormat.Letter);
            fileInfo = new FileInfo(outputFile);
            Assert.True(new FileInfo(outputFile).Length > 0);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        ///<playwright-file>pdf.spec.js</playwright-file>
        ///<playwright-it>should only have pdf in chromium</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public Task ShouldOnlyHavePdfInChromium()
            => Assert.ThrowsAsync<NotSupportedException>(() => Page.GetPdfAsync());
    }
}
