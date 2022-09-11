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
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

public class FormData : IFormData
{
    public FormData()
    {
        Values = new Dictionary<string, object>();
    }

    internal Dictionary<string, object> Values { get; }

    private IFormData SetImpl(string name, object value)
    {
        Values.Add(name, value);
        return this;
    }

    public IFormData Set(string name, string value) => SetImpl(name, value);

    public IFormData Set(string name, bool value) => SetImpl(name, value);

    public IFormData Set(string name, int value) => SetImpl(name, value);

    public IFormData Set(string name, FilePayload value) => SetImpl(name, value);

    internal IList<object> ToProtocol()
    {
        var output = new List<object>();
        foreach (var kvp in Values)
        {
            if (kvp.Value is FilePayload file)
            {
                output.Add(new Dictionary<string, object>()
                {
                    ["name"] = kvp.Key,
                    ["file"] = new Dictionary<string, string>()
                    {
                        ["name"] = file.Name,
                        ["buffer"] = Convert.ToBase64String(file.Buffer),
                        ["mimeType"] = file.MimeType,
                    },
                });
            }
            else
            {
                output.Add(new NameValue() { Name = kvp.Key, Value = kvp.Value.ToString() });
            }
        }
        return output;
    }
}
