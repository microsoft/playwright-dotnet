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

public class BrowserTypeConnectOptions
{
    public BrowserTypeConnectOptions() { }

    public BrowserTypeConnectOptions(BrowserTypeConnectOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        ExposeNetwork = clone.ExposeNetwork;
        Headers = clone.Headers;
        SlowMo = clone.SlowMo;
        Timeout = clone.Timeout;
    }

    /// <summary>
    /// <para>
    /// This option exposes network available on the connecting client to the browser being
    /// connected to. Consists of a list of rules separated by comma.
    /// </para>
    /// <para>Available rules:</para>
    /// <list type="ordinal">
    /// <item><description>
    /// Hostname pattern, for example: <c>example.com</c>, <c>*.org:99</c>, <c>x.*.y.com</c>,
    /// <c>*foo.org</c>.
    /// </description></item>
    /// <item><description>IP literal, for example: <c>127.0.0.1</c>, <c>0.0.0.0:99</c>, <c>[::1]</c>, <c>[0:0::1]:99</c>.</description></item>
    /// <item><description>
    /// <c>&lt;loopback&gt;</c> that matches local loopback interfaces: <c>localhost</c>,
    /// <c>*.localhost</c>, <c>127.0.0.1</c>, <c>[::1]</c>.
    /// </description></item>
    /// </list>
    /// <para>Some common examples:</para>
    /// <list type="ordinal">
    /// <item><description><c>"*"</c> to expose all network.</description></item>
    /// <item><description><c>"&lt;loopback&gt;"</c> to expose localhost network.</description></item>
    /// <item><description>
    /// <c>"*.test.internal-domain,*.staging.internal-domain,&lt;loopback&gt;"</c> to expose
    /// test/staging deployments and localhost.
    /// </description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("exposeNetwork")]
    public string? ExposeNetwork { get; set; }

    /// <summary><para>Additional HTTP headers to be sent with web socket connect request. Optional.</para></summary>
    [JsonPropertyName("headers")]
    public IEnumerable<KeyValuePair<string, string>>? Headers { get; set; }

    /// <summary>
    /// <para>
    /// Slows down Playwright operations by the specified amount of milliseconds. Useful
    /// so that you can see what is going on. Defaults to 0.
    /// </para>
    /// </summary>
    [JsonPropertyName("slowMo")]
    public float? SlowMo { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time in milliseconds to wait for the connection to be established. Defaults
    /// to <c>0</c> (no timeout).
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }
}

#nullable disable
