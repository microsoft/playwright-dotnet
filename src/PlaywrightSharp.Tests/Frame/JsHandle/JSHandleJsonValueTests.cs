using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class JSHandleJsonValueTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleJsonValueTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => ({ foo: 'bar'})");
            var json = await aHandle.GetJsonValueAsync<JsonElement>();
            Assert.Equal("bar", json.GetProperty("foo").GetString());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should not work with dates</playwright-it>
        [Retry]
        public async Task ShouldNotWorkWithDates()
        {
            var dateHandle = await Page.EvaluateHandleAsync("() => new Date('2017-09-26T00:00:00.000Z')");
            object json = await dateHandle.GetJsonValueAsync<object>();
            Assert.Equal("{}", json.ToJson());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.jsonValue</playwright-describe>
        ///<playwright-it>should throw for circular objects</playwright-it>
        [Retry]
        public async Task ShouldThrowForCircularObjects()
        {
            var windowHandle = await Page.EvaluateHandleAsync("window");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => windowHandle.GetJsonValueAsync<object>());
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
        [Retry]
        public async Task ShouldWorkWithTrickyValues()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => ({ a: 1})");
            var json = await aHandle.GetJsonValueAsync<JsonElement>();
            Assert.Equal(1, json.GetProperty("a").GetInt32());
        }
    }
}
