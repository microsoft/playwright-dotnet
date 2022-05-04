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
    public class APIRequestContextHeadOptions
    {
        public APIRequestContextHeadOptions() { }

        public APIRequestContextHeadOptions(APIRequestContextHeadOptions clone)
        {
            if (clone == null)
            {
                return;
            }

            FailOnStatusCode = clone.FailOnStatusCode;
            Headers = clone.Headers;
            Timeout = clone.Timeout;
        }

        /// <summary>
        /// <para>
        /// Whether to throw on response codes other than 2xx and 3xx. By default response object
        /// is returned for all status codes.
        /// </para>
        /// </summary>
        [JsonPropertyName("failOnStatusCode")]
        public bool? FailOnStatusCode { get; set; }

        /// <summary><para>Allows to set HTTP headers.</para></summary>
        [JsonPropertyName("headers")]
        public IEnumerable<KeyValuePair<string, string>>? Headers { get; set; }

        /// <summary>
        /// <para>
        /// Request timeout in milliseconds. Defaults to <c>30000</c> (30 seconds). Pass <c>0</c>
        /// to disable timeout.
        /// </para>
        /// </summary>
        [JsonPropertyName("timeout")]
        public float? Timeout { get; set; }
    }
}

#nullable disable
