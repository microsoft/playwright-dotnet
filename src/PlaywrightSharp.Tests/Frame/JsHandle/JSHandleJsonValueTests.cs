using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
    public class JSHandleJsonValueTests : PlaywrightSharpPageBaseTest
    {
        internal JSHandleJsonValueTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => ({ foo: 'bar'})");
            var json = await aHandle.GetJsonValueAsync<object>();
            Assert.Equal(new { foo = "bar" }, json);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should not work with dates</playwright-it>
        [Fact]
        public async Task ShouldNotWorkWithDates()
        {
            var dateHandle = await Page.EvaluateHandleAsync("() => new Date('2017-09-26T00:00:00.000Z')");
            var json = await dateHandle.GetJsonValueAsync<object>();
            Assert.Equal(new { }, json);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should throw for circular objects</playwright-it>
        [Fact]
        public async Task ShouldThrowForCircularObjects()
        {
            var windowHandle = await Page.EvaluateHandleAsync("window");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => windowHandle.GetJsonValueAsync<object>());
            if (TestConstants.IsWebKit)
            {
                Assert.Contains("Object has too long reference chain", exception.Message);
            }
            else if (TestConstants.IsChromium)
            {
                Assert.Contains("Object reference chain is too long", exception.Message);
            }
            else if (TestConstants.IsFirefox)
            {
                Assert.Contains("Object is not serializable", exception.Message);
            }
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should work with tricky values</playwright-it>
        [Fact]
        public async Task ShouldWorkWithTrickyValues()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => ({ a: 1})");
            var json = await aHandle.GetJsonValueAsync<object>();
            Assert.Equal(new { a = 1 }, json);
        }
    }
}
