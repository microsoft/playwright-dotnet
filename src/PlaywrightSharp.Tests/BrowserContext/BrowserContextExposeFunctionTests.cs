using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext.exposeFunction</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextExposeFunctionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextExposeFunctionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.exposeFunction</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            var context = await Browser.NewContextAsync();
            await context.ExposeFunctionAsync("add", (int a, int b) => a + b);
            var page = await context.NewPageAsync();

            await page.ExposeFunctionAsync("mul", (int a, int b) => a * b);
            await context.ExposeFunctionAsync("sub", (int a, int b) => a - b);

            var result = await Page.EvaluateAsync<JsonElement>(@"async function() {
                return { mul: await mul(9, 4), add: await add(9, 4), sub: await sub(9, 4) };
            }");
            Assert.Equal(36, result.GetProperty("mul").GetInt32());
            Assert.Equal(13, result.GetProperty("add").GetInt32());
            Assert.Equal(5, result.GetProperty("sub").GetInt32());
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.exposeFunction</playwright-describe>
        ///<playwright-it>should throw for duplicate registrations</playwright-it>
        [Retry]
        public async Task ShouldThrowForDuplicateRegistrations()
        {
            await using var context = await Browser.NewContextAsync();
            await context.ExposeFunctionAsync("foo", () => { });
            await context.ExposeFunctionAsync("bar", () => { });

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => context.ExposeFunctionAsync("foo", () => { }));
            Assert.Equal("Function \"foo\" has been already registered", exception.Message);

            var page = await context.NewPageAsync();
            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.ExposeFunctionAsync("foo", () => { }));
            Assert.Equal("Function \"foo\" has been already registered", exception.Message);

            await context.ExposeFunctionAsync("baz", () => { });
            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => context.ExposeFunctionAsync("baz", () => { }));
            Assert.Equal("Function \"baz\" has been already registered", exception.Message);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext.exposeFunction</playwright-describe>
        ///<playwright-it>should be callable from-inside addInitScript</playwright-it>
        [Retry]
        public async Task ShouldBeCallableFromInsideAddInitScript()
        {
            var args = new List<object>();
            await using var context = await Browser.NewContextAsync();
            await context.ExposeFunctionAsync("woof", (object arg) => { args.Add(arg); });

            await context.AddInitScriptAsync("() => woof('context');");
            var page = await context.NewPageAsync();
            await page.AddInitScriptAsync("() => woof('page');");

            args.Clear();
            await page.ReloadAsync();
            Assert.Equal(new List<object> { "context", "page" }, args);
        }
    }
}
