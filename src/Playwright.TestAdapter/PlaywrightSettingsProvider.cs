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
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Microsoft.Playwright.TestAdapter;

[ExtensionUri("settings://playwright")]
[SettingsName("Playwright")]
public class PlaywrightSettingsProvider : ISettingsProvider
{
    private static PlaywrightSettingsXml? _settings = null!;

    public static string BrowserName
    {
        get
        {
            var browserFromEnv = Environment.GetEnvironmentVariable("BROWSER")?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(browserFromEnv))
            {
                ValidateBrowserName(browserFromEnv!);
                return browserFromEnv!;
            }
            if (_settings != null && !string.IsNullOrEmpty(_settings.BrowserName))
            {
                var browser = _settings.BrowserName!.ToLowerInvariant();
                ValidateBrowserName(browser);
                return browser;
            }
            return BrowserType.Chromium;
        }
    }

    public static float? ExpectTimeout
    {
        get
        {
            if (_settings == null)
            {
                return null;
            }
            if (_settings.ExpectTimeout.HasValue)
            {
                return _settings.ExpectTimeout.Value;
            }
            return null;
        }
    }

    public static int Retries
    {
        get => _settings?.Retries ?? 0;
    }

    public static BrowserTypeLaunchOptions LaunchOptions
    {
        get
        {
            var launchOptions = _settings?.LaunchOptions ?? new BrowserTypeLaunchOptions();
            if (Environment.GetEnvironmentVariable("HEADED") == "1")
            {
                launchOptions.Headless = false;
            }
            else if (_settings != null && _settings.Headless.HasValue)
            {
                launchOptions.Headless = _settings.Headless.Value;
            }
            return launchOptions;
        }
    }

    private static void ValidateBrowserName(string browserName)
    {
        if (browserName != BrowserType.Chromium &&
            browserName != BrowserType.Firefox &&
            browserName != BrowserType.Webkit)
        {
            throw new ArgumentException($"Invalid browser name: {browserName}");
        }
    }

    public void Load(XmlReader reader)
        => _settings = new PlaywrightSettingsXml(reader);
}
