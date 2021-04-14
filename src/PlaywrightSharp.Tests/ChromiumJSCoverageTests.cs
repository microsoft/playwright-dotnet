using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ChromiumJSCoverageTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ChromiumJSCoverageTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should work")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldWork()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/simple.html", LoadState.NetworkIdle);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.Equal(1, coverage[0].Functions.Single(f => f.FunctionName == "foo").Ranges[0].Count);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report sourceURLs")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldReportSourceUrls()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/sourceurl.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Equal("nicename.js", coverage[0].Url);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should ignore eval() scripts by default")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldIgnoreEvalScriptsByDefault()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/eval.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "shouldn't ignore eval() scripts if reportAnonymousScripts is true")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldNotIgnoreEvalScriptsIfReportAnonymousScriptsIsTrue()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync(reportAnonymousScripts: true);
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/eval.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Equal("console.log(\"foo\")", coverage.Single(entry => entry.Url == string.Empty).Source);
            Assert.Equal(2, coverage.Length);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report multiple scripts")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldReportMultipleScripts()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/multiple.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Equal(2, coverage.Length);
            var orderedList = coverage.OrderBy(c => c.Url).ToArray();
            Assert.Contains("/jscoverage/script1.js", orderedList[0].Url);
            Assert.Contains("/jscoverage/script2.js", orderedList[1].Url);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report scripts across navigations when disabled")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldReportScriptsAcrossNavigationsWhenDisabled()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync(false);
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Equal(2, coverage.Length);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should NOT report scripts across navigations when enabled")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldNotReportScriptsAcrossNavigationsWhenEnabled()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Empty(coverage);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should not hang when there is a debugger statement")]
        [Fact(Skip = "We won't support coverage")]
        public void ShouldNotHangWhenThereIsADebuggerStatement()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                debugger; // eslint-disable-line no-debugger
            }");
            await Page.Coverage.StopJSCoverageAsync();
            */
        }
    }
}
