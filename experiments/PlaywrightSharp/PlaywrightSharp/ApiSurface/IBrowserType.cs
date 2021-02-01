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
 *
 *
 * ------------------------------------------------------------------------------ 
 * <auto-generated> 
 * This code was generated by a tool at:
 * /utils/doclint/generateDotnetApi.js
 * 
 * Changes to this file may cause incorrect behavior and will be lost if 
 * the code is regenerated. 
 * </auto-generated> 
 * ------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// BrowserType provides methods to launch a specific browser instance or connect to an existing one. The following is a typical
	/// example of using Playwright to drive automation:
	/// </summary>
	public interface IBrowserType
	{
		/// <summary>
		/// A path where Playwright expects to find a bundled browser executable.
		/// </summary>
		string GetExecutablePath();
		/// <summary>
		/// Returns the browser instance.
		/// You can use {OPTION} to filter out `--mute-audio` from default arguments:
		/// > **Chromium-only** Playwright can also be used to control the Chrome browser, but it works best with the version of Chromium
		/// it is bundled with. There is no guarantee it will work with any other version. Use {OPTION} option with extreme caution.
		/// >
		/// > If Google Chrome (rather than Chromium) is preferred, a <a href="https://www.google.com/chrome/browser/canary.html">Chrome Canary</a> or
		/// <a href="https://www.chromium.org/getting-involved/dev-channel">Dev Channel</a> build is suggested.
		/// >
		/// > In <see cref="IBrowserType.LaunchAsync"/> above, any mention of Chromium also applies to Chrome.
		/// >
		/// > See <a href="https://www.howtogeek.com/202825/what%E2%80%99s-the-difference-between-chromium-and-chrome/">`this article`</a> for
		/// a description of the differences between Chromium and Chrome. <a href="https://chromium.googlesource.com/chromium/src/+/lkgr/docs/chromium_browser_vs_google_chrome.md">`This article`</a> describes
		/// some differences for Linux users.
		/// </summary>
		Task<IBrowser> LaunchAsync(string[] args, bool chromiumSandbox, bool devtools, string downloadsPath, IEnumerable<KeyValuePair<string, string>> env, string executablePath, IEnumerable<KeyValuePair<string, string>> firefoxUserPrefs, bool handleSIGHUP, bool handleSIGINT, bool handleSIGTERM, bool headless, bool ignoreDefaultArgs, string[] ignoreDefaultArgsValues, BrowserTypeProxy proxy, float slowMo, float timeout);
		/// <summary>
		/// Returns the persistent browser context instance.
		/// Launches browser that uses persistent storage located at {PARAM} and returns the only context. Closing this context will
		/// automatically close the browser.
		/// </summary>
		Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, bool acceptDownloads, string[] args, bool bypassCSP, bool chromiumSandbox, ColorScheme colorScheme, float deviceScaleFactor, bool devtools, string downloadsPath, IEnumerable<KeyValuePair<string, string>> env, string executablePath, IEnumerable<KeyValuePair<string, string>> extraHTTPHeaders, BrowserTypeGeolocation geolocation, bool handleSIGHUP, bool handleSIGINT, bool handleSIGTERM, bool hasTouch, bool headless, BrowserTypeHttpCredentials httpCredentials, bool ignoreDefaultArgs, string[] ignoreDefaultArgsValues, bool ignoreHTTPSErrors, bool isMobile, bool javaScriptEnabled, string locale, bool offline, string[] permissions, BrowserTypeProxy proxy, float slowMo, float timeout, string timezoneId, string userAgent);
		/// <summary>
		/// Returns browser name. For example: `'chromium'`, `'webkit'` or `'firefox'`.
		/// </summary>
		string GetName();
	}
}