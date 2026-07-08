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
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;

namespace Microsoft.Playwright;

/// <summary>
/// Canvas bridge settings for Clearcote launches.
/// </summary>
public class ClearcoteCanvasBridgeOptions
{
    /// <summary><para>Bridge endpoint, for example <c>ws://127.0.0.1:9099</c>.</para></summary>
    public string? Url { get; set; }

    /// <summary><para>HTTP Basic credentials expected by the bridge.</para></summary>
    public string? Auth { get; set; }

    /// <summary><para>Per-origin policy: <c>off</c>, <c>all</c>, <c>allow</c>, or <c>deny</c>.</para></summary>
    public string? Mode { get; set; }

    /// <summary><para>eTLD+1 values bridged when <see cref="Mode"/> is <c>allow</c>.</para></summary>
    public IEnumerable<string>? Allow { get; set; }

    /// <summary><para>eTLD+1 values skipped when <see cref="Mode"/> is <c>deny</c>.</para></summary>
    public IEnumerable<string>? Deny { get; set; }

    /// <summary><para>Cold cache miss behavior: <c>block</c> or <c>local</c>.</para></summary>
    public string? Fallback { get; set; }
}
