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

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Playwright module provides a method to launch a browser instance. The following
/// is a typical example of using Playwright to drive automation:
/// </para>
/// <code>
/// using Microsoft.Playwright;<br/>
/// using System.Threading.Tasks;<br/>
/// <br/>
/// class PlaywrightExample<br/>
/// {<br/>
///     public static async Task Main()<br/>
///     {<br/>
///         using var playwright = await Playwright.CreateAsync();<br/>
///         await using var browser = await playwright.Chromium.LaunchAsync();<br/>
///         var page = await browser.NewPageAsync();<br/>
/// <br/>
///         await page.GotoAsync("https://www.microsoft.com");<br/>
///         // other actions...<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
public partial interface IPlaywright
{
    /// <summary>
    /// <para>
    /// This object can be used to launch or connect to Chromium, returning instances of
    /// <see cref="IBrowser"/>.
    /// </para>
    /// </summary>
    public IBrowserType Chromium { get; }

    /// <summary>
    /// <para>
    /// Returns a dictionary of devices to be used with <see cref="IBrowser.NewContextAsync"/>
    /// or <see cref="IBrowser.NewPageAsync"/>.
    /// </para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class PlaywrightExample<br/>
    /// {<br/>
    ///     public static async Task Main()<br/>
    ///     {<br/>
    ///         using var playwright = await Playwright.CreateAsync();<br/>
    ///         await using var browser = await playwright.Webkit.LaunchAsync();<br/>
    ///         await using var context = await browser.NewContextAsync(playwright.Devices["iPhone 6"]);<br/>
    /// <br/>
    ///         var page = await context.NewPageAsync();<br/>
    ///         await page.GotoAsync("https://www.theverge.com");<br/>
    ///         // other actions...<br/>
    ///     }<br/>
    /// }
    /// </code>
    /// </summary>
    public IReadOnlyDictionary<string, BrowserNewContextOptions> Devices { get; }

    /// <summary>
    /// <para>
    /// This object can be used to launch or connect to Firefox, returning instances of
    /// <see cref="IBrowser"/>.
    /// </para>
    /// </summary>
    public IBrowserType Firefox { get; }

    /// <summary><para>Exposes API that can be used for the Web API testing.</para></summary>
    public IAPIRequest APIRequest { get; }

    /// <summary>
    /// <para>
    /// Selectors can be used to install custom selector engines. See <a href="https://playwright.dev/dotnet/docs/selectors">Working
    /// with selectors</a> for more information.
    /// </para>
    /// </summary>
    public ISelectors Selectors { get; }

    /// <summary>
    /// <para>
    /// This object can be used to launch or connect to WebKit, returning instances of <see
    /// cref="IBrowser"/>.
    /// </para>
    /// </summary>
    public IBrowserType Webkit { get; }
}

#nullable disable
