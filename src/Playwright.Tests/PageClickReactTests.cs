using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageClickReactTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickReactTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should retarget when element is recycled during hit testing")]
        [Fact(Skip = " Skip USES_HOOKS")]
        public void ShouldRetargetWhenElementIsRecycledDuringHitTesting()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should report that selector does not match anymore")]
        [Fact(Skip = " Skip USES_HOOKS")]
        public void ShouldReportThatSelectorDoesNotMatchAnymore()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should retarget when element is recycled before enabled check")]
        [Fact(Skip = " Skip USES_HOOKS")]
        public void ShouldRetargetWhenElementIsRecycledBeforeEnabledCheck()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should not retarget the handle when element is recycled")]
        [Fact(Skip = " Skip USES_HOOKS")]
        public void ShouldNotRetargetTheHandleWhenElementIsRecycled()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should timeout when click opens alert")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWhenClickOpensAlert()
        {
            var dialogTask = Page.WaitForEventAsync(PageEvent.Dialog);

            await Page.SetContentAsync("<div onclick='window.alert(123)'>Click me</div>");

            var exception = await Assert.ThrowsAsync<TimeoutException>(() => Page.ClickAsync("div", new PageClickOptions { Timeout = 3000 }));
            Assert.Contains("Timeout 3000ms exceeded", exception.Message);
            var dialog = await dialogTask;
            await dialog.DismissAsync();
        }

        [PlaywrightTest("page-click-react.spec.ts", "should not retarget when element changes on hover")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRetargetWhenElementChangesOnHover()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/react.html");
            await Page.EvaluateAsync(@"() => {
                renderComponent(e('div', {}, [e(MyButton, { name: 'button1', renameOnHover: true }), e(MyButton, { name: 'button2' })] ));
            }");

            await Page.ClickAsync("text=button1");
            Assert.True(await Page.EvaluateAsync<bool?>("window.button1"));
            Assert.Null(await Page.EvaluateAsync<bool?>("window.button2"));
        }

        [PlaywrightTest("page-click-react.spec.ts", "should not retarget when element is recycled on hover")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRetargetWhenElementIsRecycledOnHover()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/react.html");
            await Page.EvaluateAsync(@"() => {
                function shuffle() {
                    renderComponent(e('div', {}, [e(MyButton, { name: 'button2' }), e(MyButton, { name: 'button1' })] ));
                }
                renderComponent(e('div', {}, [e(MyButton, { name: 'button1', onHover: shuffle }), e(MyButton, { name: 'button2' })] ));
            }");

            await Page.ClickAsync("text=button1");
            Assert.Null(await Page.EvaluateAsync<bool?>("window.button1"));
            Assert.True(await Page.EvaluateAsync<bool?>("window.button2"));
        }

    }
}
