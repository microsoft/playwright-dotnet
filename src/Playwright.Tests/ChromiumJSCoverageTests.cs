using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ChromiumJSCoverageTests : PageTestEx
    {
        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should work")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldWork()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/jscoverage/simple.html", LoadState.NetworkIdle);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            StringAssert.Contains("/jscoverage/simple.html", coverage[0].Url);
            Assert.AreEqual(1, coverage[0].Functions.Single(f => f.FunctionName == "foo").Ranges[0].Count);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report sourceURLs")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportSourceUrls()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/jscoverage/sourceurl.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            Assert.AreEqual("nicename.js", coverage[0].Url);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should ignore eval() scripts by default")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldIgnoreEvalScriptsByDefault()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/jscoverage/eval.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.That(coverage, Has.Count.EqualTo(1));
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "shouldn't ignore eval() scripts if reportAnonymousScripts is true")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldNotIgnoreEvalScriptsIfReportAnonymousScriptsIsTrue()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync(reportAnonymousScripts: true);
            await Page.GotoAsync(Server.Prefix + "/jscoverage/eval.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.AreEqual("console.log(\"foo\")", coverage.Single(entry => entry.Url == string.Empty).Source);
            Assert.AreEqual(2, coverage.Length);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report multiple scripts")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportMultipleScripts()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/jscoverage/multiple.html");
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.AreEqual(2, coverage.Length);
            var orderedList = coverage.OrderBy(c => c.Url).ToArray();
            StringAssert.Contains("/jscoverage/script1.js", orderedList[0].Url);
            StringAssert.Contains("/jscoverage/script2.js", orderedList[1].Url);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should report scripts across navigations when disabled")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldReportScriptsAcrossNavigationsWhenDisabled()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync(false);
            await Page.GotoAsync(Server.Prefix + "/jscoverage/multiple.html");
            await Page.GotoAsync(Server.EmptyPage);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.AreEqual(2, coverage.Length);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should NOT report scripts across navigations when enabled")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldNotReportScriptsAcrossNavigationsWhenEnabled()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GotoAsync(Server.Prefix + "/jscoverage/multiple.html");
            await Page.GotoAsync(Server.EmptyPage);
            var coverage = await Page.Coverage.StopJSCoverageAsync();
            Assert.IsEmpty(coverage);
            */
        }

        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should not hang when there is a debugger statement")]
        [Test, Ignore("We won't support coverage")]
        public void ShouldNotHangWhenThereIsADebuggerStatement()
        {
            /*
            await Page.Coverage.StartJSCoverageAsync();
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync(@"() => {
                debugger; // eslint-disable-line no-debugger
            }");
            await Page.Coverage.StopJSCoverageAsync();
            */
        }
    }
}
