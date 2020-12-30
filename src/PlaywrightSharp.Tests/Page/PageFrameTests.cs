using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.frame</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageFrameTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageFrameTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.frame</playwright-describe>
        ///<playwright-it>should respect name</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheCorrectBrowserInstance()
        {
            await Page.SetContentAsync("<iframe name=target></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Name == "target");
            Assert.Same(Page.MainFrame.ChildFrames[0], frame);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.frame</playwright-describe>
        ///<playwright-it>should respect url</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectUrl()
        {
            await Page.SetContentAsync($"<iframe src=\"{TestConstants.EmptyPage}\"></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Url.Contains("empty"));
            Assert.Same(Page.MainFrame.ChildFrames[0], frame);
        }
    }
}
