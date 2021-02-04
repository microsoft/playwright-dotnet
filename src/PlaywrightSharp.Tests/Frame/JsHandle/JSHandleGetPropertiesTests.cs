using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.getProperties</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandleGetPropertiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleGetPropertiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.getProperties", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                foo: 'bar'
            })");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.True(properties.TryGetValue("foo", out var foo));
            Assert.Equal("bar", await foo.GetJsonValueAsync<string>());
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.getProperties", "should return empty map for non-objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyMapForNonObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 123");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.Empty(properties);
        }

        [PlaywrightTest("jshandle.spec.js", "JSHandle.getProperties", "should return even non-own properties")]
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
    }
}
