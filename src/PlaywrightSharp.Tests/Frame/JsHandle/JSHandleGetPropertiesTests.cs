using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame.JsHandle
{
    ///<playwright-file>jshandle.spec.js</playwright-file>
    ///<playwright-describe>JSHandle.getProperties</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class JSHandleGetPropertiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public JSHandleGetPropertiesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperties</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var aHandle = await Page.EvaluateHandleAsync(@"() => ({
                foo: 'bar'
            })");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.True(properties.TryGetValue("foo", out var foo));
            Assert.Equal("bar", await foo.GetJsonValueAsync<string>());
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperties</playwright-describe>
        ///<playwright-it>should return empty map for non-objects</playwright-it>
        [Retry]
        public async Task ShouldReturnEmptyMapForNonObjects()
        {
            var aHandle = await Page.EvaluateHandleAsync("() => 123");
            var properties = await aHandle.GetPropertiesAsync();
            Assert.Empty(properties);
        }

        ///<playwright-file>jshandle.spec.js</playwright-file>
        ///<playwright-describe>JSHandle.getProperties</playwright-describe>
        ///<playwright-it>should return even non-own properties</playwright-it>
        [Retry]
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
