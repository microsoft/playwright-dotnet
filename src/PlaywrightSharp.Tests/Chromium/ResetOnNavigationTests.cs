using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/chromium.spec.js</playwright-file>
    ///<playwright-describe>resetOnNavigation</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class ResetOnNavigationTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResetOnNavigationTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>resetOnNavigation</playwright-describe>
        ///<playwright-it>should report stylesheets across navigations</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportStylesheetsAcrossNavigations()
        {
            await Page.Coverage.StartCSSCoverageAsync(new CoverageStartOptions
            {
                ResetOnNavigation = false
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Equal(2, coverage.Length);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>resetOnNavigation</playwright-describe>
        ///<playwright-it>should NOT report stylesheets across navigations</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotReportScriptsAcrossNavigations()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Empty(coverage);
        }

        ///<playwright-file>chromium/chromium.spec.js</playwright-file>
        ///<playwright-describe>resetOnNavigation</playwright-describe>
        ///<playwright-it>should work with a recently loaded stylesheet</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithArRecentlyLoadedStylesheet()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.EvaluateAsync(@"async url => {
                document.body.textContent = 'hello, world';

                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = url;
                document.head.appendChild(link);
                await new Promise(x => link.onload = x);
            }", TestConstants.ServerUrl + "/csscoverage/stylesheet1.css");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Single(coverage);
        }
    }
}
