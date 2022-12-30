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

using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Coverage gathers information about parts of JavaScript and CSS that were used by the page.
/// </para>
/// <para>Coverage APIs are only supported on Chromium-based browsers.</para>
/// <para>An example of using JavaScript coverage in V8 format page load:</para>
/// <code>
/// using Microsoft.Playwright;<br/>
/// using System.Threading.Tasks;<br/>
/// <br/>
/// class PageExamples<br/>
/// {<br/>
///     public static async Task Run()<br/>
///     {<br/>
///         using var playwright = await Playwright.CreateAsync();<br/>
///         await using var browser = await playwright.Webkit.LaunchAsync();<br/>
///         var page = await browser.NewPageAsync();<br/>
///         await page.Coverage.StartJSCoverageAsync();<br/>
///         await page.GotoAsync("https://www.theverge.com");<br/>
///         var coverage = await Page.Coverage.StopJSCoverageAsync();<br/>
///         Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(coverage));<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
public partial interface ICoverage
{
    /// <summary>
    /// Starts collecting CSS code coverage when using Chrome
    /// </summary>
    /// <param name="options">Call options</param>
    Task StartCSSCoverageAsync(PageStartCSSCoverageOptions? options = default);

    /// <summary>
    /// Starts collecting JS code coverage when using Chrome
    /// </summary>
    /// <param name="options">Call options</param>
    Task StartJSCoverageAsync(PageStartJSCoverageOptions? options = default);

    /// <summary>
    /// Stops collecting code coverage
    /// </summary>
    /// <returns>The array of coverage reports for all stylesheets</returns>
    /// <remarks>
    /// <para>CSS Coverage doesn't include dynamically injected style tags without sourceURLs.</para>
    /// </remarks>
    Task<List<PageStopCSSCoverageResult>> StopCSSCoverageAsync();

    /// <summary>
    /// Stops collecting code coverage
    /// </summary>
    /// <returns>The array of coverage reports for all scripts</returns>
    /// <remarks>
    /// <para>JavaScript Coverage doesn't include anonymous scripts by default. However, scripts with sourceURLs are reported.</para>
    /// </remarks>
    Task<List<PageStopJSCoverageResult>> StopJSCoverageAsync();
}
#nullable disable
