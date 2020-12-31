using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.getProperty</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandleGetPropertyTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleGetPropertyTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperty</playwright-describe>
        ///<playwright-it>should work</playwright-it>
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

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperty</playwright-describe>
        ///<playwright-it>should work with undefined, null, and empty</playwright-it>
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

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperty</playwright-describe>
        ///<playwright-it>should work with unserializable values</playwright-it>
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
