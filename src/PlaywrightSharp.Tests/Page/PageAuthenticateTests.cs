using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Page.authenticate</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class PageAuthenticateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAuthenticateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.authenticate</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            Server.SetAuth("/empty.html", "user", "pass");
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
            await Page.AuthenticateAsync(new Credentials
            {
                Username = "user",
                Password = "pass"
            });
            response = await Page.ReloadAsync();
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.authenticate</playwright-describe>
        ///<playwright-it>should fail if wrong credentials</playwright-it>
        [Retry]
        public async Task ShouldFailIfWrongCredentials()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user2", "pass2");
            await Page.AuthenticateAsync(new Credentials
            {
                Username = "foo",
                Password = "bar"
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.authenticate</playwright-describe>
        ///<playwright-it>should allow disable authentication</playwright-it>
        [Retry]
        public async Task ShouldAllowDisableAuthentication()
        {
            // Use unique user/password since Chromium caches credentials per origin.
            Server.SetAuth("/empty.html", "user3", "pass3");
            await Page.AuthenticateAsync(new Credentials
            {
                Username = "user3",
                Password = "pass3"
            });
            var response = await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal(HttpStatusCode.OK, response.Status);
            await Page.AuthenticateAsync(null);
            // Navigate to a different origin to bust Chromium's credential caching.
            response = await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            Assert.Equal(HttpStatusCode.Unauthorized, response.Status);
        }
    }
}
