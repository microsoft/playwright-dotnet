using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ChromiumCSSCoverageTests : PageTestEx
    {
        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldWork()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/simple.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            StringAssert.Contains("/csscoverage/simple.html", coverage[0].Url);
            Assert.AreEqual(new[]
            {
                new CSSCoverageEntryRange
                {
                    Start = 1,
                    End = 22
                }
            }, coverage[0].Ranges);
            var range = coverage[0].Ranges[0];
            Assert.AreEqual("div { color: green; }", coverage[0].Text.Substring(range.Start, range.End - range.Start));
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report sourceURLs")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportSourceUrls()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/sourceurl.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            Assert.AreEqual("nicename.css", coverage[0].Url);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report multiple stylesheets")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportMultipleStylesheets()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/multiple.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.AreEqual(2, coverage.Length);
            var orderedList = coverage.OrderBy(c => c.Url).ToArray();
            StringAssert.Contains("/csscoverage/stylesheet1.css", orderedList[0].Url);
            StringAssert.Contains("/csscoverage/stylesheet2.css", orderedList[1].Url);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report stylesheets that have no coverage")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportStylesheetsThatHaveNoCoverage()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/unused.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            var entry = coverage[0];
            StringAssert.Contains("unused.css", entry.Url);
            Assert.IsEmpty(entry.Ranges);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work with media queries")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldWorkWithMediaQueries()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/media.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            var entry = coverage[0];
            StringAssert.Contains("/csscoverage/media.html", entry.Url);
            Assert.AreEqual(new[]
            {
                new CSSCoverageEntryRange
                {
                    Start = 17,
                    End = 38
                }
            }, coverage[0].Ranges);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work with complicated usecases")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldWorkWithComplicatedUseCases()
        {
            /*
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
                ""Text"": ""\n @charset \""utf - 8\"";\n@namespace svg url(http://www.w3.org/2000/svg);\n@font-face {\n  font-family: \""Example Font\"";\n src: url(\""./Dosis-Regular.ttf\"");\n}\n\n#fluffy {\n  border: 1px solid black;\n  z-index: 1;\n  /\n  -lol-cats: \""dogs\"" \n}\n\n@media (min-width: 1px) {\n  span {\n    -webkit-border-radius: 10px;\n    font-family: \""Example Font\"";\n    animation: 1s identifier;\n  }\n}\n""
              }
            ]";
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/involved.html");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.AreEqual(
                TestUtils.CompressText(involved),
                Regex.Replace(TestUtils.CompressText(JsonConvert.SerializeObject(coverage)), @":\d{4}\/", ":<PORT>/"));
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should ignore injected stylesheets")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldIgnoreInjectedStylesheets()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.AddStyleTagAsync(content: "body { margin: 10px;}");
            // trigger style recalc
            string margin = await Page.EvaluateAsync<string>("window.getComputedStyle(document.body).margin");
            Assert.AreEqual("10px", margin);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.IsEmpty(coverage);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should report stylesheets across navigations")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportStylesheetsAcrossNavigations()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync(false);
            await Page.GotoAsync(Server.Prefix + "/csscoverage/multiple.html");
            await Page.GotoAsync(Server.EmptyPage);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.AreEqual(2, coverage.Length);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should NOT report stylesheets across navigations")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldNotReportScriptsAcrossNavigations()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/csscoverage/multiple.html");
            await Page.GotoAsync(Server.EmptyPage);
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.IsEmpty(coverage);
            */
        }

        [PlaywrightTest("chromium-css-coverage.spec.ts", "CSS Coverage", "should work with a recently loaded stylesheet")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldWorkWithArRecentlyLoadedStylesheet()
        {
            /*
            await Page.Coverage.StartCSSCoverageAsync();
            await Page.EvaluateAsync(@"async url => {
                document.body.textContent = 'hello, world';

                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = url;
                document.head.appendChild(link);
                await new Promise(x => link.onload = x);
                await new Promise(f => requestAnimationFrame(f));
            }", Server.Prefix + "/csscoverage/stylesheet1.css");
            var coverage = await Page.Coverage.StopCSSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            */
        }
    }
}
