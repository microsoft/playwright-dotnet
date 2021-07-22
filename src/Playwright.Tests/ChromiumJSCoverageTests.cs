/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ChromiumJSCoverageTests : PageTestEx
    {
        [PlaywrightTest("chromium-js-coverage.spec.ts", "JS Coverage", "should work")]
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
        [Ignore("We won't support coverage")]
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
