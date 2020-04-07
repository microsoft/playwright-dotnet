using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>OOPIF</playwright-describe>
    public class OopifTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public OopifTests(ITestOutputHelper output) : base(output)
        {
            DefaultOptions = TestConstants.GetDefaultBrowserOptions();
            DefaultOptions.Args = new[] { "--site-per-process" };
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>OOPIF</playwright-describe>
        ///<playwright-it>should wait for a target</playwright-it>
        [Fact(Skip = "Ignored in Playwright")]
        public async Task ShouldReportOopifFrames()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Single(Oopifs);
            Assert.Equal(2, Page.Frames.Length);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>OOPIF</playwright-describe>
        ///<playwright-it>should load oopif iframes with subresources and request interception</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldLoadOopifIframesWithSubresourcesAndRequestInterception()
        {
            await Page.SetRequestInterceptionAsync(true);
            Page.Request += (sender, e) => _ = e.Request.ContinueAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Single(Oopifs);
        }

        private IEnumerable<ITarget> Oopifs => Browser.GetTargets().Where(target => target.Type == TargetType.IFrame);
    }
}
