using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.asElement</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandleAsElementTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleAsElementTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.asElement</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => document.body");
            var element = aHandle as IElementHandle;
            Assert.NotNull(element);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.asElement</playwright-describe>
        ///<playwright-it>should return null for non-elements</playwright-it>
        [Fact]
        public async Task ShouldReturnNullForNonElements()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 2");
            var element = aHandle as IElementHandle;
            Assert.Null(element);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.asElement</playwright-describe>
        ///<playwright-it>should return ElementHandle for TextNodes</playwright-it>
        [Fact]
        public async Task ShouldReturnElementHandleForTextNodes()
        {
            await Page.SetContentAsync("<div>ee!</div>");
            var aHandle = await Page.EvaluateHandleAsync("() => document.querySelector('div').firstChild");
            var element = aHandle as IElementHandle;
            Assert.NotNull(element);
            Assert.True(await Page.EvaluateAsync<bool>("e => e.nodeType === HTMLElement.TEXT_NODE", element));
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.asElement</playwright-describe>
        ///<playwright-it>should work with nullified Node</playwright-it>
        [Fact]
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
