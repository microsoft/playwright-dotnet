using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>dialog.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Dialog</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DialogTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public DialogTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should fire</playwright-it>
        [Retry]
        public async Task ShouldFire()
        {
            Page.Dialog += async (sender, e) =>
            {
                Assert.Equal(DialogType.Alert, e.Dialog.DialogType);
                Assert.Equal(string.Empty, e.Dialog.DefaultValue);
                Assert.Equal("yo", e.Dialog.Message);

                await e.Dialog.AcceptAsync();
            };

            await Page.EvaluateAsync("alert('yo');");
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should allow accepting prompts</playwright-it>
        [Retry]
        public async Task ShouldAllowAcceptingPrompts()
        {
            Page.Dialog += async (sender, e) =>
            {
                Assert.Equal(DialogType.Prompt, e.Dialog.DialogType);
                Assert.Equal("yes.", e.Dialog.DefaultValue);
                Assert.Equal("question?", e.Dialog.Message);

                await e.Dialog.AcceptAsync("answer!");
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?', 'yes.')");
            Assert.Equal("answer!", result);
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should dismiss the prompt</playwright-it>
        [Retry]
        public async Task ShouldDismissThePrompt()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?')");
            Assert.Null(result);
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should accept the confirm prompt</playwright-it>
        [Retry]
        public async Task ShouldAcceptTheConfirmPrompts()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.AcceptAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("confirm('boolean?')");
            Assert.True(result);
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should dismiss the confirm prompt</playwright-it>
        [Retry]
        public async Task ShouldDismissTheConfirmPrompt()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
            Assert.False(result);
        }
    }
}
