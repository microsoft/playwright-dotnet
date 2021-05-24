using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public sealed class BrowsercontextStorageStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowsercontextStorageStateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-storage-state.spec.ts", "should capture local storage")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureLocalStorage()
        {
            var page1 = await Context.NewPageAsync();
            await page1.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions { Body = "<html></html>" });
            });

            await page1.GotoAsync("https://www.example.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
            }");
            await page1.GotoAsync("https://www.domain.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name2'] = 'value2';
            }");

            string storage = await Context.StorageStateAsync();

            // TODO: think about IVT-in the StorageState and serializing
            string expected = @"{""cookies"":[],""origins"":[{""origin"":""https://www.example.com"",""localStorage"":[{""name"":""name1"",""value"":""value1""}]},{""origin"":""https://www.domain.com"",""localStorage"":[{""name"":""name2"",""value"":""value2""}]}]}";
            Assert.Equal(expected, storage);
        }

        [PlaywrightTest("browsercontext-storage-state.spec.ts", "should set local storage")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout, Skip = "Needs to be implemented.")]
        public void ShouldSetLocalStorage()
        {
        }

        [PlaywrightTest("browsercontext-storage-state.spec.ts", "should round-trip through the file")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRoundTripThroughTheFile()
        {
            var page1 = await Context.NewPageAsync();
            await page1.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions { Body = "<html></html>" });
            });

            await page1.GotoAsync("https://www.example.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
                document.cookie = 'username=John Doe';
            }");
            using var tempDir = new TempDirectory();
            string path = Path.Combine(tempDir.Path, "storage-state.json");
            string storage = await Context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path });
            Assert.Equal(storage, File.ReadAllText(path));

            await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { StorageStatePath = path });
            var page2 = await context.NewPageAsync();
            await page1.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new RouteFulfillOptions { Body = "<html></html>" });
            });

            await page1.GotoAsync("https://www.example.com");
            Assert.Equal("value1", await page1.EvaluateAsync<string>("localStorage['name1']"));
            Assert.Equal("username=John Doe", await page1.EvaluateAsync<string>("document.cookie"));
        }
    }
}
