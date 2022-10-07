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

using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

#nullable enable

namespace Microsoft.Playwright;

public class FrameRunAndWaitForNavigationOptions
{
    public FrameRunAndWaitForNavigationOptions() { }

    public FrameRunAndWaitForNavigationOptions(FrameRunAndWaitForNavigationOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Timeout = clone.Timeout;
        UrlString = clone.UrlString;
        UrlRegex = clone.UrlRegex;
        UrlFunc = clone.UrlFunc;
        WaitUntil = clone.WaitUntil;
    }

    /// <summary>
    /// <para>
    /// Maximum operation time in milliseconds, defaults to 30 seconds, pass <c>0</c> to
    /// disable timeout. The default value can be changed by using the <see cref="IBrowserContext.SetDefaultNavigationTimeout"/>,
    /// <see cref="IBrowserContext.SetDefaultTimeout"/>, <see cref="IPage.SetDefaultNavigationTimeout"/>
    /// or <see cref="IPage.SetDefaultTimeout"/> methods.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary>
    /// <para>
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while waiting for the navigation. Note that if the parameter is a string without
    /// wildcard characters, the method will wait for navigation to URL that is exactly
    /// equal to the string.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlString")]
    public string? UrlString { get; set; }

    /// <summary>
    /// <para>
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while waiting for the navigation. Note that if the parameter is a string without
    /// wildcard characters, the method will wait for navigation to URL that is exactly
    /// equal to the string.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlRegex")]
    public Regex? UrlRegex { get; set; }

    /// <summary>
    /// <para>
    /// A glob pattern, regex pattern or predicate receiving <see cref="URL"/> to match
    /// while waiting for the navigation. Note that if the parameter is a string without
    /// wildcard characters, the method will wait for navigation to URL that is exactly
    /// equal to the string.
    /// </para>
    /// </summary>
    [JsonPropertyName("urlFunc")]
    public Func<string, bool>? UrlFunc { get; set; }

    /// <summary>
    /// <para>When to consider operation succeeded, defaults to <c>load</c>. Events can be either:</para>
    /// <list type="bullet">
    /// <item><description>
    /// <c>'domcontentloaded'</c> - consider operation to be finished when the <c>DOMContentLoaded</c>
    /// event is fired.
    /// </description></item>
    /// <item><description>
    /// <c>'load'</c> - consider operation to be finished when the <c>load</c> event is
    /// fired.
    /// </description></item>
    /// <item><description>
    /// <c>'networkidle'</c> - consider operation to be finished when there are no network
    /// connections for at least <c>500</c> ms.
    /// </description></item>
    /// <item><description>
    /// <c>'commit'</c> - consider operation to be finished when network response is received
    /// and the document started loading.
    /// </description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("waitUntil")]
    public WaitUntilState? WaitUntil { get; set; }
}

#nullable disable
