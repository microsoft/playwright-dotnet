using System;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEvaluateHandleTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEvaluateHandleTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle as an argument")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleAsAnArgument()
        {
            var navigatorHandle = await Page.EvaluateHandleAsync("() => navigator");
            string text = await Page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
            Assert.Contains("Mozilla", text);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle to primitive types")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleToPrimitiveTypes()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 5");
            bool isFive = await Page.EvaluateAsync<bool>("e => Object.is (e, 5)", aHandle);
            Assert.True(isFive);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept nested handle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptNestedHandle()
        {
            var foo = await Page.EvaluateHandleAsync("() => ({x: 1, y: 'foo'})");
            dynamic result = await Page.EvaluateAsync<ExpandoObject>("({ foo }) => foo", new { foo });

            Assert.Equal(1, result.x);
            Assert.Equal("foo", result.y);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept nested window handle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptNestedWindowHandle()
        {
            var foo = await Page.EvaluateHandleAsync("() => window");
            Assert.True(await Page.EvaluateAsync<bool>("({ foo }) => foo === window", new { foo }));
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept multiple nested handles")]
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

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should throw for circular objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForCircularObjects()
        {
            dynamic a = new ExpandoObject();
            a.a = 1;
            a.y = a;

            var exception = await Assert.ThrowsAnyAsync<JsonException>(() => Page.EvaluateAsync("x => x", a));
            Assert.Equal("Argument is a circular structure", exception.Message);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept same nested object multiple times")]
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

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle to unserializable value")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleToUnserializableValue()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => Infinity");
            Assert.True(await Page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle));
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should pass configurable args")]
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

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should work with primitives")]
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
