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
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Trait("Category", "webkit")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumSpecificPageTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ChromiumSpecificPageTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>Chromium-Specific Page Tests</playwright-describe>
        ///<playwright-it>Page.setRequestInterception should work with intervention headers</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithInterventionHeaders()
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

            await Page.SetRequestInterceptionAsync(true);
            Page.Request += async (sender, e) => await e.Request.ContinueAsync();

            await Page.GoToAsync(TestConstants.ServerUrl + "/intervention");

            Assert.Contains("feature/5718547946799104", interventionHeader);
        }
    }
}
