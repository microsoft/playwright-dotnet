using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BeforeUnloadTests : PageTestEx
    {

        [PlaywrightTest("beforeunload.spec.ts", "should run beforeunload if asked for")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunBeforeunloadIfAskedFor()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GotoAsync(Server.Prefix + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");

            var dialogEvent = new TaskCompletionSource<IDialog>();
            newPage.Dialog += (_, dialog) => dialogEvent.TrySetResult(dialog);

            var pageClosingTask = newPage.CloseAsync(new() { RunBeforeUnload = true });
            var dialog = await dialogEvent.Task;
            Assert.AreEqual(DialogType.BeforeUnload, dialog.Type);
            Assert.IsEmpty(dialog.DefaultValue);
            if (TestConstants.IsChromium)
            {
                Assert.IsEmpty(dialog.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.AreEqual("Leave?", dialog.Message);
            }
            else
            {
                StringAssert.Contains("This page is asking you to confirm that you want to leave", dialog.Message);
            }

            await dialog.AcceptAsync();
            await pageClosingTask;
        }

        [PlaywrightTest("beforeunload.spec.ts", "should *not* run beforeunload by default")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRunBeforeunloadByDefault()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GotoAsync(Server.Prefix + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            await newPage.CloseAsync();
        }

    }
}
