using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("pdf.spec.js", "should be able to save file")]
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

        [PlaywrightTest("pdf.spec.js", "should only have pdf in chromium")]
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public Task ShouldOnlyHavePdfInChromium()
            => Assert.ThrowsAsync<NotSupportedException>(() => Page.GetPdfAsync());
    }
}
