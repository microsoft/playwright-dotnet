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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// <para>
    /// A Browser is created via <see cref="IBrowserType.LaunchAsync"/>. An example of using
    /// a <see cref="IBrowser"/> to create a <see cref="IPage"/>:
    /// </para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class Program<br/>
    /// {<br/>
    ///     public static async Task Main()<br/>
    ///     {<br/>
    ///         using var playwright = await Playwright.CreateAsync();<br/>
    ///         var firefox = playwright.Firefox;<br/>
    ///         var browser = await firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });<br/>
    ///         var page = await browser.NewPageAsync();<br/>
    ///         await page.GotoAsync("https://www.bing.com");<br/>
    ///         await browser.CloseAsync();<br/>
    ///     }<br/>
    /// }
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
        /// await page.GotoAsync("https://www.bing.com");
        /// </code>
        /// </summary>
        /// <param name="options">Call options</param>
        Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options = default);

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
        Task<IPage> NewPageAsync(BrowserNewPageOptions options = default);

        /// <summary><para>Returns the browser version.</para></summary>
        string Version { get; }
    }
}
