using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BeforeUnloadTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BeforeUnloadTests(ITestOutputHelper output) : base(output)
        {
        }


        [PlaywrightTest("beforeunload.spec.ts", "should run beforeunload if asked for")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunBeforeunloadIfAskedFor()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");

            var dialogTask = newPage.WaitForEventAsync(PageEvent.Dialog);
            var pageClosingTask = newPage.CloseAsync(true);
            var dialog = await dialogTask;
            Assert.Equal(DialogType.BeforeUnload, dialog.Type);
            Assert.Empty(dialog.DefaultValue);
            if (TestConstants.IsChromium)
            {
                Assert.Empty(dialog.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.Equal("Leave?", dialog.Message);
            }
            else
            {
                Assert.Equal("This page is asking you to confirm that you want to leave - data you have entered may not be saved.", dialog.Message);
            }

            await dialog.AcceptAsync();
            await pageClosingTask;
        }

        [PlaywrightTest("beforeunload.spec.ts", "should *not* run beforeunload by default")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRunBeforeunloadByDefault()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            await newPage.CloseAsync();
        }

    }
}
