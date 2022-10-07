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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// A Browser is created via <see cref="IBrowserType.LaunchAsync"/>. An example of using
/// a <see cref="IBrowser"/> to create a <see cref="IPage"/>:
/// </para>
/// <code>
/// using Microsoft.Playwright;<br/>
/// <br/>
/// using var playwright = await Playwright.CreateAsync();<br/>
/// var firefox = playwright.Firefox;<br/>
/// var browser = await firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });<br/>
/// var page = await browser.NewPageAsync();<br/>
/// await page.GotoAsync("https://www.bing.com");<br/>
/// await browser.CloseAsync();
/// </code>
/// </summary>
public partial interface IBrowser
{
    /// <summary>
    /// <para>
    /// Emitted when Browser gets disconnected from the browser application. This might
    /// happen because of one of the following:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Browser application is closed or crashed.</description></item>
    /// <item><description>The <see cref="IBrowser.CloseAsync"/> method was called.</description></item>
    /// </list>
    /// </summary>
    event EventHandler<IBrowser> Disconnected;

    /// <summary><para>Get the browser type (chromium, firefox or webkit) that the browser belongs to.</para></summary>
    IBrowserType BrowserType { get; }

    /// <summary>
    /// <para>
    /// In case this browser is obtained using <see cref="IBrowserType.LaunchAsync"/>, closes
    /// the browser and all of its pages (if any were opened).
    /// </para>
    /// <para>
    /// In case this browser is connected to, clears all created contexts belonging to this
    /// browser and disconnects from the browser server.
    /// </para>
    /// <para>
    /// The <see cref="IBrowser"/> object itself is considered to be disposed and cannot
    /// be used anymore.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is similar to force quitting the browser. Therefore, you should call <see cref="IBrowserContext.CloseAsync"/>
    /// on any <see cref="IBrowserContext"/>'s you explicitly created earlier with <see
    /// cref="IBrowser.NewContextAsync"/> **before** calling <see cref="IBrowser.CloseAsync"/>.
    /// </para>
    /// </remarks>
    Task CloseAsync();

    /// <summary>
    /// <para>
    /// Returns an array of all open browser contexts. In a newly created browser, this
    /// will return zero browser contexts.
    /// </para>
    /// <code>
    /// using var playwright = await Playwright.CreateAsync();<br/>
    /// var browser = await playwright.Webkit.LaunchAsync();<br/>
    /// System.Console.WriteLine(browser.Contexts.Count); // prints "0"<br/>
    /// var context = await browser.NewContextAsync();<br/>
    /// System.Console.WriteLine(browser.Contexts.Count); // prints "1"
    /// </code>
    /// </summary>
    IReadOnlyList<IBrowserContext> Contexts { get; }

    /// <summary><para>Indicates that the browser is connected.</para></summary>
    bool IsConnected { get; }

    /// <summary>
    /// <para>Creates a new browser context. It won't share cookies/cache with other browser contexts.</para>
    /// <code>
    /// using var playwright = await Playwright.CreateAsync();<br/>
    /// var browser = await playwright.Firefox.LaunchAsync();<br/>
    /// // Create a new incognito browser context.<br/>
    /// var context = await browser.NewContextAsync();<br/>
    /// // Create a new page in a pristine context.<br/>
    /// var page = await context.NewPageAsync(); ;<br/>
    /// await page.GotoAsync("https://www.bing.com");<br/>
    /// <br/>
    /// // Gracefully close up everything<br/>
    /// await context.CloseAsync();<br/>
    /// await browser.CloseAsync();
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// If directly using this method to create <see cref="IBrowserContext"/>s, it is best
    /// practice to explicitly close the returned context via <see cref="IBrowserContext.CloseAsync"/>
    /// when your code is done with the <see cref="IBrowserContext"/>, and before calling
    /// <see cref="IBrowser.CloseAsync"/>. This will ensure the <c>context</c> is closed
    /// gracefully and any artifacts—like HARs and videos—are fully flushed and saved.
    /// </para>
    /// </remarks>
    /// <param name="options">Call options</param>
    Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions? options = default);

    /// <summary>
    /// <para>
    /// Creates a new page in a new browser context. Closing this page will close the context
    /// as well.
    /// </para>
    /// <para>
    /// This is a convenience API that should only be used for the single-page scenarios
    /// and short snippets. Production code and testing frameworks should explicitly create
    /// <see cref="IBrowser.NewContextAsync"/> followed by the <see cref="IBrowserContext.NewPageAsync"/>
    /// to control their exact life times.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IPage> NewPageAsync(BrowserNewPageOptions? options = default);

    /// <summary><para>Returns the browser version.</para></summary>
    string Version { get; }
}

#nullable disable
