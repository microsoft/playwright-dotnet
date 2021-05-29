using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class JSHandleAsElementTests : PageTestEx
    {
        [PlaywrightTest("jshandle-as-element.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => document.body");
            var element = aHandle as IElementHandle;
            Assert.NotNull(element);
        }

        [PlaywrightTest("jshandle-as-element.spec.ts", "should return null for non-elements")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonElements()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 2");
            var element = aHandle as IElementHandle;
            Assert.Null(element);
        }

        [PlaywrightTest("jshandle-as-element.spec.ts", "should return ElementHandle for TextNodes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnElementHandleForTextNodes()
        {
            await Page.SetContentAsync("<div>ee!</div>");
            var aHandle = await Page.EvaluateHandleAsync("() => document.querySelector('div').firstChild");
            var element = aHandle as IElementHandle;
            Assert.NotNull(element);
            Assert.True(await Page.EvaluateAsync<bool>("e => e.nodeType === HTMLElement.TEXT_NODE", element));
        }

        [PlaywrightTest("jshandle-as-element.spec.ts", "should work with nullified Node")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNullifiedNode()
        {
            await Page.SetContentAsync("<section>test</section>");
            await Page.EvaluateAsync("() => delete Node");
            var handle = await Page.EvaluateHandleAsync("() => document.querySelector('section')");
            var element = handle as IElementHandle;
            Assert.NotNull(element);
        }
    }
}
