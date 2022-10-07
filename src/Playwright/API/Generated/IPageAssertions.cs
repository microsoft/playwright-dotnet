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

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// The <see cref="IPageAssertions"/> class provides assertion methods that can be used
/// to make assertions about the <see cref="IPage"/> state in the tests. A new instance
/// of <see cref="IPageAssertions"/> is created by calling <see cref="IPlaywrightAssertions.Expect"/>:
/// </para>
/// <code>
/// using System.Text.RegularExpressions;<br/>
/// using System.Threading.Tasks;<br/>
/// using Microsoft.Playwright.NUnit;<br/>
/// using NUnit.Framework;<br/>
/// <br/>
/// namespace PlaywrightTests;<br/>
/// <br/>
/// [TestFixture]<br/>
/// public class ExampleTests : PageTest<br/>
/// {<br/>
///     [Test]<br/>
///     public async Task NavigatetoLoginPage()<br/>
///     {<br/>
///         // ..<br/>
///         await Page.GetByText("Sing in").ClickAsync();<br/>
///         await Expect(Page.Locator("div#foobar")).ToHaveURL(new Regex(".*/login"));<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
public partial interface IPageAssertions
{
    /// <summary>
    /// <para>
    /// Makes the assertion check for the opposite condition. For example, this code tests
    /// that the page URL doesn't contain <c>"error"</c>:
    /// </para>
    /// <code>await Expect(page).Not.ToHaveURL("error");</code>
    /// </summary>
    public IPageAssertions Not { get; }

    /// <summary>
    /// <para>Ensures the page has the given title.</para>
    /// <code>await Expect(page).ToHaveTitle("Playwright");</code>
    /// </summary>
    /// <param name="titleOrRegExp">Expected title or RegExp.</param>
    /// <param name="options">Call options</param>
    Task ToHaveTitleAsync(string titleOrRegExp, PageAssertionsToHaveTitleOptions? options = default);

    /// <summary>
    /// <para>Ensures the page has the given title.</para>
    /// <code>await Expect(page).ToHaveTitle("Playwright");</code>
    /// </summary>
    /// <param name="titleOrRegExp">Expected title or RegExp.</param>
    /// <param name="options">Call options</param>
    Task ToHaveTitleAsync(Regex titleOrRegExp, PageAssertionsToHaveTitleOptions? options = default);

    /// <summary>
    /// <para>Ensures the page is navigated to the given URL.</para>
    /// <code>await Expect(page).ToHaveURL(new Regex(".*checkout"));</code>
    /// </summary>
    /// <param name="urlOrRegExp">Expected URL string or RegExp.</param>
    /// <param name="options">Call options</param>
    Task ToHaveURLAsync(string urlOrRegExp, PageAssertionsToHaveURLOptions? options = default);

    /// <summary>
    /// <para>Ensures the page is navigated to the given URL.</para>
    /// <code>await Expect(page).ToHaveURL(new Regex(".*checkout"));</code>
    /// </summary>
    /// <param name="urlOrRegExp">Expected URL string or RegExp.</param>
    /// <param name="options">Call options</param>
    Task ToHaveURLAsync(Regex urlOrRegExp, PageAssertionsToHaveURLOptions? options = default);
}

#nullable disable
