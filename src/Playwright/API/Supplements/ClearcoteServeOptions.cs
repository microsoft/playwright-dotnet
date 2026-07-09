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

namespace Microsoft.Playwright;

/// <summary>
/// Options for <see cref="ClearcoteBrowser.ServeAsync"/> — launch Clearcote with a raw CDP endpoint.
/// </summary>
public class ClearcoteServeOptions : ClearcoteLaunchOptions
{
    /// <summary><para>CDP port (default: a free ephemeral port).</para></summary>
    public int? Port { get; set; }

    /// <summary><para>Bind address — keep it loopback for stealth (default: <c>127.0.0.1</c>).</para></summary>
    public string? Host { get; set; }

    /// <summary><para><c>--remote-allow-origins</c> value (default: the loopback origins only).</para></summary>
    public string? AllowOrigins { get; set; }

    /// <summary><para>Persistent profile directory (default: a fresh temp dir, removed on close).</para></summary>
    public string? UserDataDir { get; set; }

    /// <summary><para>How long to wait for the CDP endpoint to come up, in ms (default: 30000).</para></summary>
    public int? ReadyTimeoutMs { get; set; }
}
