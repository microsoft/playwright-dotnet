using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/oopif.spec.js</playwright-file>
    ///<playwright-describe>OOPIF</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class OopifTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public OopifTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/oopif.spec.js</playwright-file>
        ///<playwright-describe>OOPIF</playwright-describe>
        ///<playwright-it>should report oopif frames</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportOopifFrames()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = options.Args.Prepend("--site-per-process").ToArray();
            await using var browser = await BrowserType.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Equal(2, page.Frames.Length);
            Assert.Equal(TestConstants.CrossProcessHttpPrefix + "/grid.html", await page.Frames[1].EvaluateAsync<string>("() => '' + location.href"));

        }

        ///<playwright-file>chromium/oopif.spec.js</playwright-file>
        ///<playwright-describe>OOPIF</playwright-describe>
        ///<playwright-it>should load oopif iframes with subresources and request interception</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldLoadOopifIframesWithSubresourcesAndRequestInterception()
        {
            var options = TestConstants.GetDefaultBrowserOptions();
            options.Args = options.Args.Prepend("--site-per-process").ToArray();
            await using var browser = await BrowserType.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);
            page.Request += (sender, e) => _ = e.Request.ContinueAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/dynamic-oopif.html");
            Assert.Single(browser.GetTargets().Where(target => target.Type == TargetType.IFrame));
        }
    }
}
