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

public class LocatorHoverOptions
{
    public LocatorHoverOptions() { }

    public LocatorHoverOptions(LocatorHoverOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Force = clone.Force;
        Modifiers = clone.Modifiers;
        Position = clone.Position;
        Timeout = clone.Timeout;
        Trial = clone.Trial;
    }

    /// <summary>
    /// <para>
    /// Whether to bypass the <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks. Defaults to <c>false</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("force")]
    public bool? Force { get; set; }

    /// <summary>
    /// <para>
    /// Modifier keys to press. Ensures that only these modifiers are pressed during the
    /// operation, and then restores current modifiers back. If not specified, currently
    /// pressed modifiers are used.
    /// </para>
    /// </summary>
    [JsonPropertyName("modifiers")]
    public IEnumerable<KeyboardModifier>? Modifiers { get; set; }

    /// <summary>
    /// <para>
    /// A point to use relative to the top-left corner of element padding box. If not specified,
    /// uses some visible point of the element.
    /// </para>
    /// </summary>
    [JsonPropertyName("position")]
    public Position? Position { get; set; }

    /// <summary>
    /// <para>
    /// Maximum time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
    /// The default value can be changed by using the <see cref="IBrowserContext.SetDefaultTimeout"/>
    /// or <see cref="IPage.SetDefaultTimeout"/> methods.
    /// </para>
    /// </summary>
    [JsonPropertyName("timeout")]
    public float? Timeout { get; set; }

    /// <summary>
    /// <para>
    /// When set, this method only performs the <a href="https://playwright.dev/dotnet/docs/actionability">actionability</a>
    /// checks and skips the action. Defaults to <c>false</c>. Useful to wait until the
    /// element is ready for the action without performing it.
    /// </para>
    /// </summary>
    [JsonPropertyName("trial")]
    public bool? Trial { get; set; }
}

#nullable disable
