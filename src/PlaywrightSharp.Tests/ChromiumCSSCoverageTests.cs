using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumCSSCoverageTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ChromiumCSSCoverageTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWork()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/simple.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Single(coverage);
            Assert.Contains("/csscoverage/simple.html", coverage[0].Url);
            Assert.Equal(new[]
            {
                new CSSCoverageEntryRange
                {
                    Start = 1,
                    End = 22
                }
            }, coverage[0].Ranges);
            var range = coverage[0].Ranges[0];
            Assert.Equal("div { color: green; }", coverage[0].Text.Substring(range.Start, range.End - range.Start));
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report sourceURLs")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportSourceUrls()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/sourceurl.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Single(coverage);
            Assert.Equal("nicename.css", coverage[0].Url);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report multiple stylesheets")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportMultipleStylesheets()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/multiple.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Equal(2, coverage.Length);
            var orderedList = coverage.OrderBy(c => c.Url).ToArray();
            Assert.Contains("/csscoverage/stylesheet1.css", orderedList[0].Url);
            Assert.Contains("/csscoverage/stylesheet2.css", orderedList[1].Url);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report stylesheets that have no coverage")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportStylesheetsThatHaveNoCoverage()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/unused.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Single(coverage);
            var entry = coverage[0];
            Assert.Contains("unused.css", entry.Url);
            Assert.Empty(entry.Ranges);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work with media queries")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithMediaQueries()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/media.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Single(coverage);
            var entry = coverage[0];
            Assert.Contains("/csscoverage/media.html", entry.Url);
            Assert.Equal(new[]
            {
                new CSSCoverageEntryRange
                {
                    Start = 17,
                    End = 38
                }
            }, coverage[0].Ranges);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work with complicated usecases")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithComplicatedUseCases()
        {
            const string involved = @"[
              {
                ""Url"": ""http://localhost:<PORT>/csscoverage/involved.html"",
                ""Ranges"": [
                  {
                    ""Start"": 149,
                    ""End"": 297
                  },
                  {
                    ""Start"": 327,
                    ""End"": 433
                  }
                ],
                ""Text"": ""\n @charset \""utf - 8\"";\n@namespace svg url(http://www.w3.org/2000/svg);\n@font-face {\n  font-family: \""Example Font\"";\n src: url(\""./Dosis-Regular.ttf\"");\n}\n\n#fluffy {\n  border: 1px solid black;\n  z-index: 1;\n  /* -webkit-disabled-property: rgb(1, 2, 3) */\n  -lol-cats: \""dogs\"" /* non-existing property */\n}\n\n@media (min-width: 1px) {\n  span {\n    -webkit-border-radius: 10px;\n    font-family: \""Example Font\"";\n    animation: 1s identifier;\n  }\n}\n""
              }
            ]";
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/involved.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Equal(
                TestUtils.CompressText(involved),
                Regex.Replace(TestUtils.CompressText(JsonConvert.SerializeObject(coverage)), @":\d{4}\/", ":<PORT>/"));
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should ignore injected stylesheets")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldIgnoreInjectedStylesheets()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.AddStyleTagAsync(content: "body { margin: 10px;}");
            // trigger style recalc
            string margin = await Page.EvaluateAsync<string>("window.getComputedStyle(document.body).margin");
            Assert.Equal("10px", margin);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Empty(coverage);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report stylesheets across navigations")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportStylesheetsAcrossNavigations()
        {
            await Page.Coverage.StartCSSCoverageAsync(false);
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Equal(2, coverage.Length);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should NOT report stylesheets across navigations")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotReportScriptsAcrossNavigations()
        {
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/csscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Empty(coverage);
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work with a recently loaded stylesheet")]
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
                await new Promise(f => requestAnimationFrame(f));
            }", TestConstants.ServerUrl + "/csscoverage/stylesheet1.css");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.Single(coverage);
        }
    }
}
