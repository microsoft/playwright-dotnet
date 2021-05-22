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
    /// BrowserType provides methods to launch a specific browser instance or connect to
    /// an existing one. The following is a typical example of using Playwright to drive
    /// automation:
    /// </para>
    /// <code>
    /// using Microsoft.Playwright;<br/>
    /// using System.Threading.Tasks;<br/>
    /// <br/>
    /// class BrowserTypeExamples<br/>
    /// {<br/>
    ///     public static async Task Run()<br/>
    ///     {<br/>
    ///         using var playwright = await Playwright.CreateAsync();<br/>
    ///         var chromium = playwright.Chromium;<br/>
    ///         var browser = await chromium.LaunchAsync();<br/>
    ///         var page = await browser.NewPageAsync();<br/>
    ///         await page.GoToAsync("https://www.bing.com");<br/>
    ///         // other actions<br/>
    ///         await browser.CloseAsync();<br/>
    ///     }<br/>
    /// }
    /// </code>
    /// </summary>
    public partial interface IBrowserType
    {
        /// <summary><para>A path where Playwright expects to find a bundled browser executable.</para></summary>
        string ExecutablePath { get; }

        /// <summary>
        /// <para>Returns the browser instance.</para>
        /// <para>
        /// You can use <paramref name="ignoreDefaultArgs"/> to filter out <c>--mute-audio</c>
        /// from default arguments:
        /// </para>
        /// <code>
        /// var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {<br/>
        ///     IgnoreDefaultArgs = new[] { "--mute-audio" }<br/>
        /// })
        /// </code>
        /// <para>
        /// > **Chromium-only** Playwright can also be used to control the Google Chrome or
        /// Microsoft Edge browsers, but it works best with the version of Chromium it is bundled
        /// with. There is no guarantee it will work with any other version. Use <paramref name="executablePath"/>
        /// option with extreme caution.
        /// </para>
        /// <para>></para>
        /// <para>
        /// > If Google Chrome (rather than Chromium) is preferred, a <a href="https://www.google.com/chrome/browser/canary.html">Chrome
        /// Canary</a> or <a href="https://www.chromium.org/getting-involved/dev-channel">Dev
        /// Channel</a> build is suggested.
        /// </para>
        /// <para>></para>
        /// <para>
        /// > Stock browsers like Google Chrome and Microsoft Edge are suitable for tests that
        /// require proprietary media codecs for video playback. See <a href="https://www.howtogeek.com/202825/what%E2%80%99s-the-difference-between-chromium-and-chrome/">this
        /// article</a> for other differences between Chromium and Chrome. <a href="https://chromium.googlesource.com/chromium/src/+/lkgr/docs/chromium_browser_vs_google_chrome.md">This
        /// article</a> describes some differences for Linux users.
        /// </para>
        /// </summary>
        /// <param name="options">Call options</param>
        Task<IBrowser> LaunchAsync(BrowserTypeLaunchOptions options = default);

        /// <summary>
        /// <para>Returns the persistent browser context instance.</para>
        /// <para>
        /// Launches browser that uses persistent storage located at <paramref name="userDataDir"/>
        /// and returns the only context. Closing this context will automatically close the
        /// browser.
        /// </para>
        /// </summary>
        /// <param name="userDataDir">
        /// Path to a User Data Directory, which stores browser session data like cookies and
        /// local storage. More details for <a href="https://chromium.googlesource.com/chromium/src/+/master/docs/user_data_dir.md#introduction">Chromium</a>
        /// and <a href="https://developer.mozilla.org/en-US/docs/Mozilla/Command_Line_Options#User_Profile">Firefox</a>.
        /// Note that Chromium's user data directory is the **parent** directory of the "Profile
        /// Path" seen at <c>chrome://version</c>.
        /// </param>
        /// <param name="options">Call options</param>
        Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, BrowserTypeLaunchPersistentContextOptions options = default);

        /// <summary><para>Returns browser name. For example: <c>'chromium'</c>, <c>'webkit'</c> or <c>'firefox'</c>.</para></summary>
        string Name { get; }
    }
}
