using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>frame.spec.js</playwright-file>
    ///<playwright-describe>Frame.evaluateHandle</playwright-describe>
    public class FrameEvaluateHandleTests : PlaywrightSharpPageBaseTest
    {
        internal FrameEvaluateHandleTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>frame.spec.js</playwright-file>
        ///<playwright-describe>Frame.evaluateHandle</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var windowHandle = await Page.MainFrame.EvaluateHandleAsync("() => window");
            Assert.NotNull(windowHandle);
        }
    }
}
