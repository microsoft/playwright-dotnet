using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextCredentialsTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextCredentialsTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail without credentials")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithoutCredentials()
        {
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with setHTTPCredentials")]
        [Fact(Skip = "This test is no longer applicable as the API no longer exists.")]
        public void ShouldWorkWithSetHTTPCredentials()
        {
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with correct credentials")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCorrectCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "user",
                    Password = "pass"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail if wrong credentials")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailIfWrongCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "foo",
                    Password = "bar"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should return resource body")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnResourceBody()
        {
            Server.SetAuth("/playground.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "user",
                    Password = "pass"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Playground", await page.TitleAsync());
            Assert.Contains("Playground", await response.GetTextAsync());
        }
    }
}
