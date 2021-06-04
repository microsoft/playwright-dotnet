using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageCheckTests : PageTestEx
    {
        [PlaywrightTest("page-check.spec.ts", "should check the box")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            await Page.CheckAsync("input");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should not check the checked box")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotCheckTheCheckedBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
            await Page.CheckAsync("input");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should uncheck the box")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldUncheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
            await Page.UncheckAsync("input");
            Assert.False(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should check the box by label")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxByLabel()
        {
            await Page.SetContentAsync("<label for='checkbox'><input id='checkbox' type='checkbox'></input></label>");
            await Page.CheckAsync("label");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should check the box outside label")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxOutsideLabel()
        {
            await Page.SetContentAsync("<label for='checkbox'>Text</label><div><input id='checkbox' type='checkbox'></input></div>");
            await Page.CheckAsync("label");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should check the box inside label w/o id")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxInsideLabelWoId()
        {
            await Page.SetContentAsync("<label>Text<span><input id='checkbox' type='checkbox'></input></span></label>");
            await Page.CheckAsync("label");
            Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should check radio")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckRadio()
        {
            await Page.SetContentAsync(@"
                <input type='radio'>one</input>
                <input id='two' type='radio'>two</input>
                <input type='radio'>three</input>");
            await Page.CheckAsync("#two");
            Assert.True(await Page.EvaluateAsync<bool?>("two.checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "should check the box by aria role")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCheckTheBoxByAriaRole()
        {
            await Page.SetContentAsync(@"
                <div role='checkbox' id='checkbox'>CHECKBOX</div>
                <script>
                checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
                </script>");
            await Page.CheckAsync("div");
            Assert.AreEqual("true", await Page.EvaluateAsync<string>("checkbox.getAttribute('aria-checked')"));
        }

        [PlaywrightTest("page-check.spec.ts", "trial run should not check")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task TrialRunShouldNotCheck()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            await Page.CheckAsync("input", new PageCheckOptions { Trial = true });
            Assert.False(await Page.EvaluateAsync<bool>("window['checkbox'].checked"));
        }

        [PlaywrightTest("page-check.spec.ts", "trial run should not uncheck")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task TrialRunShouldNotUncheck()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
            await Page.CheckAsync("input", new PageCheckOptions { Trial = true });
            Assert.True(await Page.EvaluateAsync<bool>("window['checkbox'].checked"));
        }
    }
}
