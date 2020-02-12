using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>Page.evaluateHandle</playwright-describe>
    public class PageEvaluateHandleTests : PlaywrightSharpPageBaseTest
    {
        internal PageEvaluateHandleTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle as an argument</playwright-it>
        [Fact]
        public async Task ShouldAcceptObjectHandleAsAnArgument()
        {
            var navigatorHandle = await Page.EvaluateHandleAsync("() => navigator");
            var text = await Page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
            Assert.Contains("Mozilla", text);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle to primitive types</playwright-it>
        [Fact]
        public async Task ShouldAcceptObjectHandleToPrimitiveTypes()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 5");
            var isFive = await Page.EvaluateAsync<bool>("e => Object.is (e, 5)", aHandle);
            Assert.True(isFive);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should warn on nested object handles</playwright-it>
        [Fact]
        public async Task ShouldWarnOnNestedObjectHandles()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => document.body");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(
                () => Page.EvaluateHandleAsync("opts => opts.elem.querySelector('p')", new { elem = aHandle }));
            Assert.Contains("Are you passing a nested JSHandle?", exception.Message);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle to unserializable value</playwright-it>
        [Fact]
        public async Task ShouldAcceptObjectHandleToUnserializableValue()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => Infinity");
            Assert.True(await Page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle));
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should use the same JS wrappers</playwright-it>
        [Fact]
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
        [Fact]
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
