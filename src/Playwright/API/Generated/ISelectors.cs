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

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Selectors can be used to install custom selector engines. See <a href="https://playwright.dev/dotnet/docs/selectors">Working
/// with selectors</a> for more information.
/// </para>
/// </summary>
public partial interface ISelectors
{
    /// <summary>
    /// <para>An example of registering selector engine that queries elements based on a tag name:</para>
    /// <code>
    /// using var playwright = await Playwright.CreateAsync();<br/>
    /// // Script that evaluates to a selector engine instance. The script is evaluated in the page context.<br/>
    /// await playwright.Selectors.RegisterAsync("tag", @"{<br/>
    /// // Returns the first element matching given selector in the root's subtree.<br/>
    /// query(root, selector) {<br/>
    ///     return root.querySelector(selector);<br/>
    ///   },<br/>
    ///   // Returns all elements matching given selector in the root's subtree.<br/>
    ///   queryAll(root, selector) {<br/>
    ///     return Array.from(root.querySelectorAll(selector));<br/>
    ///   }<br/>
    /// }");<br/>
    /// <br/>
    /// await using var browser = await playwright.Chromium.LaunchAsync();<br/>
    /// var page = await browser.NewPageAsync();<br/>
    /// await page.SetContentAsync("&lt;div&gt;&lt;button&gt;Click me&lt;/button&gt;&lt;/div&gt;");<br/>
    /// // Use the selector prefixed with its name.<br/>
    /// var button = page.Locator("tag=button");<br/>
    /// // Combine it with other selector engines.<br/>
    /// await page.Locator("tag=div &gt;&gt; text=\"Click me\"").ClickAsync();<br/>
    /// // Can use it in any methods supporting selectors.<br/>
    /// int buttonCount = await page.Locator("tag=button").CountAsync();
    /// </code>
    /// </summary>
    /// <param name="name">
    /// Name that is used in selectors as a prefix, e.g. <c>{name: 'foo'}</c> enables <c>foo=myselectorbody</c>
    /// selectors. May only contain <c>[a-zA-Z0-9_]</c> characters.
    /// </param>
    /// <param name="options">Call options</param>
    Task RegisterAsync(string name, SelectorsRegisterOptions? options = default);

    /// <summary>
    /// <para>
    /// Defines custom attribute name to be used in <see cref="IPage.GetByTestId"/>. <c>data-testid</c>
    /// is used by default.
    /// </para>
    /// </summary>
    /// <param name="attributeName">Test id attribute name.</param>
    void SetTestIdAttribute(string attributeName);
}

#nullable disable
