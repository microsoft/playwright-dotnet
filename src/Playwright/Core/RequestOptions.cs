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

namespace Microsoft.Playwright.Core
{
    public class RequestOptions : IRequestOptions
    {
        internal IDictionary<string, object> Parameters { get; set; }

        internal string Method { get; private set; }

        internal IDictionary<string, string> Headers { get; set; }

        internal object Data { get; set; }

        internal FormData Form { get; private set; }

        internal FormData MultiPart { get; private set; }

        internal bool? FailOnStatusCode { get; private set; }

        internal bool? IgnoreHTTPSErrors { get; private set; }

        internal float? Timeout { get; private set; }

        public IRequestOptions SetData(string data) => SetDataImpl(data);

        public IRequestOptions SetData(byte[] data) => SetDataImpl(data);

        public IRequestOptions SetData(object data) => SetDataImpl(data);

        private IRequestOptions SetDataImpl(object data)
        {
            Data = data;
            return this;
        }

        public IRequestOptions SetFailOnStatusCode(bool failOnStatusCode)
        {
            FailOnStatusCode = failOnStatusCode;
            return this;
        }

        public IRequestOptions SetForm(IFormData form)
        {
            Form = (FormData)form;
            return this;
        }

        public IRequestOptions SetHeader(string name, string value)
        {
            if (Headers == null)
            {
                Headers = new Dictionary<string, string>();
            }
            Headers[name] = value;
            return this;
        }

        public IRequestOptions SetIgnoreHTTPSErrors(bool ignoreHTTPSErrors)
        {
            IgnoreHTTPSErrors = ignoreHTTPSErrors;
            return this;
        }

        public IRequestOptions SetMethod(string method)
        {
            Method = method;
            return this;
        }

        public IRequestOptions SetMultipart(IFormData form)
        {
            MultiPart = (FormData)form;
            return this;
        }

        public IRequestOptions SetQueryParam(string name, string value) => SetQueryParamImpl(name, value);

        public IRequestOptions SetQueryParam(string name, bool value) => SetQueryParamImpl(name, value);

        public IRequestOptions SetQueryParam(string name, int value) => SetQueryParamImpl(name, value);

        private IRequestOptions SetQueryParamImpl(string name, object value)
        {
            if (Parameters == null)
            {
                Parameters = new Dictionary<string, object>();
            }
            Parameters[name] = value;
            return this;
        }

        public IRequestOptions SetTimeout(float timeout)
        {
            Timeout = timeout;
            return this;
        }
    }
}
