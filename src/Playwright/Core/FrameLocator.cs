/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Copyright (c) 2020 Meir Blachman
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

using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Core;

internal class FrameLocator : IFrameLocator
{
    private readonly Frame _frame;
    private readonly string _frameSelector;

    public FrameLocator(Frame parent, string selector)
    {
        _frame = parent;
        _frameSelector = selector;
    }

    IFrameLocator IFrameLocator.First => new FrameLocator(_frame, $"{_frameSelector} >> nth=0");

    IFrameLocator IFrameLocator.Last => new FrameLocator(_frame, $"{_frameSelector} >> nth=-1");

    ILocator IFrameLocator.GetByAltText(string text, FrameLocatorGetByAltTextOptions options)
        => Locator(Core.Locator.GetByAltTextSelector(text, options?.Exact));

    public ILocator GetByAltText(Regex text, FrameLocatorGetByAltTextOptions options = null)
        => Locator(Core.Locator.GetByAltTextSelector(text, options?.Exact));

    public ILocator GetByLabel(string text, FrameLocatorGetByLabelOptions options = null)
        => Locator(Core.Locator.GetByLabelSelector(text, options?.Exact));

    public ILocator GetByLabel(Regex text, FrameLocatorGetByLabelOptions options = null)
        => Locator(Core.Locator.GetByLabelSelector(text, options?.Exact));

    public ILocator GetByPlaceholder(string text, FrameLocatorGetByPlaceholderOptions options = null)
        => Locator(Core.Locator.GetByPlaceholderSelector(text, options?.Exact));

    public ILocator GetByPlaceholder(Regex text, FrameLocatorGetByPlaceholderOptions options = null)
        => Locator(Core.Locator.GetByPlaceholderSelector(text, options?.Exact));

    public ILocator GetByRole(AriaRole role, FrameLocatorGetByRoleOptions options = null)
        => Locator(Core.Locator.GetByRoleSelector(role, new(options)));

    public ILocator GetByTestId(string testId)
        => Locator(Core.Locator.GetByTestIdSelector(testId));

    public ILocator GetByText(string text, FrameLocatorGetByTextOptions options = null)
        => Locator(Core.Locator.GetByTextSelector(text, options?.Exact));

    public ILocator GetByText(Regex text, FrameLocatorGetByTextOptions options = null)
        => Locator(Core.Locator.GetByTextSelector(text, options?.Exact));

    public ILocator GetByTitle(string text, FrameLocatorGetByTitleOptions options = null)
        => Locator(Core.Locator.GetByTitleSelector(text, options?.Exact));

    public ILocator GetByTitle(Regex text, FrameLocatorGetByTitleOptions options = null)
        => Locator(Core.Locator.GetByTitleSelector(text, options?.Exact));

    IFrameLocator IFrameLocator.FrameLocator(string selector) => new FrameLocator(_frame, $"{_frameSelector} >> internal:control=enter-frame  >> {selector}");

    public ILocator Locator(string selector, FrameLocatorLocatorOptions options = null) => new Locator(_frame, $"{_frameSelector} >> internal:control=enter-frame  >> {selector}", new() { HasTextRegex = options?.HasTextRegex, HasTextString = options?.HasTextString });

    public IFrameLocator Nth(int index) => new FrameLocator(_frame, $"{_frameSelector} >> nth={index}");
}
