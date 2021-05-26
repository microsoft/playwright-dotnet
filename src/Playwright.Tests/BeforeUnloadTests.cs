using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
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
            await newPage.GotoAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");

            var dialogEvent = new TaskCompletionSource<IDialog>();
            newPage.Dialog += (_, dialog) => dialogEvent.TrySetResult(dialog);

            var pageClosingTask = newPage.CloseAsync(new PageCloseOptions { RunBeforeUnload = true });
            var dialog = await dialogEvent.Task;
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
                Assert.Contains("This page is asking you to confirm that you want to leave", dialog.Message);
            }

            await dialog.AcceptAsync();
            await pageClosingTask;
        }

        [PlaywrightTest("beforeunload.spec.ts", "should *not* run beforeunload by default")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRunBeforeunloadByDefault()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GotoAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            await newPage.CloseAsync();
        }

    }
}
