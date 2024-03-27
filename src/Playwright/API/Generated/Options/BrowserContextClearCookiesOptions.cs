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

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#nullable enable

namespace Microsoft.Playwright;

public class BrowserContextClearCookiesOptions
{
    public BrowserContextClearCookiesOptions() { }

    public BrowserContextClearCookiesOptions(BrowserContextClearCookiesOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Domain = clone.Domain;
        DomainRegex = clone.DomainRegex;
        DomainString = clone.DomainString;
        Name = clone.Name;
        NameRegex = clone.NameRegex;
        NameString = clone.NameString;
        Path = clone.Path;
        PathRegex = clone.PathRegex;
        PathString = clone.PathString;
    }

    /// <summary><para>Only removes cookies with the given domain.</para></summary>
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    /// <summary><para>Only removes cookies with the given domain.</para></summary>
    [JsonPropertyName("domainRegex")]
    public Regex? DomainRegex { get; set; }

    /// <summary><para>Only removes cookies with the given domain.</para></summary>
    [JsonPropertyName("domainString")]
    public string? DomainString { get; set; }

    /// <summary><para>Only removes cookies with the given name.</para></summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary><para>Only removes cookies with the given name.</para></summary>
    [JsonPropertyName("nameRegex")]
    public Regex? NameRegex { get; set; }

    /// <summary><para>Only removes cookies with the given name.</para></summary>
    [JsonPropertyName("nameString")]
    public string? NameString { get; set; }

    /// <summary><para>Only removes cookies with the given path.</para></summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary><para>Only removes cookies with the given path.</para></summary>
    [JsonPropertyName("pathRegex")]
    public Regex? PathRegex { get; set; }

    /// <summary><para>Only removes cookies with the given path.</para></summary>
    [JsonPropertyName("pathString")]
    public string? PathString { get; set; }
}

#nullable disable
