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
    internal partial class BrowserType
    {
        public Task<IBrowser> LaunchAsync(BrowserTypeLaunchOptions options = default)
        {
            options ??= new BrowserTypeLaunchOptions();
            return LaunchAsync(args: options.Args, channel: options.Channel, chromiumSandbox: options.ChromiumSandbox, devtools: options.Devtools, downloadsPath: options.DownloadsPath, env: options.Env, executablePath: options.ExecutablePath, handleSIGINT: options.HandleSIGINT, handleSIGTERM: options.HandleSIGTERM, handleSIGHUP: options.HandleSIGHUP, headless: options.Headless, proxy: options.Proxy, timeout: options.Timeout, tracesDir: options.TracesDir, firefoxUserPrefs: options.FirefoxUserPrefs, slowMo: options.SlowMo, ignoreDefaultArgs: options.IgnoreDefaultArgs, ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs);
        }

        public Task<IBrowserContext> LaunchPersistentContextAsync(string userDataDir, BrowserTypeLaunchPersistentContextOptions options = default)
        {
            options ??= new BrowserTypeLaunchPersistentContextOptions();
            return LaunchPersistentContextAsync(userDataDir, args: options.Args, channel: options.Channel, chromiumSandbox: options.ChromiumSandbox, devtools: options.Devtools, downloadsPath: options.DownloadsPath, env: options.Env, executablePath: options.ExecutablePath, handleSIGINT: options.HandleSIGINT, handleSIGTERM: options.HandleSIGTERM, handleSIGHUP: options.HandleSIGHUP, headless: options.Headless, proxy: options.Proxy, timeout: options.Timeout, tracesDir: options.TracesDir, slowMo: options.SlowMo, ignoreDefaultArgs: options.IgnoreDefaultArgs, ignoreAllDefaultArgs: options.IgnoreAllDefaultArgs, acceptDownloads: options.AcceptDownloads, ignoreHTTPSErrors: options.IgnoreHTTPSErrors, bypassCSP: options.BypassCSP, viewportSize: options.ViewportSize, screenSize: options.ScreenSize, userAgent: options.UserAgent, deviceScaleFactor: options.DeviceScaleFactor, isMobile: options.IsMobile, hasTouch: options.HasTouch, javaScriptEnabled: options.JavaScriptEnabled, timezoneId: options.TimezoneId, geolocation: options.Geolocation, locale: options.Locale, permissions: options.Permissions, extraHTTPHeaders: options.ExtraHTTPHeaders, offline: options.Offline, httpCredentials: options.HttpCredentials, colorScheme: options.ColorScheme, reducedMotion: options.ReducedMotion, recordHarPath: options.RecordHarPath, recordHarOmitContent: options.RecordHarOmitContent, recordVideoDir: options.RecordVideoDir, recordVideoSize: options.RecordVideoSize);
        }
    }
}
