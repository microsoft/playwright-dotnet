using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEvaluateHandleTests : PageTestEx
    {
        [PlaywrightTest("page-evaluate-handle.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var windowHandle = await Page.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle as an argument")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleAsAnArgument()
        {
            var navigatorHandle = await Page.EvaluateHandleAsync("() => navigator");
            string text = await Page.EvaluateAsync<string>("e => e.userAgent", navigatorHandle);
            StringAssert.Contains("Mozilla", text);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle to primitive types")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleToPrimitiveTypes()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 5");
            bool isFive = await Page.EvaluateAsync<bool>("e => Object.is (e, 5)", aHandle);
            Assert.True(isFive);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept nested handle")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptNestedHandle()
        {
            var foo = await Page.EvaluateHandleAsync("() => ({x: 1, y: 'foo'})");
            dynamic result = await Page.EvaluateAsync<ExpandoObject>("({ foo }) => foo", new { foo });

            Assert.AreEqual(1, result.x);
            Assert.AreEqual("foo", result.y);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept nested window handle")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptNestedWindowHandle()
        {
            var foo = await Page.EvaluateHandleAsync("() => window");
            Assert.True(await Page.EvaluateAsync<bool>("({ foo }) => foo === window", new { foo }));
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept multiple nested handles")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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

            Assert.AreEqual(1, json.GetProperty("a1").GetProperty("foo").GetProperty("x").GetInt32());
            Assert.AreEqual("foo", json.GetProperty("a1").GetProperty("foo").GetProperty("y").ToString());
            Assert.AreEqual(5, json.GetProperty("a2").GetProperty("bar").GetInt32());
            Assert.AreEqual("baz", json.GetProperty("a2").GetProperty("arr").EnumerateArray().First().GetProperty("baz").EnumerateArray().First().ToString());
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should throw for circular objects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void ShouldThrowForCircularObjects()
        {
            dynamic a = new ExpandoObject();
            a.a = 1;
            a.y = a;

            var exception = Assert.ThrowsAsync<JsonException>(async () => await Page.EvaluateAsync("x => x", a));
            Assert.AreEqual("Argument is a circular structure", exception.Message);
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept same nested object multiple times")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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

            Assert.AreEqual(1, json.GetProperty("foo").GetProperty("x").GetInt32());
            Assert.AreEqual(1, json.GetProperty("bar").EnumerateArray().First().GetProperty("x").GetInt32());
            Assert.AreEqual(1, json.GetProperty("baz").GetProperty("foo").GetProperty("x").GetInt32());
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should accept object handle to unserializable value")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptObjectHandleToUnserializableValue()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => Infinity");
            Assert.True(await Page.EvaluateAsync<bool>("e => Object.is(e, Infinity)", aHandle));
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should pass configurable args")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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

            Assert.AreEqual("{}", result.ToString());
        }

        [PlaywrightTest("page-evaluate-handle.spec.ts", "should work with primitives")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPrimitives()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => {
                window.FOO = 123;
                return window;
            }");
            Assert.AreEqual(123, await Page.EvaluateAsync<int>("e => e.FOO", aHandle));
        }
    }
}
