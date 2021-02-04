using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>user-agent sanity</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class UserAgentSanityTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public UserAgentSanityTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "user-agent sanity", "should be a sane user agent")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeASaneUserAgent()
        {
            string userAgent = await Page.EvaluateAsync<string>("() => navigator.userAgent");
            var regex = new Regex("[()]");
            string[] parts = Regex.Split(userAgent, "[()]").Select(t => t.Trim()).ToArray();

            Assert.Equal("Mozilla/5.0", parts[0]);

            if (TestConstants.IsFirefox)
            {
                string[] engineBrowser = parts[2].Split(' ');
                Assert.StartsWith("Gecko", engineBrowser[0]);
                Assert.StartsWith("Firefox", engineBrowser[1]);
            }
            else
            {
                Assert.StartsWith("AppleWebKit/", parts[2]);
                Assert.Equal("KHTML, like Gecko", parts[3]);
                string[] engineBrowser = parts[4].Split(' ');
                Assert.StartsWith("Safari/", engineBrowser[1]);

                if (TestConstants.IsChromium)
                {
                    Assert.Contains("Chrome/", engineBrowser[0]);
                }
                else
                {
                    Assert.StartsWith("Version", engineBrowser[0]);
                }
            }
        }
    }
}
