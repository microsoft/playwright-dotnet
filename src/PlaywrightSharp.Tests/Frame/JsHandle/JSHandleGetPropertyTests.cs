using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.getProperty</playwright-describe>
    public class JSHandleGetPropertyTests : PlaywrightSharpPageBaseTest
    {
        internal JSHandleGetPropertyTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperty</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
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
    }
}
