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

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public partial class Proxy
{
    /// <summary>
    /// <para>
    /// Proxy to be used for all requests. HTTP and SOCKS proxies are supported, for example
    /// <c>http://myproxy.com:3128</c> or <c>socks5://myproxy.com:3128</c>. Short form <c>myproxy.com:3128</c>
    /// is considered an HTTP proxy.
    /// </para>
    /// </summary>
    [Required]
    [JsonPropertyName("server")]
    public string Server { get; set; } = default!;

    /// <summary>
    /// <para>
    /// Optional comma-separated domains to bypass proxy, for example <c>".com, chromium.org,
    /// .domain.com"</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("bypass")]
    public string? Bypass { get; set; }

    /// <summary><para>Optional username to use if HTTP proxy requires authentication.</para></summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary><para>Optional password to use if HTTP proxy requires authentication.</para></summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

#nullable disable
