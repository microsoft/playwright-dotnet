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

public class SelectorsRegisterOptions
{
    public SelectorsRegisterOptions() { }

    public SelectorsRegisterOptions(SelectorsRegisterOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        ContentScript = clone.ContentScript;
        Path = clone.Path;
        Script = clone.Script;
    }

    /// <summary>
    /// <para>
    /// Whether to run this selector engine in isolated JavaScript environment. This environment
    /// has access to the same DOM, but not any JavaScript objects from the frame's scripts.
    /// Defaults to <c>false</c>. Note that running as a content script is not guaranteed
    /// when this engine is used together with other registered engines.
    /// </para>
    /// </summary>
    [JsonPropertyName("contentScript")]
    public bool? ContentScript { get; set; }

    /// <summary>
    /// <para>
    /// Script that evaluates to a selector engine instance. The script is evaluated in
    /// the page context.
    /// </para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// <para>
    /// Script that evaluates to a selector engine instance. The script is evaluated in
    /// the page context.
    /// </para>
    /// </summary>
    [JsonPropertyName("script")]
    public string? Script { get; set; }
}

#nullable disable
