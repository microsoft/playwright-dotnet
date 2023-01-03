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

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static NUnit.Framework.Constraints.Tolerance;

namespace Microsoft.Playwright.Tests;

///<playwright-file>"chromium/js-coverage.spec.ts"</playwright-file>

public class PageCSSCoverageTests : PageTestEx
{
    [PlaywrightTest]
    [Skip(SkipAttribute.Targets.Chromium)]
    public void ShouldThrowNotSupportedExceptionWhenStartingCoverageForNonChromeBrowser()
    {
        Assert.ThrowsAsync<NotSupportedException>(async () => await Page.Coverage.StartCSSCoverageAsync());
    }

    [PlaywrightTest]
    [Skip(SkipAttribute.Targets.Chromium)]
    public void ShouldThrowNotSupportedExceptionWhenStoppingCoverageForNonChromeBrowser()
    {
        Assert.ThrowsAsync<NotSupportedException>(async () => await Page.Coverage.StopCSSCoverageAsync());
    }

    [PlaywrightTest("chromium/css-coverage.spec.ts", "should work")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWork()
    {
        await Page.Coverage.StartCSSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/csscoverage/simple.html");
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        Assert.IsTrue(coverage[0].Url.Contains("/csscoverage/simple.html"));
        Assert.AreEqual(1, coverage[0].Ranges.Count);
        var range = coverage[0].Ranges[0];
        var rangeComparer = new PageStopCSSCoverageResultRangeComparer();
        Assert.IsTrue(rangeComparer.Equals(new PageStopCSSCoverageResultRange { Start = 1, End = 22 }, range));
        Assert.AreEqual("div { color: green; }", coverage[0].Text.Substring(range.Start, range.End - range.Start));
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report sourceURLs")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportSourceURLs()
    {
        await Page.Coverage.StartCSSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/csscoverage/sourceurl.html");
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        Assert.AreEqual("nicename.css", coverage[0].Url);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report multiple stylesheets")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportMultipleStylesSheets()
    {
        await Page.Coverage.StartCSSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/csscoverage/multiple.html");
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(2, coverage.Count);
        coverage.Sort((c1, c2) => c1.Url.CompareTo(c2.Url));
        Assert.IsTrue(coverage[0].Url.Contains("/csscoverage/stylesheet1.css"));
        Assert.IsTrue(coverage[1].Url.Contains("/csscoverage/stylesheet2.css"));
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report stylesheets that have no coverage")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportStyleSheetsThatHaveNoCoverage()
    {
        await Page.Coverage.StartCSSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/csscoverage/unused.html");
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        Assert.AreEqual("unused.css", coverage[0].Url);
        Assert.AreEqual(0, coverage[0].Ranges.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should work with media queries")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithMediaQueries()
    {
        if (ChromiumVersionLessThan(Browser.Version, "110.0.5451.0"))
        {
            // https://chromium-review.googlesource.com/c/chromium/src/+/4051280
            return;
        }

        await Page.Coverage.StartCSSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/csscoverage/media.html");
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        Assert.IsTrue(coverage[0].Url.Contains("/csscoverage/media.html"));
        Assert.AreEqual(1, coverage[0].Ranges.Count);
        var rangeComparer = new PageStopCSSCoverageResultRangeComparer();
        Assert.IsTrue(rangeComparer.Equals(new PageStopCSSCoverageResultRange { Start = 8, End = 10 }, coverage[0].Ranges[0]));
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should work with complicated usecases")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithComplicatedUsecases()
    {
        if (ChromiumVersionLessThan(Browser.Version, "110.0.5451.0"))
        {
            // https://chromium-review.googlesource.com/c/chromium/src/+/4051280
            return;
        }

        await Page.Coverage.StartCSSCoverageAsync();
        await Page.GotoAsync(Server.Prefix + "/csscoverage/involved.html");
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
        coverage[0].Ranges.Sort((r1, r2) => r1.Start.CompareTo(r2.Start));
        var rangeComparer = new PageStopCSSCoverageResultRangeComparer();
        Assert.IsTrue(rangeComparer.Equals(new PageStopCSSCoverageResultRange { Start = 149, End = 297 }, coverage[0].Ranges[0]));
        Assert.IsTrue(rangeComparer.Equals(new PageStopCSSCoverageResultRange { Start = 306, End = 435 }, coverage[0].Ranges[1]));
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should ignore injected stylesheets")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldIgnoreInjectedStyleSheets()
    {
        await Page.Coverage.StartCSSCoverageAsync();
        await Page.AddStyleTagAsync(new() { Content = "body { margin: 10px;}" });
        // trigger style recalc
        var margin = await Page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).margin");
        Assert.AreEqual("10px", margin);

        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(0, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should report stylesheets across navigations")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportStyleSheetsAcrossNavigations()
    {
        await Page.Coverage.StartCSSCoverageAsync(new PageStartCSSCoverageOptions { ResetOnNavigation = false });
        await Page.GotoAsync(Server.Prefix + "/csscoverage/multiple.html");
        await Page.GotoAsync(Server.EmptyPage);
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(2, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should NOT report scripts across navigations")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldNOTReportScriptsAcrossNavigations()
    {
        await Page.Coverage.StartCSSCoverageAsync(); // Enabled by default
        await Page.GotoAsync(Server.Prefix + "/csscoverage/multiple.html");
        await Page.GotoAsync(Server.EmptyPage);
        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(0, coverage.Count);
    }

    [PlaywrightTest("chromium/js-coverage.spec.ts", "should work with a recently loaded stylesheet")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldWorkWithRecentlyLoadedStyleSheet()
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
        }", Server.Prefix + "/csscoverage/stylesheet1.css");

        var coverage = await Page.Coverage.StopCSSCoverageAsync();

        Assert.AreEqual(1, coverage.Count);
    }

    private class PageStopCSSCoverageResultRangeComparer : IEqualityComparer<PageStopCSSCoverageResultRange>
    {
        public bool Equals(PageStopCSSCoverageResultRange x, PageStopCSSCoverageResultRange y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            return (x.Start == y.Start) && (x.End == y.End);
        }

        public int GetHashCode(PageStopCSSCoverageResultRange obj)
        {
            throw new NotImplementedException();
        }
    }

    private bool ChromiumVersionLessThan(string v1, string v2)
    {
        var left = v1.Split('.');
        var right = v2.Split('.');
        for (var i = 0; i < 4; i++)
        {
            var leftNum = int.Parse(left[i], CultureInfo.InvariantCulture);
            var rightNum = int.Parse(right[i], CultureInfo.InvariantCulture);

            if (leftNum > rightNum)
            {
                return false;
            }
            if (leftNum < rightNum)
            {
                return true;
            }
        }
        return false;
    }
}
