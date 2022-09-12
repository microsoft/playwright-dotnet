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

internal class PlaywrightInitializer
{
    [JsonPropertyName("chromium")]
    public Core.BrowserType Chromium { get; set; }

    [JsonPropertyName("firefox")]
    public Core.BrowserType Firefox { get; set; }

    [JsonPropertyName("webkit")]
    public Core.BrowserType Webkit { get; set; }

    [JsonPropertyName("utils")]
    public Core.LocalUtils Utils { get; set; }

    [JsonPropertyName("deviceDescriptors")]
    public List<DeviceDescriptorEntry> DeviceDescriptors { get; set; }

    [JsonPropertyName("selectors")]
    public Core.Selectors Selectors { get; set; }

    [JsonPropertyName("preLaunchedBrowser")]
    public Core.Browser PreLaunchedBrowser { get; set; }

    [JsonPropertyName("socksSupport")]
    public Core.SocksSupport SocksSupport { get; set; }
}
