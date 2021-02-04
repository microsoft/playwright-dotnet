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
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWork()
        {
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/simple.html", LifecycleEvent.Networkidle);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.Equal(1, coverage[0].Functions.FirstOrDefault(f => f.FunctionName == "foo").Ranges[0].Count);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report sourceURLs")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportSourceUrls()
        {
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/sourceurl.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
            Assert.Equal("nicename.js", coverage[0].Url);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should ignore eval() scripts by default")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldIgnoreEvalScriptsByDefault()
        {
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/eval.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Single(coverage);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "shouldn't ignore eval() scripts if reportAnonymousScripts is true")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotIgnoreEvalScriptsIfReportAnonymousScriptsIsTrue()
        {
            await Page.Coverage.StartJSCoverageAsync(reportAnonymousScripts: true);
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/eval.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Equal("console.log(\"foo\")", coverage.FirstOrDefault(entry => entry.Url == string.Empty).Source);
            Assert.Equal(2, coverage.Length);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report multiple scripts")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportMultipleScripts()
        {
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/multiple.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Equal(2, coverage.Length);
            var orderedList = coverage.OrderBy(c => c.Url).ToArray();
            Assert.Contains("/jscoverage/script1.js", orderedList[0].Url);
            Assert.Contains("/jscoverage/script2.js", orderedList[1].Url);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report scripts across navigations when disabled")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReportScriptsAcrossNavigationsWhenDisabled()
        {
            await Page.Coverage.StartJSCoverageAsync(false);
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Equal(2, coverage.Length);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should NOT report scripts across navigations when enabled")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotReportScriptsAcrossNavigationsWhenEnabled()
        {
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/jscoverage/multiple.html");
            await Page.GoToAsync(TestConstants.EmptyPage);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.Empty(coverage);
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should not hang when there is a debugger statement")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldNotHangWhenThereIsADebuggerStatement()
        {
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                debugger; // eslint-disable-line no-debugger
            }");
            await Page.Coverage.StopJSCoverageAsync();
        }
    }
}
