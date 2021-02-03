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
    ///<playwright-describe>Page api coverage</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageApiCoverageTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageApiCoverageTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page api coverage", "Page.press should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PagePressShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            await Page.PressAsync("textarea", "a");
            Assert.Equal("a", await Page.EvaluateAsync<string>("() => document.querySelector('textarea').value"));
        }

        [PlaywrightTest("page.spec.js", "Page api coverage", "Frame.press should work")]
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
