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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class RawHeaders
{
    private readonly Dictionary<string, List<string>> _headersMap = new();

    public RawHeaders(List<NameValue> headers)
    {
        HeadersArray = new(headers.Select(x => new Header() { Name = x.Name, Value = x.Value }));
        foreach (var entry in headers)
        {
            var name = entry.Name.ToLowerInvariant();
            if (!_headersMap.TryGetValue(name, out List<string> values))
            {
                values = new List<string>();
                _headersMap[name] = values;
            }

            values.Add(entry.Value);
        }
    }

    public List<Header> HeadersArray { get; }

    public Dictionary<string, string> Headers => _headersMap.Keys.ToDictionary(x => x, y => Get(y));

    public string Get(string name)
    {
        var values = GetAll(name);
        if (values == null)
        {
            return null;
        }

        return string.Join("set-cookie".Equals(name, StringComparison.OrdinalIgnoreCase) ? "\n" : ", ", values);
    }

    public string[] GetAll(string name)
    {
        if (_headersMap.TryGetValue(name.ToLowerInvariant(), out List<string> values))
        {
            return values.ToArray();
        }

        return null;
    }

    internal static RawHeaders FromHeadersObjectLossy(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var headersArray = headers.Select(x => new NameValue() { Name = x.Key, Value = x.Value }).Where(x => x.Value != null).ToList();
        return new RawHeaders(headersArray);
    }
}
