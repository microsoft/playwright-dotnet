using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandlePropertiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandlePropertiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "getProperties should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task GetPropertiesShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                foo: 'bar'
            })");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.True(properties.TryGetValue("foo", out var foo));
            Assert.Equal("bar", await foo.GetJsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should return empty map for non-objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyMapForNonObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 123");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.Empty(properties);
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should return even non-own properties")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEvenNonOwnProperties()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => {
                class A
                {
                    constructor()
                    {
                        this.a = '1';
                    }
                }
                class B extends A
                {
                    constructor() {
                        super();
                        this.b = '2';
                    }
                }
                return new B();
            }");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.Equal("1", await properties["a"].GetJsonValueAsync<string>());
            Assert.Equal("2", await properties["b"].GetJsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                one: 1,
                two: 2,
                three: 3
            })");
            var twoHandle = await aHandle.GetPropertyAsync("two");
            Assert.Equal(2, await twoHandle.GetJsonValueAsync<int>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should work with undefined, null, and empty")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUndefinedNullAndEmpty()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                undefined: undefined,
                null: null,
            })");
            var undefinedHandle = await aHandle.GetPropertyAsync("undefined");
            Assert.Null(await undefinedHandle.GetJsonValueAsync<string>());
            var nullHandle = await aHandle.GetPropertyAsync("null");
            Assert.Null(await nullHandle.GetJsonValueAsync<string>());
            var emptyHandle = await aHandle.GetPropertyAsync("emptyHandle");
            Assert.Null(await emptyHandle.GetJsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle-properties.spec.ts", "should work with unserializable values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUnserializableValues()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                infinity: Infinity,
                nInfinity: -Infinity,
                nan: NaN,
                nzero: -0
            })");

            var infinityHandle = await aHandle.GetPropertyAsync("infinity");
            Assert.Equal(double.PositiveInfinity, await infinityHandle.GetJsonValueAsync<double>());
            var ninfinityHandle = await aHandle.GetPropertyAsync("nInfinity");
            Assert.Equal(double.NegativeInfinity, await ninfinityHandle.GetJsonValueAsync<double>());
            var nanHandle = await aHandle.GetPropertyAsync("nan");
            Assert.Equal(double.NaN, await nanHandle.GetJsonValueAsync<double>());
            var nzeroHandle = await aHandle.GetPropertyAsync("nzero");
            Assert.True((await nzeroHandle.GetJsonValueAsync<double>()).IsNegativeZero());
        }
    }
}
