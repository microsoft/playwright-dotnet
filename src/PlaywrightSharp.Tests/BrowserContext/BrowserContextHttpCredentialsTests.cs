using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({httpCredentials})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextHttpCredentialsTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextHttpCredentialsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({httpCredentials})</playwright-describe>
        ///<playwright-it>sshould fail without credentials</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task ShouldFailWithoutCredentials()
        {
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({httpCredentials})</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task ShouldWork()
        {
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
            await context.SetHttpCredentials(new Credentials
            {
                Username = "user",
                Password = "pass"
            });
            response = await page.ReloadAsync();
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({httpCredentials})</playwright-describe>
        ///<playwright-it>should work with correct credentials</playwright-it>
        [Retry]
        public async Task ShouldWorkWithCorrectCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new Credentials
                {
                    Username = "user",
                    Password = "pass"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({httpCredentials})</playwright-describe>
        ///<playwright-it>should fail if wrong credentials</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task ShouldFailIfWrongCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new Credentials
                {
                    Username = "foo",
                    Password = "bar"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({httpCredentials})</playwright-describe>
        ///<playwright-it>should return resource body</playwright-it>
        [Retry]
        public async Task ShouldReturnResourceBody()
        {
            Server.SetAuth("/playground.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                HttpCredentials = new Credentials
                {
                    Username = "user",
                    Password = "pass"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
            Assert.Equal("Playground", await page.GetTitleAsync());
            Assert.Contains("Playground", await response.GetTextAsync());
        }
    }
}
