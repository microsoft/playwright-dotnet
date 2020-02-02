using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;
using System;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.visibleRatio</playwright-describe>
    public class ElementHandleVisibleRatioTests : PlaywrightSharpPageBaseTest
    {
        internal ElementHandleVisibleRatioTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.visibleRatio</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            for (var i = 0; i < 11; ++i)
            {
                var button = await Page.QuerySelectorAsync("#btn" + i);
                var ratio = await button.VisibleRatioAsync();
                Assert.Equal(10 - i, Math.Round(ratio * 10));
            }
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.visibleRatio</playwright-describe>
        ///<playwright-it>should work when Node is removed</playwright-it>
        [Fact]
        public async Task ShouldWorkWhenNodeIsRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            for (var i = 0; i < 11; ++i)
            {
                var button = await Page.QuerySelectorAsync("#btn" + i);
                var ratio = await button.VisibleRatioAsync();
                Assert.Equal(10 - i, Math.Round(ratio * 10));
            }
        }
    }
}
