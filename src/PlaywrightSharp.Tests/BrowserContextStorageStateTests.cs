using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{

    /// <playwright-file>browsercontext-storage-state.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public sealed class BrowsercontextStorageStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowsercontextStorageStateTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>browsercontext-storage-state.spec.ts</playwright-file>
        /// <playwright-it>should capture local storage</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldCaptureLocalStorage()
        {
            var page1 = await Context.NewPageAsync();
            await page1.RouteAsync("**/*", (route, _) =>
            {
                route.FulfillAsync(body: "<html></html>");
            });

            await page1.GoToAsync("https://www.example.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
            }");
            await page1.GoToAsync("https://www.domain.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name2'] = 'value2';
            }");
            var storage = await Context.GetStorageStateAsync();
            Assert.Equal(
                new List<StorageStateOrigin>()
                {
                    new StorageStateOrigin
                    {
                        Origin= "https://www.example.com",
                        LocalStorage = new List<NameValueEntry>
                        {
                            new NameValueEntry("name1", "value1")
                        },
                    },
                    new StorageStateOrigin
                    {
                        Origin= "https://www.domain.com",
                        LocalStorage = new List<NameValueEntry>
                        {
                            new NameValueEntry("name2", "value2")
                        },
                    }
                  },
                storage.Origins);
        }

        /// <playwright-file>browsercontext-storage-state.spec.ts</playwright-file>
        /// <playwright-it>should set local storage</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSetLocalStorage()
        {
            await using var context = await Browser.NewContextAsync(storageState: new StorageState
            {
                Origins = new List<StorageStateOrigin>
                {
                    new StorageStateOrigin
                    {
                        Origin= "https://www.example.com",
                        LocalStorage = new List<NameValueEntry>
                        {
                            new NameValueEntry("name1", "value1")
                        },
                    }
                }
            });
            var page1 = await context.NewPageAsync();
            await page1.RouteAsync("**/*", (route, _) =>
            {
                route.FulfillAsync(body: "<html></html>");
            });

            await page1.GoToAsync("https://www.example.com");
            Assert.Equal("value1", await page1.EvaluateAsync<string>(@"localStorage['name1']"));
        }

        /// <playwright-file>browsercontext-storage-state.spec.ts</playwright-file>
        /// <playwright-it>should round-trip through the file</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRoundTripThroughTheFile()
        {
            var page1 = await Context.NewPageAsync();
            await page1.RouteAsync("**/*", (route, _) =>
            {
                route.FulfillAsync(body: "<html></html>");
            });

            await page1.GoToAsync("https://www.example.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
                document.cookie = 'username=John Doe';
            }");
            using var tempDir = new TempDirectory();
            string path = Path.Combine(tempDir.Path, "storage-state.json");
            var storage = await Context.GetStorageStateAsync(path);
            Assert.Equal(storage, JsonSerializer.Deserialize<StorageState>(File.ReadAllText(path), JsonExtensions.DefaultJsonSerializerOptions));

            await using var context = await Browser.NewContextAsync(storageStatePath: path);
            var page2 = await context.NewPageAsync();
            await page1.RouteAsync("**/*", (route, _) =>
            {
                route.FulfillAsync(body: "<html></html>");
            });

            await page1.GoToAsync("https://www.example.com");
            Assert.Equal("value1", await page1.EvaluateAsync<string>("localStorage['name1']"));
            Assert.Equal("username=John Doe", await page1.EvaluateAsync<string>("document.cookie"));
        }
    }
}
