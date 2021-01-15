using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.toString</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandleToStringTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleToStringTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.toString", "should work for primitives")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForPrimitives()
        {
            var numberHandle = await Page.EvaluateHandleAsync("() => 2");
            Assert.Equal("JSHandle@2", numberHandle.ToString());
            var stringHandle = await Page.EvaluateHandleAsync("() => 'a'");
            Assert.Equal("JSHandle@a", stringHandle.ToString());
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.toString", "should work for complicated objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForComplicatedObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.Equal("JSHandle@object", aHandle.ToString());
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.toString", "should work for promises")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForPromises()
        {
            // wrap the promise in an object, otherwise we will await.
            var wrapperHandle = await Page.EvaluateHandleAsync("() => ({ b: Promise.resolve(123)})");
            var bHandle = await wrapperHandle.GetPropertyAsync("b");
            Assert.Equal("JSHandle@promise", bHandle.ToString());
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.toString", "should work with different subtypes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDifferentSubtypes()
        {
            Assert.Equal("JSHandle@function", (await Page.EvaluateHandleAsync("(function(){})")).ToString());
            Assert.Equal("JSHandle@12", (await Page.EvaluateHandleAsync("12")).ToString());
            Assert.Equal("JSHandle@true", (await Page.EvaluateHandleAsync("true")).ToString());
            Assert.Equal("JSHandle@undefined", (await Page.EvaluateHandleAsync("undefined")).ToString());
            Assert.Equal("JSHandle@foo", (await Page.EvaluateHandleAsync("\"foo\"")).ToString());
            Assert.Equal("JSHandle@symbol", (await Page.EvaluateHandleAsync("Symbol()")).ToString());
            Assert.Equal("JSHandle@map", (await Page.EvaluateHandleAsync("new Map()")).ToString());
            Assert.Equal("JSHandle@set", (await Page.EvaluateHandleAsync("new Set()")).ToString());
            Assert.Equal("JSHandle@array", (await Page.EvaluateHandleAsync("[]")).ToString());
            Assert.Equal("JSHandle@null", (await Page.EvaluateHandleAsync("null")).ToString());
            Assert.Equal("JSHandle@regexp", (await Page.EvaluateHandleAsync("/foo/")).ToString());
            // We get the preview faster than in node.
            Assert.Contains((await Page.EvaluateHandleAsync("document.body")).ToString(), new[] { "JSHandle@node", "JSHandle@<body></body>" });
            Assert.Equal("JSHandle@date", (await Page.EvaluateHandleAsync("new Date()")).ToString());
            Assert.Equal("JSHandle@weakmap", (await Page.EvaluateHandleAsync("new WeakMap()")).ToString());
            Assert.Equal("JSHandle@weakset", (await Page.EvaluateHandleAsync("new WeakSet()")).ToString());
            Assert.Equal("JSHandle@error", (await Page.EvaluateHandleAsync("new Error()")).ToString());
            Assert.Equal(TestConstants.IsWebKit ? "JSHandle@array" : "JSHandle@typedarray", (await Page.EvaluateHandleAsync("new Int32Array()")).ToString());
            Assert.Equal("JSHandle@proxy", (await Page.EvaluateHandleAsync("new Proxy({}, {})")).ToString());
        }
    }
}
