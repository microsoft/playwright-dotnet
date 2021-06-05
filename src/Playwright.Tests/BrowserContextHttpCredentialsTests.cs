using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextCredentialsTests : BrowserTestEx
    {
        public BrowserContextCredentialsTests()
        {
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail without credentials")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithoutCredentials()
        {
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with setHTTPCredentials")]
        [Test, Ignore("This test is no longer applicable as the API no longer exists.")]
        public void ShouldWorkWithSetHTTPCredentials()
        {
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with correct credentials")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCorrectCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new()
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "user",
                    Password = "pass"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail if wrong credentials")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailIfWrongCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new()
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "foo",
                    Password = "bar"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
        }

        [PlaywrightTest("browsercontext-credentials.spec.ts", "should return resource body")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnResourceBody()
        {
            Server.SetAuth("/playground.html", "user", "pass");
            await using var context = await Browser.NewContextAsync(new()
            {
                HttpCredentials = new HttpCredentials
                {
                    Username = "user",
                    Password = "pass"
                },
            });

            var page = await context.NewPageAsync();
            var response = await page.GotoAsync(Server.Prefix + "/playground.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
            Assert.AreEqual("Playground", await page.TitleAsync());
            StringAssert.Contains("Playground", await response.TextAsync());
        }
    }
}
