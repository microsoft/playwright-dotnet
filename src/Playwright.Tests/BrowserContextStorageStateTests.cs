using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    public sealed class BrowsercontextStorageStateTests : PageTestEx
    {
        [PlaywrightTest("browsercontext-storage-state.spec.ts", "should capture local storage")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldCaptureLocalStorage()
        {
            var page1 = await Context.NewPageAsync();
            await page1.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new() { Body = "<html></html>" });
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
            Assert.AreEqual(expected, storage);
        }

        [PlaywrightTest("browsercontext-storage-state.spec.ts", "should set local storage")]
        [Test, Timeout(TestConstants.DefaultTestTimeout), Ignore("Needs to be implemented.")]
        public void ShouldSetLocalStorage()
        {
        }

        [PlaywrightTest("browsercontext-storage-state.spec.ts", "should round-trip through the file")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRoundTripThroughTheFile()
        {
            var page1 = await Context.NewPageAsync();
            await page1.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new() { Body = "<html></html>" });
            });

            await page1.GotoAsync("https://www.example.com");
            await page1.EvaluateAsync(@"() =>
            {
                localStorage['name1'] = 'value1';
                document.cookie = 'username=John Doe';
            }");
            using var tempDir = new TempDirectory();
            string path = Path.Combine(tempDir.Path, "storage-state.json");
            string storage = await Context.StorageStateAsync(new() { Path = path });
            Assert.AreEqual(storage, File.ReadAllText(path));

            await using var context = await Browser.NewContextAsync(new() { StorageStatePath = path });
            var page2 = await context.NewPageAsync();
            await page2.RouteAsync("**/*", (route) =>
            {
                route.FulfillAsync(new() { Body = "<html></html>" });
            });

            await page2.GotoAsync("https://www.example.com");
            Assert.AreEqual("value1", await page2.EvaluateAsync<string>("localStorage['name1']"));
            Assert.AreEqual("username=John Doe", await page2.EvaluateAsync<string>("document.cookie"));
        }
    }
}
