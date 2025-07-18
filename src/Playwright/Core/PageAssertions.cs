/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class PageAssertions : AssertionsBase, IPageAssertions
{
    private readonly Page _page;

    public PageAssertions(IPage page, bool isNot) : base(isNot)
    {
        _page = (Page)page;
    }

    public IPageAssertions Not => new PageAssertions(_page, !IsNot);

    protected override Task<FrameExpectResult> CallExpectAsync(string expression, FrameExpectOptions expectOptions, string title)
    {
        var frame = _page.MainFrame;
        return frame.WrapApiCallAsync(
              () => frame.ExpectAsync(null, expression, expectOptions),
              false,
              title);
    }

    public Task ToHaveTitleAsync(string titleOrRegExp, PageAssertionsToHaveTitleOptions? options = null) =>
        ExpectImplAsync("to.have.title", new ExpectedTextValue() { String = titleOrRegExp, NormalizeWhiteSpace = true }, titleOrRegExp, "Page title expected to be", "Expect \"ToHaveTitleAsync\"", ConvertToFrameExpectOptions(options));

    public Task ToHaveTitleAsync(Regex titleOrRegExp, PageAssertionsToHaveTitleOptions? options = null) =>
        ExpectImplAsync("to.have.title", ExpectedRegex(titleOrRegExp, new() { NormalizeWhiteSpace = true }), titleOrRegExp, "Page title expected to be", "Expect \"ToHaveTitleAsync\"", ConvertToFrameExpectOptions(options));

    public Task ToHaveURLAsync(string urlOrRegExp, PageAssertionsToHaveURLOptions? options = null) =>
        ExpectImplAsync("to.have.url", new ExpectedTextValue() { String = URLMatch.ConstructURLBasedOnBaseURL(_page.Context.BaseURL, urlOrRegExp), IgnoreCase = options?.IgnoreCase }, urlOrRegExp, "Page URL expected to be", "Expect \"ToHaveURLAsync\"", ConvertToFrameExpectOptions(options));

    public Task ToHaveURLAsync(Regex urlOrRegExp, PageAssertionsToHaveURLOptions? options = null) =>
        ExpectImplAsync("to.have.url", ExpectedRegex(urlOrRegExp, new() { IgnoreCase = options?.IgnoreCase }), urlOrRegExp, "Page URL expected to match regex", "Expect \"ToHaveURLAsync\"", ConvertToFrameExpectOptions(options));
}
