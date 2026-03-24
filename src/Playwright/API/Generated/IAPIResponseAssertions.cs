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
/// The <see cref="IAPIResponseAssertions"/> class provides assertion methods that can
/// be used to make assertions about the <see cref="IAPIResponse"/> in the tests.
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
///     public async Task NavigatesToLoginPage()<br/>
///     {<br/>
///         var response = await Page.APIRequest.GetAsync("https://playwright.dev");<br/>
///         await Expect(response).ToBeOKAsync();<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
public partial interface IAPIResponseAssertions
{
    /// <summary>
    /// <para>Makes the assertion check for the opposite condition.</para>
    /// <para>**Usage**</para>
    /// <para>For example, this code tests that the response status is not successful:</para>
    /// <code>await Expect(response).Not.ToBeOKAsync();</code>
    /// </summary>
    public IAPIResponseAssertions Not { get; }

    /// <summary>
    /// <para>Ensures the response status code is within <c>200..299</c> range.</para>
    /// <para>**Usage**</para>
    /// <code>await Expect(response).ToBeOKAsync();</code>
    /// </summary>
    Task ToBeOKAsync();
}
