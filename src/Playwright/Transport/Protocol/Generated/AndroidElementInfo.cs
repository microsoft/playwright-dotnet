/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

namespace Microsoft.Playwright.Transport.Protocol;

internal class AndroidElementInfo
{
    [JsonPropertyName("children")]
    public List<AndroidElementInfo> Children { get; set; }

    [JsonPropertyName("clazz")]
    public string Clazz { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("res")]
    public string Res { get; set; }

    [JsonPropertyName("pkg")]
    public string Pkg { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("bounds")]
    public Rect Bounds { get; set; }

    [JsonPropertyName("checkable")]
    public bool Checkable { get; set; }

    [JsonPropertyName("checked")]
    public bool Checked { get; set; }

    [JsonPropertyName("clickable")]
    public bool Clickable { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("focusable")]
    public bool Focusable { get; set; }

    [JsonPropertyName("focused")]
    public bool Focused { get; set; }

    [JsonPropertyName("longClickable")]
    public bool LongClickable { get; set; }

    [JsonPropertyName("scrollable")]
    public bool Scrollable { get; set; }

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }
}
