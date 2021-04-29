using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>pdf.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PdfTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PdfTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("pdf.spec.ts", "should be able to save file")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToSaveFile()
        {
            string outputFile = Path.Combine(BaseDirectory, "output.pdf");
            var fileInfo = new FileInfo(outputFile);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            await Page.PdfAsync(outputFile, format: PaperFormat.Letter);
            fileInfo = new FileInfo(outputFile);
            Assert.True(new FileInfo(outputFile).Length > 0);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        [PlaywrightTest("pdf.spec.ts", "should only have pdf in chromium")]
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public Task ShouldOnlyHavePdfInChromium()
            => Assert.ThrowsAsync<NotSupportedException>(() => Page.PdfAsync());
    }
}
