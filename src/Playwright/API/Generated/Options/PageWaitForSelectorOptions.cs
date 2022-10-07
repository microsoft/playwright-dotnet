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

public class PageWaitForSelectorOptions
{
    public PageWaitForSelectorOptions() { }

    public PageWaitForSelectorOptions(PageWaitForSelectorOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        State = clone.State;
        Strict = clone.Strict;
        Timeout = clone.Timeout;
    }

    /// <summary>
    /// <para>Defaults to <c>'visible'</c>. Can be either:</para>
    /// <list type="bullet">
    /// <item><description><c>'attached'</c> - wait for element to be present in DOM.</description></item>
    /// <item><description><c>'detached'</c> - wait for element to not be present in DOM.</description></item>
    /// <item><description>
    /// <c>'visible'</c> - wait for element to have non-empty bounding box and no <c>visibility:hidden</c>.
    /// Note that element without any content or with <c>display:none</c> has an empty bounding
    /// box and is not considered visible.
    /// </description></item>
    /// <item><description>
    /// <c>'hidden'</c> - wait for element to be either detached from DOM, or have an empty
    /// bounding box or <c>visibility:hidden</c>. This is opposite to the <c>'visible'</c>
    /// option.
    /// </description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("state")]
    public WaitForSelectorState? State { get; set; }

    /// <summary>
    /// <para>
    /// When true, the call requires selector to resolve to a single element. If given selector
    /// resolves to more than one element, the call throws an exception.
    /// </para>
    /// </summary>
    [JsonPropertyName("strict")]
    public bool? Strict { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
    /// The default value can be changed by using the <see cref="IBrowserContext.SetDefaultTimeout"/>
    /// or <see cref="IPage.SetDefaultTimeout"/> methods.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }
}

#nullable disable
