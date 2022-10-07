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
using System.Text.Json;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <see cref="IAPIResponse"/> class represents responses returned by <see cref="IAPIRequestContext.GetAsync"/>
/// and similar methods.
/// </para>
/// </summary>
public partial interface IAPIResponse
{
    /// <summary><para>Returns the buffer with response body.</para></summary>
    Task<byte[]> BodyAsync();

    /// <summary><para>An object with all the response HTTP headers associated with this response.</para></summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// <para>
    /// An array with all the request HTTP headers associated with this response. Header
    /// names are not lower-cased. Headers with multiple entries, such as <c>Set-Cookie</c>,
    /// appear in the array multiple times.
    /// </para>
    /// </summary>
    IReadOnlyList<Header> HeadersArray { get; }

    /// <summary>
    /// <para>Returns the JSON representation of response body.</para>
    /// <para>This method will throw if the response body is not parsable via <c>JSON.parse</c>.</para>
    /// </summary>
    Task<JsonElement?> JsonAsync();

    /// <summary>
    /// <para>
    /// Contains a boolean stating whether the response was successful (status in the range
    /// 200-299) or not.
    /// </para>
    /// </summary>
    bool Ok { get; }

    /// <summary><para>Contains the status code of the response (e.g., 200 for a success).</para></summary>
    int Status { get; }

    /// <summary><para>Contains the status text of the response (e.g. usually an "OK" for a success).</para></summary>
    string StatusText { get; }

    /// <summary><para>Returns the text representation of response body.</para></summary>
    Task<string> TextAsync();

    /// <summary><para>Contains the URL of the response.</para></summary>
    string Url { get; }
}

#nullable disable
