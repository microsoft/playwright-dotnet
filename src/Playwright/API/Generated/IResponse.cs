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

/// <summary><para><see cref="IResponse"/> class represents responses which are received by page.</para></summary>
public partial interface IResponse
{
    /// <summary><para>An object with all the response HTTP headers associated with this response.</para></summary>
    Task<Dictionary<string, string>> AllHeadersAsync();

    /// <summary><para>Returns the buffer with response body.</para></summary>
    Task<byte[]> BodyAsync();

    /// <summary><para>Waits for this response to finish, returns always <c>null</c>.</para></summary>
    Task<string?> FinishedAsync();

    /// <summary><para>Returns the <see cref="IFrame"/> that initiated this response.</para></summary>
    IFrame Frame { get; }

    /// <summary>
    /// <para>
    /// Indicates whether this Response was fulfilled by a Service Worker's Fetch Handler
    /// (i.e. via <a href="https://developer.mozilla.org/en-US/docs/Web/API/FetchEvent/respondWith">FetchEvent.respondWith</a>).
    /// </para>
    /// </summary>
    bool FromServiceWorker { get; }

    /// <summary>
    /// <para>
    /// An object with the response HTTP headers. The header names are lower-cased. Note
    /// that this method does not return security-related headers, including cookie-related
    /// ones. You can use <see cref="IResponse.AllHeadersAsync"/> for complete list of headers
    /// that include <c>cookie</c> information.
    /// </para>
    /// </summary>
    Dictionary<string, string> Headers { get; }

    /// <summary>
    /// <para>
    /// An array with all the request HTTP headers associated with this response. Unlike
    /// <see cref="IResponse.AllHeadersAsync"/>, header names are NOT lower-cased. Headers
    /// with multiple entries, such as <c>Set-Cookie</c>, appear in the array multiple times.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Header>> HeadersArrayAsync();

    /// <summary>
    /// <para>
    /// Returns the value of the header matching the name. The name is case insensitive.
    /// If multiple headers have the same name (except <c>set-cookie</c>), they are returned
    /// as a list separated by <c>, </c>. For <c>set-cookie</c>, the <c>\n</c> separator
    /// is used. If no headers are found, <c>null</c> is returned.
    /// </para>
    /// </summary>
    /// <param name="name">Name of the header.</param>
    Task<string?> HeaderValueAsync(string name);

    /// <summary>
    /// <para>
    /// Returns all values of the headers matching the name, for example <c>set-cookie</c>.
    /// The name is case insensitive.
    /// </para>
    /// </summary>
    /// <param name="name">Name of the header.</param>
    Task<IReadOnlyList<string>> HeaderValuesAsync(string name);

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

    /// <summary><para>Returns the matching <see cref="IRequest"/> object.</para></summary>
    IRequest Request { get; }

    /// <summary><para>Returns SSL and other security information.</para></summary>
    Task<ResponseSecurityDetailsResult?> SecurityDetailsAsync();

    /// <summary><para>Returns the IP address and port of the server.</para></summary>
    Task<ResponseServerAddrResult?> ServerAddrAsync();

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
