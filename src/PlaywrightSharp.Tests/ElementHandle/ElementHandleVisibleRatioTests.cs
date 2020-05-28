using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.visibleRatio</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleVisibleRatioTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleVisibleRatioTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.visibleRatio</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            for (int i = 0; i < 11; ++i)
            {
                var button = await Page.QuerySelectorAsync("#btn" + i);
                double ratio = await button.GetVisibleRatioAsync();
                Assert.Equal(10 - i, Math.Round(ratio * 10));
            }
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.visibleRatio</playwright-describe>
        ///<playwright-it>should work when Node is removed</playwright-it>
        [Retry]
        public async Task ShouldWorkWhenNodeIsRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            for (int i = 0; i < 11; ++i)
            {
                var button = await Page.QuerySelectorAsync("#btn" + i);
                double ratio = await button.GetVisibleRatioAsync();
                Assert.Equal(10 - i, Math.Round(ratio * 10));
            }
        }
    }
}
