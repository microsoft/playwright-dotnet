using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page api coverage</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageApiCoverageTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageApiCoverageTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page api coverage</playwright-describe>
        ///<playwright-it>Page.press should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PagePressShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.PressAsync("textarea", "a");
            Assert.Equal("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page api coverage</playwright-describe>
        ///<playwright-it>Frame.press should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task FramePressShouldWork()
        {
            await Page.SetContentAsync($"<iframe name =inner src=\"{TestConstants.ServerUrl}/input/textarea.html\"></iframe>");
            var frame = Page.Frames.FirstOrDefault(f => f.Name == "inner");
            await frame.PressAsync("textarea", "a");
            Assert.Equal("a", await frame.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }
    }
}
