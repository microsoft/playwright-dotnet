using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]class TimezoneTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public TimezoneTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWork()
        {
            const string func = "() => new Date(1479579154987).toString()";
            var page = await NewPageAsync(new BrowserContextOptions { TimezoneId = "America/Jamaica" });
            Assert.Equal(
                "Sat Nov 19 2016 13:12:34 GMT-0500 (Eastern Standard Time)",
                await page.EvaluateAsync<string>(func));

            page = await NewPageAsync(new BrowserContextOptions { TimezoneId = "Pacific/Honolulu" });
            Assert.Equal(
                "Sat Nov 19 2016 08:12:34 GMT-1000 (Hawaii-Aleutian Standard Time)",
                await page.EvaluateAsync<string>(func));

            page = await NewPageAsync(new BrowserContextOptions { TimezoneId = "America/Buenos_Aires" });
            Assert.Equal(
                "Sat Nov 19 2016 15:12:34 GMT-0300 (Argentina Standard Time)",
                await page.EvaluateAsync<string>(func));

            page = await NewPageAsync(new BrowserContextOptions { TimezoneId = "Europe/Berlin" });
            Assert.Equal(
                "Sat Nov 19 2016 19:12:34 GMT+0100 (Central European Standard Time)",
                await page.EvaluateAsync<string>(func));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>BrowserContext({timezoneId})</playwright-describe>
        ///<playwright-it>should throw for invalid timezone IDs</playwright-it>
        [Retry]
        public async Task ShouldThrowForInvalidTimezoneId()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(
                () => NewPageAsync(new BrowserContextOptions { TimezoneId = "Foo/Bar" }));
            Assert.Contains("Invalid timezone ID: Foo/Bar", exception.Message);

            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(
                () => NewPageAsync(new BrowserContextOptions { TimezoneId = "Baz/Qux" }));
            Assert.Contains("Invalid timezone ID: Baz/Qux", exception.Message);
        }
    }
}
