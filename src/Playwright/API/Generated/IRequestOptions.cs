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
    /// <summary>
    /// <para>
    /// The <see cref="IRequestOptions"/> allows to create form data to be sent via <see
    /// cref="IAPIRequestContext"/>.
    /// </para>
    /// </summary>
    public partial interface IRequestOptions
    {
        /// <summary><para>Sets the request's post data.</para></summary>
        /// <param name="data">
        /// Allows to set post data of the request. If the data parameter is an object, it will
        /// be serialized to json string and <c>content-type</c> header will be set to <c>application/json</c>
        /// if not explicitly set. Otherwise the <c>content-type</c> header will be set to <c>application/octet-stream</c>
        /// if not explicitly set.
        /// </param>
        IRequestOptions SetData(string data);

        /// <summary><para>Sets the request's post data.</para></summary>
        /// <param name="data">
        /// Allows to set post data of the request. If the data parameter is an object, it will
        /// be serialized to json string and <c>content-type</c> header will be set to <c>application/json</c>
        /// if not explicitly set. Otherwise the <c>content-type</c> header will be set to <c>application/octet-stream</c>
        /// if not explicitly set.
        /// </param>
        IRequestOptions SetData(byte[] data);

        /// <summary><para>Sets the request's post data.</para></summary>
        /// <param name="data">
        /// Allows to set post data of the request. If the data parameter is an object, it will
        /// be serialized to json string and <c>content-type</c> header will be set to <c>application/json</c>
        /// if not explicitly set. Otherwise the <c>content-type</c> header will be set to <c>application/octet-stream</c>
        /// if not explicitly set.
        /// </param>
        IRequestOptions SetData(object data);

        /// <param name="failOnStatusCode">
        /// Whether to throw on response codes other than 2xx and 3xx. By default response object
        /// is returned for all status codes.
        /// </param>
        IRequestOptions SetFailOnStatusCode(bool failOnStatusCode);

        /// <summary>
        /// <para>
        /// Provides <see cref="IFormData"/> object that will be serialized as html form using
        /// <c>application/x-www-form-urlencoded</c> encoding and sent as this request body.
        /// If this parameter is specified <c>content-type</c> header will be set to <c>application/x-www-form-urlencoded</c>
        /// unless explicitly provided.
        /// </para>
        /// </summary>
        /// <param name="form">
        /// Form data to be serialized as html form using <c>application/x-www-form-urlencoded</c>
        /// encoding and sent as this request body.
        /// </param>
        IRequestOptions SetForm(IFormData form);

        /// <summary><para>Sets an HTTP header to the request.</para></summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        IRequestOptions SetHeader(string name, string value);

        /// <param name="ignoreHTTPSErrors">Whether to ignore HTTPS errors when sending network requests.</param>
        IRequestOptions SetIgnoreHTTPSErrors(bool ignoreHTTPSErrors);

        /// <summary>
        /// <para>
        /// Changes the request method (e.g. <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/PUT">PUT</a>
        /// or <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/POST">POST</a>).
        /// </para>
        /// </summary>
        /// <param name="method">Request method, e.g. <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods/POST">POST</a>.</param>
        IRequestOptions SetMethod(string method);

        /// <summary>
        /// <para>
        /// Provides <see cref="IFormData"/> object that will be serialized as html form using
        /// <c>multipart/form-data</c> encoding and sent as this request body. If this parameter
        /// is specified <c>content-type</c> header will be set to <c>multipart/form-data</c>
        /// unless explicitly provided.
        /// </para>
        /// </summary>
        /// <param name="form">
        /// Form data to be serialized as html form using <c>multipart/form-data</c> encoding
        /// and sent as this request body.
        /// </param>
        IRequestOptions SetMultipart(IFormData form);

        /// <summary><para>Adds a query parameter to the request URL.</para></summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        IRequestOptions SetQueryParam(string name, string value);

        /// <summary><para>Adds a query parameter to the request URL.</para></summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        IRequestOptions SetQueryParam(string name, bool value);

        /// <summary><para>Adds a query parameter to the request URL.</para></summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        IRequestOptions SetQueryParam(string name, int value);

        /// <summary>
        /// <para>
        /// Sets request timeout in milliseconds. Defaults to <c>30000</c> (30 seconds). Pass
        /// <c>0</c> to disable timeout.
        /// </para>
        /// </summary>
        /// <param name="timeout">Request timeout in milliseconds.</param>
        IRequestOptions SetTimeout(float timeout);
    }
}

#nullable disable
