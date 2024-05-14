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

public class PageAddLocatorHandlerOptions
{
    public PageAddLocatorHandlerOptions() { }

    public PageAddLocatorHandlerOptions(PageAddLocatorHandlerOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        NoWaitAfter = clone.NoWaitAfter;
        Times = clone.Times;
    }

    /// <summary>
    /// <para>
    /// By default, after calling the handler Playwright will wait until the overlay becomes
    /// hidden, and only then Playwright will continue with the action/assertion that triggered
    /// the handler. This option allows to opt-out of this behavior, so that overlay can
    /// stay visible after the handler has run.
    /// </para>
    /// </summary>
    [JsonPropertyName("noWaitAfter")]
    public bool? NoWaitAfter { get; set; }

    /// <summary>
    /// <para>
    /// Specifies the maximum number of times this handler should be called. Unlimited by
    /// default.
    /// </para>
    /// </summary>
    [JsonPropertyName("times")]
    public int? Times { get; set; }
}

#nullable disable
