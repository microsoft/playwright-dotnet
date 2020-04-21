using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Input
{
    ///<playwright-file>input.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForFileChooser isMultiple</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForFileChooserIsMultipleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForFileChooserIsMultipleTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser isMultiple</playwright-describe>
        ///<playwright-it>should work for single file pick</playwright-it>
        [Fact]
        public async Task ShouldWorkForSingleFilePick()
        {
            await Page.SetContentAsync("<input type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.False(fileChooser.Multiple);
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser isMultiple</playwright-describe>
        ///<playwright-it>should work for "multiple"</playwright-it>
        [Fact]
        public async Task ShouldWorkForMultiple()
        {
            await Page.SetContentAsync("<input multiple type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.Multiple);
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForFileChooser isMultiple</playwright-describe>
        ///<playwright-it>should work for "webkitdirectory"</playwright-it>
        [Fact]
        public async Task ShouldWorkForWebkitdirectory()
        {
            await Page.SetContentAsync("<input multiple webkitdirectory type=file>");
            var fileChooser = await TaskUtils.WhenAll(
               Page.WaitForEvent<FileChooserEventArgs>(PageEvent.FileChooser),
               Page.ClickAsync("input")
            );
            Assert.True(fileChooser.Multiple);
        }
    }
}
