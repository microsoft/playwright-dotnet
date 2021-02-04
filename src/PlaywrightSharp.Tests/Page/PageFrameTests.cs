using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("page.spec.js", "Page.frame", "should respect name")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnTheCorrectBrowserInstance()
        {
            await Page.SetContentAsync("<iframe name=target></iframe>");
            Assert.Null(Page.Frames.FirstOrDefault(f => f.Name == "bogus"));
            var frame = Page.Frames.FirstOrDefault(f => f.Name == "target");
            Assert.Same(Page.MainFrame.ChildFrames[0], frame);
        }

        [PlaywrightTest("page.spec.js", "Page.frame", "should respect url")]
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
