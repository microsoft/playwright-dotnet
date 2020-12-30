using System;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>Page.evaluateHandle</playwright-describe>
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle as an argument</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleAsAnArgument()
        {
            var navigatorHandle = await Page.EvaluateHandleAsync("() => navigator");
            string text = await Page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
            Assert.Contains("Mozilla", text);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle to primitive types</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleToPrimitiveTypes()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 5");
            bool isFive = await Page.EvaluateAsync<bool>("e => Object.is (e, 5)", aHandle);
            Assert.True(isFive);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept nested handle</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptNestedHandle()
        {
            var foo = await Page.EvaluateHandleAsync("() => ({x: 1, y: 'foo'})");
            dynamic result = await Page.EvaluateAsync<ExpandoObject>("({ foo }) => foo", new { foo });

            Assert.Equal(1, result.x);
            Assert.Equal("foo", result.y);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept nested window handle</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptNestedWindowHandle()
        {
            var foo = await Page.EvaluateHandleAsync("() => window");
            Assert.True(await Page.EvaluateAsync<bool>("({ foo }) => foo === window", new { foo }));
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept multiple nested handles</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptMultipleNestedHandles()
        {
            var foo = await Page.EvaluateHandleAsync("() => ({ x: 1, y: 'foo' })");
            var bar = await Page.EvaluateHandleAsync("() => 5");
            var baz = await Page.EvaluateHandleAsync("() => ['baz']");

            string result = await Page.EvaluateAsync<string>(
                "x => JSON.stringify(x)",
                new
                {
                    a1 = new { foo },
                    a2 = new
                    {
                        bar,
                        arr = new[] { new { baz } },
                    },
                });

            var json = JsonDocument.Parse(result).RootElement;

            Assert.Equal(1, json.GetProperty("a1").GetProperty("foo").GetProperty("x").GetInt32());
            Assert.Equal("foo", json.GetProperty("a1").GetProperty("foo").GetProperty("y").ToString());
            Assert.Equal(5, json.GetProperty("a2").GetProperty("bar").GetInt32());
            Assert.Equal("baz", json.GetProperty("a2").GetProperty("arr").EnumerateArray().First().GetProperty("baz").EnumerateArray().First().ToString());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should throw for circular objects</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForCircularObjects()
        {
            dynamic a = new ExpandoObject();
            a.a = 1;
            a.y = a;

            var exception = await Assert.ThrowsAnyAsync<JsonException>(() => Page.EvaluateAsync("x => x", a));
            Assert.Equal("Argument is a circular structure", exception.Message);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept same nested object multiple times</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptSameNestedObjectMultipleTimes()
        {
            dynamic foo = new { x = 1 };
            string result = await Page.EvaluateAsync<string>(
                "x => JSON.stringify(x)",
                new
                {
                    foo,
                    bar = new[] { foo },
                    baz = new { foo },
                });

            var json = JsonDocument.Parse(result).RootElement;

            Assert.Equal(1, json.GetProperty("foo").GetProperty("x").GetInt32());
            Assert.Equal(1, json.GetProperty("bar").EnumerateArray().First().GetProperty("x").GetInt32());
            Assert.Equal(1, json.GetProperty("baz").GetProperty("foo").GetProperty("x").GetInt32());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should accept object handle to unserializable value</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleToUnserializableValue()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => Infinity");
            Assert.True(await Page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle));
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should pass configurable args</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPassConfigurableArgs()
        {
            JsonElement result = await Page.EvaluateAsync<JsonElement>(
               @"arg =>{
                  if (arg.foo !== 42)
                    throw new Error('Not a 42');
                  arg.foo = 17;
                  if (arg.foo !== 17)
                    throw new Error('Not 17');
                  delete arg.foo;
                  if (arg.foo === 17)
                    throw new Error('Still 17');
                  return arg;
                }",
                new { foo = 42 });

            Assert.Equal("{}", result.ToString());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>Page.evaluateHandle</playwright-describe>
        ///<playwright-it>should work with primitives</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
