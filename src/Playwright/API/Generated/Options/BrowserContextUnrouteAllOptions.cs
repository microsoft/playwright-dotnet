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

public class BrowserContextUnrouteAllOptions
{
    public BrowserContextUnrouteAllOptions() { }

    public BrowserContextUnrouteAllOptions(BrowserContextUnrouteAllOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Behavior = clone.Behavior;
    }

    /// <summary>
    /// <para>
    /// Specifies whether to wait for already running handlers and what to do if they throw
    /// errors:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <c>'default'</c> - do not wait for current handler calls (if any) to finish, if
    /// unrouted handler throws, it may result in unhandled error
    /// </description></item>
    /// <item><description><c>'wait'</c> - wait for current handler calls (if any) to finish</description></item>
    /// <item><description>
    /// <c>'ignoreErrors'</c> - do not wait for current handler calls (if any) to finish,
    /// all errors thrown by the handlers after unrouting are silently caught
    /// </description></item>
    /// </list>
    /// </summary>
    [JsonPropertyName("behavior")]
    public UnrouteBehavior? Behavior { get; set; }
}

#nullable disable
