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

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// WebStorage exposes the page's <c>localStorage</c> or <c>sessionStorage</c> for the
/// current origin via an async, <a href="https://developer.mozilla.org/en-US/docs/Web/API/Storage">browser-consistent</a>
/// API.
/// </para>
/// <para>Instances are accessed through <see cref="IPage.LocalStorage"/> and <see cref="IPage.SessionStorage"/>.</para>
/// <code>
/// await page.GotoAsync("https://example.com");<br/>
/// await page.LocalStorage.SetItemAsync("token", "abc");<br/>
/// var token = await page.LocalStorage.GetItemAsync("token");<br/>
/// var all = await page.LocalStorage.ItemsAsync();<br/>
/// await page.LocalStorage.RemoveItemAsync("token");<br/>
/// await page.LocalStorage.ClearAsync();
/// </code>
/// </summary>
public partial interface IWebStorage
{
    /// <summary><para>Returns all items in the storage as name/value pairs.</para></summary>
    Task<IReadOnlyList<WebStorageItem>> ItemsAsync();

    /// <summary><para>Returns the value for the given <see cref="IWebStorage.GetItemAsync"/> if present.</para></summary>
    /// <param name="name">Name of the item to retrieve.</param>
    Task<string?> GetItemAsync(string name);

    /// <summary>
    /// <para>
    /// Sets the value for the given <see cref="IWebStorage.SetItemAsync"/>. Overwrites
    /// any existing value for that name.
    /// </para>
    /// </summary>
    /// <param name="name">Name of the item to set.</param>
    /// <param name="value">New value for the item.</param>
    Task SetItemAsync(string name, string value);

    /// <summary>
    /// <para>
    /// Removes the item with the given <see cref="IWebStorage.RemoveItemAsync"/>. No-op
    /// if the item is absent.
    /// </para>
    /// </summary>
    /// <param name="name">Name of the item to remove.</param>
    Task RemoveItemAsync(string name);

    /// <summary><para>Removes all items from the storage.</para></summary>
    Task ClearAsync();
}
