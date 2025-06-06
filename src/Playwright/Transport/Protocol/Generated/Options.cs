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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Protocol;

internal class Options
{
    [JsonPropertyName("noDefaultViewport")]
    public bool? NoDefaultViewport { get; set; }

    [JsonPropertyName("viewport")]
    public ViewportSize Viewport { get; set; } = null!;

    [JsonPropertyName("screen")]
    public ViewportSize Screen { get; set; } = null!;

    [JsonPropertyName("ignoreHTTPSErrors")]
    public bool? IgnoreHTTPSErrors { get; set; }

    [JsonPropertyName("clientCertificates")]
    public List<OptionsClientCertificates> ClientCertificates { get; set; } = null!;

    [JsonPropertyName("javaScriptEnabled")]
    public bool? JavaScriptEnabled { get; set; }

    [JsonPropertyName("bypassCSP")]
    public bool? BypassCSP { get; set; }

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = null!;

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = null!;

    [JsonPropertyName("timezoneId")]
    public string TimezoneId { get; set; } = null!;

    [JsonPropertyName("geolocation")]
    public OptionsGeolocation Geolocation { get; set; } = null!;

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = null!;

    [JsonPropertyName("extraHTTPHeaders")]
    public List<NameValue> ExtraHTTPHeaders { get; set; } = null!;

    [JsonPropertyName("offline")]
    public bool? Offline { get; set; }

    [JsonPropertyName("httpCredentials")]
    public OptionsHttpCredentials HttpCredentials { get; set; } = null!;

    [JsonPropertyName("deviceScaleFactor")]
    public int? DeviceScaleFactor { get; set; }

    [JsonPropertyName("isMobile")]
    public bool? IsMobile { get; set; }

    [JsonPropertyName("hasTouch")]
    public bool? HasTouch { get; set; }

    [JsonPropertyName("colorScheme")]
    public string ColorScheme { get; set; } = null!;

    [JsonPropertyName("reducedMotion")]
    public string ReducedMotion { get; set; } = null!;

    [JsonPropertyName("forcedColors")]
    public string ForcedColors { get; set; } = null!;

    [JsonPropertyName("acceptDownloads")]
    public string AcceptDownloads { get; set; } = null!;

    [JsonPropertyName("contrast")]
    public string Contrast { get; set; } = null!;

    [JsonPropertyName("baseURL")]
    public string BaseURL { get; set; } = null!;

    [JsonPropertyName("recordVideo")]
    public OptionsRecordVideo RecordVideo { get; set; } = null!;

    [JsonPropertyName("strictSelectors")]
    public bool? StrictSelectors { get; set; }

    [JsonPropertyName("serviceWorkers")]
    public string ServiceWorkers { get; set; } = null!;

    [JsonPropertyName("selectorEngines")]
    public List<SelectorEngine> SelectorEngines { get; set; } = null!;

    [JsonPropertyName("testIdAttributeName")]
    public string TestIdAttributeName { get; set; } = null!;
}
