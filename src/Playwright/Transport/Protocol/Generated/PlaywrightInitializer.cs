/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Protocol;

internal class PlaywrightInitializer
{
    [JsonPropertyName("chromium")]
    public Core.BrowserType Chromium { get; set; } = null!;

    [JsonPropertyName("firefox")]
    public Core.BrowserType Firefox { get; set; } = null!;

    [JsonPropertyName("webkit")]
    public Core.BrowserType Webkit { get; set; } = null!;

    [JsonPropertyName("bidiChromium")]
    public Core.BrowserType BidiChromium { get; set; } = null!;

    [JsonPropertyName("bidiFirefox")]
    public Core.BrowserType BidiFirefox { get; set; } = null!;

    [JsonPropertyName("utils")]
    public Core.LocalUtils Utils { get; set; } = null!;

    [JsonPropertyName("selectors")]
    public Core.Selectors Selectors { get; set; } = null!;

    [JsonPropertyName("preLaunchedBrowser")]
    public Core.Browser PreLaunchedBrowser { get; set; } = null!;

    [JsonPropertyName("preConnectedAndroidDevice")]
    public Core.AndroidDevice PreConnectedAndroidDevice { get; set; } = null!;

    [JsonPropertyName("socksSupport")]
    public Core.SocksSupport SocksSupport { get; set; } = null!;
}
