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

using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// **DEPRECATED** This class is deprecated. Please use other libraries such as <a href="https://www.deque.com/axe/">Axe</a>
/// if you need to test page accessibility. See our Node.js <a href="https://playwright.dev/docs/accessibility-testing">guide</a>
/// for integration with Axe.
/// </para>
/// <para>
/// The Accessibility class provides methods for inspecting Chromium's accessibility
/// tree. The accessibility tree is used by assistive technology such as <a href="https://en.wikipedia.org/wiki/Screen_reader">screen
/// readers</a> or <a href="https://en.wikipedia.org/wiki/Switch_access">switches</a>.
/// </para>
/// <para>
/// Accessibility is a very platform-specific thing. On different platforms, there are
/// different screen readers that might have wildly different output.
/// </para>
/// <para>
/// Rendering engines of Chromium, Firefox and WebKit have a concept of "accessibility
/// tree", which is then translated into different platform-specific APIs. Accessibility
/// namespace gives access to this Accessibility Tree.
/// </para>
/// <para>
/// Most of the accessibility tree gets filtered out when converting from internal browser
/// AX Tree to Platform-specific AX-Tree or by assistive technologies themselves. By
/// default, Playwright tries to approximate this filtering, exposing only the "interesting"
/// nodes of the tree.
/// </para>
/// </summary>
public partial interface IAccessibility
{
    /// <summary>
    /// <para>
    /// **DEPRECATED** This method is deprecated. Please use other libraries such as <a
    /// href="https://www.deque.com/axe/">Axe</a> if you need to test page accessibility.
    /// See our Node.js <a href="https://playwright.dev/docs/accessibility-testing">guide</a>
    /// for integration with Axe.
    /// </para>
    /// <para>
    /// Captures the current state of the accessibility tree. The returned object represents
    /// the root accessible node of the page.
    /// </para>
    /// <para>An example of dumping the entire accessibility tree:</para>
    /// <code>
    /// var accessibilitySnapshot = await page.Accessibility.SnapshotAsync();<br/>
    /// Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(accessibilitySnapshot));
    /// </code>
    /// <para>An example of logging the focused node's name:</para>
    /// <code>
    /// var accessibilitySnapshot = await page.Accessibility.SnapshotAsync();<br/>
    /// Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(accessibilitySnapshot));
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Chromium accessibility tree contains nodes that go unused on most platforms
    /// and by most screen readers. Playwright will discard them as well for an easier to
    /// process tree, unless <paramref name="interestingOnly"/> is set to <c>false</c>.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    [System.Obsolete]
    Task<JsonElement?> SnapshotAsync(AccessibilitySnapshotOptions? options = default);
}

#nullable disable
