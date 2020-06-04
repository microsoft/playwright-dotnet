using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>Page.evaluateHandle</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEvaluateHandleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEvaluateHandleTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle as an argument</playwright-it>
        [Retry]
        public async Task ShouldAcceptObjectHandleAsAnArgument()
        {
            var navigatorHandle = await Page.EvaluateHandleAsync("() => navigator");
            string text = await Page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
            Assert.Contains("Mozilla", text);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle to primitive types</playwright-it>
        [Retry]
        public async Task ShouldAcceptObjectHandleToPrimitiveTypes()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 5");
            bool isFive = await Page.EvaluateAsync<bool>("e => Object.is (e, 5)", aHandle);
            Assert.True(isFive);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should warn on nested object handles</playwright-it>
        [Retry]
        public async Task ShouldWarnOnNestedObjectHandles()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => document.body");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(
                () => Page.EvaluateHandleAsync("opts => opts.elem.querySelector('p')", new { elem = aHandle }));
            Assert.Contains("Are you passing a nested JSHandle?", exception.Message);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle to unserializable value</playwright-it>
        [Retry]
        public async Task ShouldAcceptObjectHandleToUnserializableValue()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => Infinity");
            Assert.True(await Page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle));
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should use the same JS wrappers</playwright-it>
        [Retry]
        public async Task ShouldUseTheSameJSWrappers()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => {
                window.FOO = 123;
                return window;
            }");
            Assert.Equal(123, await Page.EvaluateAsync<int>("e => e.FOO", aHandle));
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should work with primitives</playwright-it>
        [Retry]
        public async Task ShouldWorkWithPrimitives()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => {
                window.FOO = 123;
                return window;
            }");
            Assert.Equal(123, await Page.EvaluateAsync<int>("e => e.FOO", aHandle));
        }
    }
}
