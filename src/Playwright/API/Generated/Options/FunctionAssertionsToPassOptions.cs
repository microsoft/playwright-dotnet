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

namespace Microsoft.Playwright;

public class FunctionAssertionsToPassOptions
{
    public FunctionAssertionsToPassOptions() { }

    public FunctionAssertionsToPassOptions(FunctionAssertionsToPassOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Intervals = clone.Intervals;
        Timeout = clone.Timeout;
    }

    /// <summary>
    /// <para>
    /// Time to wait between the retries in milliseconds. The last interval is used for
    /// all the following retries. Defaults to <c>[100, 250, 500, 1000]</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("intervals")]
    public IEnumerable<int>? Intervals { get; set; }

    /// <summary>
    /// <para>
    /// Time to retry the callback for in milliseconds. Defaults to <c>30000</c>, change
    /// the default with <c>SetDefaultExpectToPassTimeout</c>. Pass <c>0</c> to disable
    /// the timeout.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }
}
