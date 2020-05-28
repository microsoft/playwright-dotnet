using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.toString</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandleToStringTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleToStringTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.toString</playwright-describe>
        ///<playwright-it>should work for primitives</playwright-it>
        [Retry]
        public async Task ShouldWorkForPrimitives()
        {
            var numberHandle = await Page.EvaluateHandleAsync("() => 2");
            Assert.Equal("JSHandle:2", numberHandle.ToString());
            var stringHandle = await Page.EvaluateHandleAsync("() => 'a'");
            Assert.Equal("JSHandle:a", stringHandle.ToString());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.toString</playwright-describe>
        ///<playwright-it>should work for complicated objects</playwright-it>
        [Retry]
        public async Task ShouldWorkForComplicatedObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.Equal("JSHandle@object", aHandle.ToString());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.toString</playwright-describe>
        ///<playwright-it>should work for promises</playwright-it>
        [Retry]
        public async Task ShouldWorkForPromises()
        {
            // wrap the promise in an object, otherwise we will await.
            var wrapperHandle = await Page.EvaluateHandleAsync("() => ({ b: Promise.resolve(123)})");
            var bHandle = await wrapperHandle.GetPropertyAsync("b");
            Assert.Equal("JSHandle@promise", bHandle.ToString());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.toString</playwright-describe>
        ///<playwright-it>should work with different subtypes</playwright-it>
        [Retry]
        public async Task ShouldWorkWithDifferentSubtypes()
        {
            Assert.Equal("JSHandle@function", (await Page.EvaluateHandleAsync("(function(){})")).ToString());
            Assert.Equal("JSHandle:12", (await Page.EvaluateHandleAsync("12")).ToString());
            Assert.Equal("JSHandle:True", (await Page.EvaluateHandleAsync("true")).ToString());
            Assert.Equal("JSHandle:undefined", (await Page.EvaluateHandleAsync("undefined")).ToString());
            Assert.Equal("JSHandle:foo", (await Page.EvaluateHandleAsync("\"foo\"")).ToString());
            Assert.Equal("JSHandle@symbol", (await Page.EvaluateHandleAsync("Symbol()")).ToString());
            Assert.Equal("JSHandle@map", (await Page.EvaluateHandleAsync("new Map()")).ToString());
            Assert.Equal("JSHandle@set", (await Page.EvaluateHandleAsync("new Set()")).ToString());
            Assert.Equal("JSHandle@array", (await Page.EvaluateHandleAsync("[]")).ToString());
            Assert.Equal("JSHandle:null", (await Page.EvaluateHandleAsync("null")).ToString());
            Assert.Equal("JSHandle@regexp", (await Page.EvaluateHandleAsync("/foo/")).ToString());
            Assert.Equal("JSHandle@node", (await Page.EvaluateHandleAsync("document.body")).ToString());
            Assert.Equal("JSHandle@date", (await Page.EvaluateHandleAsync("new Date()")).ToString());
            Assert.Equal("JSHandle@weakmap", (await Page.EvaluateHandleAsync("new WeakMap()")).ToString());
            Assert.Equal("JSHandle@weakset", (await Page.EvaluateHandleAsync("new WeakSet()")).ToString());
            Assert.Equal("JSHandle@error", (await Page.EvaluateHandleAsync("new Error()")).ToString());
            Assert.Equal(TestConstants.IsWebKit ? "JSHandle@array" : "JSHandle@typedarray", (await Page.EvaluateHandleAsync("new Int32Array()")).ToString());
            Assert.Equal("JSHandle@proxy", (await Page.EvaluateHandleAsync("new Proxy({}, {})")).ToString());
        }
    }
}
