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


#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Playwright gives you Web-First Assertions with convenience methods for creating
/// assertions that will wait and retry until the expected condition is met.
/// </para>
/// <para>Consider the following example:</para>
/// <code>
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
///     public async Task StatusBecomesSubmitted()<br/>
///     {<br/>
///         await Page.Locator("#submit-button").ClickAsync();<br/>
///         await Expect(Page.Locator(".status")).ToHaveTextAsync("Submitted");<br/>
///     }<br/>
/// }
/// </code>
/// <para>
/// Playwright will be re-testing the node with the selector <c>.status</c> until fetched
/// Node has the <c>"Submitted"</c> text. It will be re-fetching the node and checking
/// it over and over, until the condition is met or until the timeout is reached. You
/// can pass this timeout as an option.
/// </para>
/// <para>By default, the timeout for assertions is set to 5 seconds.</para>
/// </summary>
public partial interface IPlaywrightAssertions
{
    /// <summary><para>Creates a <see cref="IAPIResponseAssertions"/> object for the given <see cref="IAPIResponse"/>.</para></summary>
    /// <param name="response"><see cref="IAPIResponse"/> object to use for assertions.</param>
    IAPIResponseAssertions Expect(IAPIResponse response);

    /// <summary>
    /// <para>Creates a <see cref="ILocatorAssertions"/> object for the given <see cref="ILocator"/>.</para>
    /// <code>await Expect(locator).ToBeVisibleAsync();</code>
    /// </summary>
    /// <param name="locator"><see cref="ILocator"/> object to use for assertions.</param>
    ILocatorAssertions Expect(ILocator locator);

    /// <summary>
    /// <para>Creates a <see cref="IPageAssertions"/> object for the given <see cref="IPage"/>.</para>
    /// <code>await Expect(page).ToHaveTitleAsync("News");</code>
    /// </summary>
    /// <param name="page"><see cref="IPage"/> object to use for assertions.</param>
    IPageAssertions Expect(IPage page);
}

#nullable disable
