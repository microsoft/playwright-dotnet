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

#nullable enable

namespace Microsoft.Playwright;

public class LocatorAssertionsToHaveTextOptions
{
    public LocatorAssertionsToHaveTextOptions() { }

    public LocatorAssertionsToHaveTextOptions(LocatorAssertionsToHaveTextOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        IgnoreCase = clone.IgnoreCase;
        Timeout = clone.Timeout;
        UseInnerText = clone.UseInnerText;
    }

    /// <summary>
    /// <para>
    /// Whether to perform case-insensitive match. <paramref name="ignoreCase"/> option
    /// takes precedence over the corresponding regular expression flag if specified.
    /// </para>
    /// </summary>
    [JsonPropertyName("ignoreCase")]
    public bool? IgnoreCase { get; set; }

    /// <summary><para>Time to retry the assertion for.</para></summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary>
    /// <para>
    /// Whether to use <c>element.innerText</c> instead of <c>element.textContent</c> when
    /// retrieving DOM node text.
    /// </para>
    /// </summary>
    [JsonPropertyName("useInnerText")]
    public bool? UseInnerText { get; set; }
}

#nullable disable
