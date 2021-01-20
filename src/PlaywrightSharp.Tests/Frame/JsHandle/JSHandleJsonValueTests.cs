using System;
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
    public class JSHandleJsonValueTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleJsonValueTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.jsonValue", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => ({ foo: 'bar'})");
            var json = await aHandle.GetJsonValueAsync<JsonElement>();
            Assert.Equal("bar", json.GetProperty("foo").GetString());
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.jsonValue", "should work with dates")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDates()
        {
            var dateHandle = await Page.EvaluateHandleAsync("() => new Date('2017-09-26T00:00:00.000Z')");
            var json = await dateHandle.GetJsonValueAsync<DateTime>();
            Assert.Equal(2017, json.Year);
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.jsonValue", "should throw for circular objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForCircularObjects()
        {
            var windowHandle = await Page.EvaluateHandleAsync("window");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => windowHandle.GetJsonValueAsync<object>());
            Assert.Contains("Argument is a circular structure", exception.Message);
        }
    }
}
