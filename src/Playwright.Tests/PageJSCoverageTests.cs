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

namespace Microsoft.Playwright.Tests;

///<playwright-file>"chromium/js-coverage.spec.ts"</playwright-file>

public class PageJSCoverageTests : PageTestEx
{
    [PlaywrightTest]
    [Skip(SkipAttribute.Targets.Chromium)]
    public void ShouldThrowNotSupportedExceptionWhenStartingCoverageForNonChromeBrowser()
    {
        Assert.ThrowsAsync<NotSupportedException>(async () => await Page.Coverage.StartJSCoverageAsync());
    }

    [PlaywrightTest]
    [Skip(SkipAttribute.Targets.Chromium)]
    public void ShouldThrowNotSupportedExceptionWhenStoppingCoverageForNonChromeBrowser()
    {
        Assert.ThrowsAsync<NotSupportedException>(async () => await Page.Coverage.StopJSCoverageAsync());
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should work")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWork()
    {
        await Page.Coverage.StartJSCoverageAsync();
        var pageGotoOptions = new PageGotoOptions();
        pageGotoOptions.WaitUntil = WaitUntilState.Load;
        await Page.GotoAsync(Server.Prefix + "/jscoverage/simple.html", pageGotoOptions);
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        Assert.IsTrue(coverage[0].Url.Contains("/jscoverage/simple.html"));
        Assert.AreEqual(1, coverage[0].Functions.First(f => f.FunctionName == "foo").Ranges.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report sourceURLs")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportSourceURLs()
    {
        await Page.Coverage.StartJSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/jscoverage/sourceurl.html");
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        Assert.AreEqual(coverage[0].Url, "nicename.js");
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should ignore eval() scripts by default")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldIgnoreEvalScriptsByDefault()
    {
        await Page.Coverage.StartJSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/jscoverage/eval.html");
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "shouldn't ignore eval() scripts if reportAnonymousScripts is true")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotIgnoreEvalScriptsIfReportAnonymouseScriptsIsTrue()
    {
        await Page.Coverage.StartJSCoverageAsync(new PageStartJSCoverageOptions { ReportAnonymousScripts = true });
        await Page.GotoAsync(Server.Prefix + "/jscoverage/eval.html");
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual("console.log(\"foo\")", coverage.First(c => c.Url == "").Source);
        Assert.AreEqual(2, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report multiple scripts")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportMultipleScripts()
    {
        await Page.Coverage.StartJSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/jscoverage/multiple.html");
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual(2, coverage.Count);
        coverage.Sort((c1, c2) => c1.Url.CompareTo(c2.Url));
        Assert.IsTrue(coverage[0].Url.Contains("/jscoverage/script1.js"));
        Assert.IsTrue(coverage[1].Url.Contains("/jscoverage/script2.js"));
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report scripts across navigations when disabled")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportScriptsAcrossNavigationsWhenDisabled()
    {
        await Page.Coverage.StartJSCoverageAsync(new PageStartJSCoverageOptions { ResetOnNavigation = false });
        await Page.GotoAsync(Server.Prefix + "/jscoverage/multiple.html");
        await Page.GotoAsync(Server.EmptyPage);
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual(2, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should NOT report scripts across navigations when enabled")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotReportScriptsAcrossNavigationsWhenEnabled()
    {
        await Page.Coverage.StartJSCoverageAsync(); // Enabled by default
        await Page.GotoAsync(Server.Prefix + "/jscoverage/multiple.html");
        await Page.GotoAsync(Server.EmptyPage);
        var coverage = await Page.Coverage.StopJSCoverageAsync();

        Assert.AreEqual(0, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should not hang when there is a debugger statement")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldNotHangWhenThereIsADebuggerStatement()
    {
        await Page.Coverage.StartJSCoverageAsync();
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync("() => {debugger; }");
        await Page.Coverage.StopJSCoverageAsync();
    }
}
