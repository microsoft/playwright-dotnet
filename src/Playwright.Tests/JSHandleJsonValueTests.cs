using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class JSHandleJsonValueTests : PageTestEx
    {
        [PlaywrightTest("jshandle-json-value.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => ({ foo: 'bar'})");
            var json = await aHandle.JsonValueAsync<JsonElement>();
            Assert.AreEqual("bar", json.GetProperty("foo").GetString());
        }

        [PlaywrightTest("jshandle-json-value.spec.ts", "should work with dates")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDates()
        {
            var dateHandle = await Page.EvaluateHandleAsync("() => new Date('2017-09-26T00:00:00.000Z')");
            var json = await dateHandle.JsonValueAsync<DateTime>();
            Assert.AreEqual(2017, json.Year);
        }

        [PlaywrightTest("jshandle-json-value.spec.ts", "should throw for circular objects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForCircularObjects()
        {
            var windowHandle = await Page.EvaluateHandleAsync("window");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => windowHandle.JsonValueAsync<object>());
            StringAssert.Contains("Argument is a circular structure", exception.Message);
        }
    }
}
