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

internal class AXNode
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("valueString")]
    public string ValueString { get; set; }

    [JsonPropertyName("valueNumber")]
    public int? ValueNumber { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("keyshortcuts")]
    public string Keyshortcuts { get; set; }

    [JsonPropertyName("roledescription")]
    public string Roledescription { get; set; }

    [JsonPropertyName("valuetext")]
    public string Valuetext { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("expanded")]
    public bool Expanded { get; set; }

    [JsonPropertyName("focused")]
    public bool Focused { get; set; }

    [JsonPropertyName("modal")]
    public bool Modal { get; set; }

    [JsonPropertyName("multiline")]
    public bool Multiline { get; set; }

    [JsonPropertyName("multiselectable")]
    public bool Multiselectable { get; set; }

    [JsonPropertyName("readonly")]
    public bool Readonly { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }

    [JsonPropertyName("checked")]
    public string Checked { get; set; }

    [JsonPropertyName("pressed")]
    public string Pressed { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("valuemin")]
    public int? Valuemin { get; set; }

    [JsonPropertyName("valuemax")]
    public int? Valuemax { get; set; }

    [JsonPropertyName("autocomplete")]
    public string Autocomplete { get; set; }

    [JsonPropertyName("haspopup")]
    public string Haspopup { get; set; }

    [JsonPropertyName("invalid")]
    public string Invalid { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; }

    [JsonPropertyName("children")]
    public List<AXNode> Children { get; set; }
}
