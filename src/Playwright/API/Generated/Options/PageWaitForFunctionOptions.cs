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

namespace Microsoft.Playwright;

public class PageWaitForFunctionOptions
{
    public PageWaitForFunctionOptions() { }

    public PageWaitForFunctionOptions(PageWaitForFunctionOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        PollingInterval = clone.PollingInterval;
        Timeout = clone.Timeout;
    }

    /// <summary>
    /// <para>
    /// If specified, then it is treated as an interval in milliseconds at which the function
    /// would be executed. By default if the option is not specified <see cref="IPage.WaitForFunctionAsync"/>
    /// is executed in <c>requestAnimationFrame</c> callback.
    /// </para>
    /// </summary>
    [JsonPropertyName("pollingInterval")]
    public float? PollingInterval { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time to wait for in milliseconds. Defaults to <c>30000</c> (30 seconds).
    /// Pass <c>0</c> to disable timeout. The default value can be changed by using the
    /// <see cref="IBrowserContext.SetDefaultTimeout"/> or <see cref="IPage.SetDefaultTimeout"/>
    /// methods.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }
}
