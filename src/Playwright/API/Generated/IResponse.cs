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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright
{
    /// <summary><para><see cref="IResponse"/> class represents responses which are received by page.</para></summary>
    public partial interface IResponse
    {
        /// <summary><para>Returns the buffer with response body.</para></summary>
        Task<byte[]> BodyAsync();

        /// <summary><para>Waits for this response to finish, returns failure error if request failed.</para></summary>
        Task<string?> FinishedAsync();

        /// <summary><para>Returns the <see cref="IFrame"/> that initiated this response.</para></summary>
        IFrame Frame { get; }

        /// <summary>
        /// <para>
        /// Returns the object with HTTP headers associated with the response. All header names
        /// are lower-case.
        /// </para>
        /// </summary>
        Dictionary<string, string> Headers { get; }

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
}

#nullable disable
