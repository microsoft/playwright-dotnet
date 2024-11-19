/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

public class PageAriaSnapshotTests : PageTestEx
{
    private string _unshift(string snapshot)
    {
        var lines = snapshot.Split('\n');
        var whitespacePrefixLength = 100;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var match = System.Text.RegularExpressions.Regex.Match(line, @"^(\s*)");
            if (match.Success && match.Groups[1].Value.Length < whitespacePrefixLength)
                whitespacePrefixLength = match.Groups[1].Value.Length;
            break;
        }
        return string.Join('\n', lines.Where(t => !string.IsNullOrWhiteSpace(t)).Select(line => line.Substring(whitespacePrefixLength)));
    }

    private async Task CheckAndMatchSnapshot(ILocator locator, string snapshot)
    {
        Assert.AreEqual(_unshift(snapshot), await locator.AriaSnapshotAsync());
        await Expect(locator).ToMatchAriaSnapshotAsync(snapshot);
    }

    [PlaywrightTest("page-aria-snapshot.spec.ts", "should snapshot")]
    public async Task ShouldSnapshot()
    {
        await Page.SetContentAsync("<h1>title</h1>");
        await CheckAndMatchSnapshot(Page.Locator("body"), @"
            - heading ""title"" [level=1]
        ");
    }

    [PlaywrightTest("page-aria-snapshot.spec.ts", "should snapshot list")]
    public async Task ShouldSnapshotList()
    {
        await Page.SetContentAsync(@"
            <h1>title</h1>
            <h1>title 2</h1>
        ");
        await CheckAndMatchSnapshot(Page.Locator("body"), @"
            - heading ""title"" [level=1]
            - heading ""title 2"" [level=1]
        ");
    }

    [PlaywrightTest("page-aria-snapshot.spec.ts", "should snapshot list with accessible name")]
    public async Task ShouldSnapshotListWithAccessibleName()
    {
        await Page.SetContentAsync(@"
            <ul aria-label=""my list"">
                <li>one</li>
                <li>two</li>
            </ul>
        ");
        await CheckAndMatchSnapshot(Page.Locator("body"), @"
            - list ""my list"":
              - listitem: one
              - listitem: two
        ");
    }

    [PlaywrightTest("page-aria-snapshot.spec.ts", "should snapshot complex")]
    public async Task ShouldSnapshotComplex()
    {
        await Page.SetContentAsync(@"
            <ul>
                <li>
                    <a href='about:blank'>link</a>
                </li>
            </ul>
        ");
        await CheckAndMatchSnapshot(Page.Locator("body"), @"
            - list:
              - listitem:
                - link ""link""
        ");
    }
}
