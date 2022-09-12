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

internal class SerializedValue
{
    [JsonPropertyName("n")]
    public int? N { get; set; }

    [JsonPropertyName("b")]
    public bool B { get; set; }

    [JsonPropertyName("s")]
    public string S { get; set; }

    [JsonPropertyName("v")]
    public string V { get; set; }

    [JsonPropertyName("d")]
    public string D { get; set; }

    [JsonPropertyName("u")]
    public string U { get; set; }

    [JsonPropertyName("r")]
    public SerializedValueR R { get; set; }

    [JsonPropertyName("a")]
    public List<System.Text.Json.JsonElement> A { get; set; }

    [JsonPropertyName("o")]
    public List<SerializedValueO> O { get; set; }

    [JsonPropertyName("h")]
    public int? H { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("ref")]
    public int? Ref { get; set; }
}
