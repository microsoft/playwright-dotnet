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
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public class BrowserTypeLaunchOptions
{
    public BrowserTypeLaunchOptions() { }

    public BrowserTypeLaunchOptions(BrowserTypeLaunchOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Args = clone.Args;
        Channel = clone.Channel;
        ChromiumSandbox = clone.ChromiumSandbox;
        Devtools = clone.Devtools;
        DownloadsPath = clone.DownloadsPath;
        Env = clone.Env;
        ExecutablePath = clone.ExecutablePath;
        FirefoxUserPrefs = clone.FirefoxUserPrefs;
        HandleSIGHUP = clone.HandleSIGHUP;
        HandleSIGINT = clone.HandleSIGINT;
        HandleSIGTERM = clone.HandleSIGTERM;
        Headless = clone.Headless;
        IgnoreAllDefaultArgs = clone.IgnoreAllDefaultArgs;
        IgnoreDefaultArgs = clone.IgnoreDefaultArgs;
        Proxy = clone.Proxy;
        SlowMo = clone.SlowMo;
        Timeout = clone.Timeout;
        TracesDir = clone.TracesDir;
    }

    /// <summary>
    /// <para>
    /// Additional arguments to pass to the browser instance. The list of Chromium flags
    /// can be found <a href="http://peter.sh/experiments/chromium-command-line-switches/">here</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("args")]
    public IEnumerable<string>? Args { get; set; }

    /// <summary>
    /// <para>
    /// Browser distribution channel.  Supported values are "chrome", "chrome-beta", "chrome-dev",
    /// "chrome-canary", "msedge", "msedge-beta", "msedge-dev", "msedge-canary". Read more
    /// about using <a href="https://playwright.dev/dotnet/docs/browsers#google-chrome--microsoft-edge">Google
    /// Chrome and Microsoft Edge</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary><para>Enable Chromium sandboxing. Defaults to <c>false</c>.</para></summary>
    [JsonPropertyName("chromiumSandbox")]
    public bool? ChromiumSandbox { get; set; }

    /// <summary>
    /// <para>
    /// **Chromium-only** Whether to auto-open a Developer Tools panel for each tab. If
    /// this option is <c>true</c>, the <paramref name="headless"/> option will be set <c>false</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("devtools")]
    public bool? Devtools { get; set; }

    /// <summary>
    /// <para>
    /// If specified, accepted downloads are downloaded into this directory. Otherwise,
    /// temporary directory is created and is deleted when browser is closed. In either
    /// case, the downloads are deleted when the browser context they were created in is
    /// closed.
    /// </para>
    /// </summary>
    [JsonPropertyName("downloadsPath")]
    public string? DownloadsPath { get; set; }

    /// <summary><para>Specify environment variables that will be visible to the browser. Defaults to <c>process.env</c>.</para></summary>
    [JsonPropertyName("env")]
    public IEnumerable<KeyValuePair<string, string>>? Env { get; set; }

    /// <summary>
    /// <para>
    /// Path to a browser executable to run instead of the bundled one. If <paramref name="executablePath"/>
    /// is a relative path, then it is resolved relative to the current working directory.
    /// Note that Playwright only works with the bundled Chromium, Firefox or WebKit, use
    /// at your own risk.
    /// </para>
    /// </summary>
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    /// <summary><para>Firefox user preferences. Learn more about the Firefox user preferences at <a href="https://support.mozilla.org/en-US/kb/about-config-editor-firefox"><c>about:config</c></a>.</para></summary>
    [JsonPropertyName("firefoxUserPrefs")]
    public IEnumerable<KeyValuePair<string, object>>? FirefoxUserPrefs { get; set; }

    /// <summary><para>Close the browser process on SIGHUP. Defaults to <c>true</c>.</para></summary>
    [JsonPropertyName("handleSIGHUP")]
    public bool? HandleSIGHUP { get; set; }

    /// <summary><para>Close the browser process on Ctrl-C. Defaults to <c>true</c>.</para></summary>
    [JsonPropertyName("handleSIGINT")]
    public bool? HandleSIGINT { get; set; }

    /// <summary><para>Close the browser process on SIGTERM. Defaults to <c>true</c>.</para></summary>
    [JsonPropertyName("handleSIGTERM")]
    public bool? HandleSIGTERM { get; set; }

    /// <summary>
    /// <para>
    /// Whether to run browser in headless mode. More details for <a href="https://developers.google.com/web/updates/2017/04/headless-chrome">Chromium</a>
    /// and <a href="https://developer.mozilla.org/en-US/docs/Mozilla/Firefox/Headless_mode">Firefox</a>.
    /// Defaults to <c>true</c> unless the <paramref name="devtools"/> option is <c>true</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("headless")]
    public bool? Headless { get; set; }

    /// <summary>
    /// <para>
    /// If <c>true</c>, Playwright does not pass its own configurations args and only uses
    /// the ones from <paramref name="args"/>. Dangerous option; use with care. Defaults
    /// to <c>false</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("ignoreAllDefaultArgs")]
    public bool? IgnoreAllDefaultArgs { get; set; }

    /// <summary>
    /// <para>
    /// If <c>true</c>, Playwright does not pass its own configurations args and only uses
    /// the ones from <paramref name="args"/>. Dangerous option; use with care.
    /// </para>
    /// </summary>
    [JsonPropertyName("ignoreDefaultArgs")]
    public IEnumerable<string>? IgnoreDefaultArgs { get; set; }

    /// <summary><para>Network proxy settings.</para></summary>
    [JsonPropertyName("proxy")]
    public Proxy? Proxy { get; set; }

    /// <summary>
    /// <para>
    /// Slows down Playwright operations by the specified amount of milliseconds. Useful
    /// so that you can see what is going on.
    /// </para>
    /// </summary>
    [JsonPropertyName("slowMo")]
    public float? SlowMo { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time in milliseconds to wait for the browser instance to start. Defaults
    /// to <c>30000</c> (30 seconds). Pass <c>0</c> to disable timeout.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary><para>If specified, traces are saved into this directory.</para></summary>
    [JsonPropertyName("tracesDir")]
    public string? TracesDir { get; set; }
}

#nullable disable
