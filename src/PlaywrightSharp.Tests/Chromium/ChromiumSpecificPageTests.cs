using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>Chromium-Specific Page Tests</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumSpecificPageTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ChromiumSpecificPageTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("chromium/chromium.spec.js", "Chromium-Specific Page Tests", "Page.route should work with intervention headers")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task PageRouteShouldWorkWithInterventionHeaders()
        {
            Server.SetRoute("/intervention", context => context.Response.WriteAsync($@"
              <script>
                document.write('<script src=""{TestConstants.CrossProcessHttpPrefix}/intervention.js"">' + '</scr' + 'ipt>');
              </script>
            "));
            Server.SetRedirect("/intervention.js", "/redirect.js");

            string interventionHeader = null;
            Server.SetRoute("/redirect.js", context =>
            {
                interventionHeader = context.Request.Headers["intervention"];
                return context.Response.WriteAsync("console.log(1);");
            });

            await Page.RouteAsync("*", (route, _) => route.ContinueAsync());

            await Page.GoToAsync(TestConstants.ServerUrl + "/intervention");

            Assert.Contains("feature/5718547946799104", interventionHeader);
        }
    }
}
