using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;
using System.Net;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Page.authenticate</playwright-describe>
    public class PageAuthenticateTests : PlaywrightSharpPageBaseTest
    {
        internal PageAuthenticateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Page.authenticate</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
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
        [Fact]
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
        [Fact]
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
