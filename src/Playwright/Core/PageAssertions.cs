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

    public PageAssertions(IPage page, bool isNot, string? customMessage = null) : base(isNot, customMessage)
    {
        _page = (Page)page;
    }

    public IPageAssertions Not => new PageAssertions(_page, !IsNot, CustomMessage);

    protected override Task<FrameExpectResult> CallExpectAsync(string expression, FrameExpectOptions expectOptions, string title)
    {
        var frame = _page.MainFrame;
        return frame.WrapApiCallAsync(
              () => frame.ExpectAsync(null, expression, expectOptions),
              false,
              title);
    }

    public Task ToHaveTitleAsync(string titleOrRegExp, PageAssertionsToHaveTitleOptions? options = null)
        => ToHaveTitleAsync((object)titleOrRegExp, options);

    public Task ToHaveTitleAsync(Regex titleOrRegExp, PageAssertionsToHaveTitleOptions? options = null)
        => ToHaveTitleAsync((object)titleOrRegExp, options);

    public Task ToHaveURLAsync(string urlOrRegExp, PageAssertionsToHaveURLOptions? options = null)
        => ToHaveURLAsync((object)URLMatch.ConstructURLBasedOnBaseURL(_page.Context.BaseURL, urlOrRegExp), options);

    public Task ToHaveURLAsync(Regex urlOrRegExp, PageAssertionsToHaveURLOptions? options = null)
        => ToHaveURLAsync((object)urlOrRegExp, options);

    public Task ToMatchAriaSnapshotAsync(string expected, PageAssertionsToMatchAriaSnapshotOptions? options = null)
        => ToMatchAriaSnapshotAsync(expected, ConvertToFrameExpectOptions(options));

    private Task ToHaveTitleAsync(object expected, PageAssertionsToHaveTitleOptions? options)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpectedText = ExpectedText(expected, new() { NormalizeWhiteSpace = true });
        return ToMatchTextAsync("ToHaveTitleAsync", "to.have.title", expected, frameExpectOptions);
    }

    private Task ToHaveURLAsync(object expected, PageAssertionsToHaveURLOptions? options)
    {
        var frameExpectOptions = ConvertToFrameExpectOptions(options);
        frameExpectOptions.ExpectedText = ExpectedText(expected, new() { IgnoreCase = options?.IgnoreCase });
        return ToMatchTextAsync("ToHaveURLAsync", "to.have.url", expected, frameExpectOptions);
    }
}
