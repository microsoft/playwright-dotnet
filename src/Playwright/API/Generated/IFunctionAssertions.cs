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

using System.Threading.Tasks;

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// The <see cref="IFunctionAssertions"/> class provides assertion methods that retry
/// a block of code. A new instance of <see cref="IFunctionAssertions"/> is created
/// by calling <see cref="IPlaywrightAssertions.Expect"/>:
/// </para>
/// <code>
/// using Microsoft.Playwright;<br/>
/// using Microsoft.Playwright.MSTest;<br/>
/// <br/>
/// namespace PlaywrightTests;<br/>
/// <br/>
/// [TestClass]<br/>
/// public class ExampleTests : PageTest<br/>
/// {<br/>
///     [TestMethod]<br/>
///     public async Task ApiEventuallySucceeds()<br/>
///     {<br/>
///         await Expect(async () =&gt;<br/>
///         {<br/>
///             var response = await Page.APIRequest.GetAsync("https://api.example.com");<br/>
///             Assert.AreEqual(200, response.Status);<br/>
///         }).ToPassAsync();<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
public partial interface IFunctionAssertions
{
    /// <summary>
    /// <para>
    /// Retries the callback until it completes without throwing an exception, or until
    /// the timeout is reached. When the timeout is reached, throws a <see cref="PlaywrightAssertionException"/>
    /// with the last exception that the callback has thrown.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await Expect(async () =&gt;<br/>
    /// {<br/>
    ///     var response = await Page.APIRequest.GetAsync("https://api.example.com");<br/>
    ///     Assert.AreEqual(200, response.Status);<br/>
    /// }).ToPassAsync(new()<br/>
    /// {<br/>
    ///     // Probe, wait 1s, probe, wait 2s, probe, wait 10s, probe, wait 10s, probe<br/>
    ///     Intervals = [1_000, 2_000, 10_000],<br/>
    ///     Timeout = 60_000,<br/>
    /// });
    /// </code>
    /// </summary>
    /// <param name="options">Call options</param>
    Task ToPassAsync(FunctionAssertionsToPassOptions? options = default);
}
