using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>pdf.spec.ts</playwright-file>
    [Parallelizable(ParallelScope.Self)]
    public class PdfTests : PageTestEx
    {
        [PlaywrightTest("pdf.spec.ts", "should be able to save file")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldBeAbleToSaveFile()
        {
            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
            string outputFile = Path.Combine(baseDirectory, "output.pdf");
            var fileInfo = new FileInfo(outputFile);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
            await Page.PdfAsync(new() { Path = outputFile, Format = PaperFormat.Letter });
            fileInfo = new FileInfo(outputFile);
            Assert.True(new FileInfo(outputFile).Length > 0);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        [PlaywrightTest("pdf.spec.ts", "should only have pdf in chromium")]
        [Test, SkipBrowserAndPlatform(skipChromium: true)]
        public Task ShouldOnlyHavePdfInChromium()
            => AssertThrowsAsync<NotSupportedException>(() => Page.PdfAsync());
    }
}
