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

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Playwright;

public partial class ElementHandleBoundingBoxResult
{
    /// <summary><para>the x coordinate of the element in pixels.</para></summary>
    [Required]
    [JsonPropertyName("x")]
    public float X { get; set; } = default!;

    /// <summary><para>the y coordinate of the element in pixels.</para></summary>
    [Required]
    [JsonPropertyName("y")]
    public float Y { get; set; } = default!;

    /// <summary><para>the width of the element in pixels.</para></summary>
    [Required]
    [JsonPropertyName("width")]
    public float Width { get; set; } = default!;

    /// <summary><para>the height of the element in pixels.</para></summary>
    [Required]
    [JsonPropertyName("height")]
    public float Height { get; set; } = default!;
}

#nullable disable
