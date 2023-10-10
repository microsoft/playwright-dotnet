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
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class ConsoleMessage : IConsoleMessage
{
    private readonly BrowserContextConsoleEvent _event;

    internal ConsoleMessage(BrowserContextConsoleEvent @event)
    {
        _event = @event;
    }

    public string Type => _event.Type;

    public IReadOnlyList<IJSHandle> Args => _event.Args.Cast<IJSHandle>().ToList().AsReadOnly();

    public string Location => _event.Location.ToString();

    public string Text => _event.Text;

    // Note: currently, we only report console messages for pages and they always have a page.
    // However, in the future we might report console messages for service workers or something else,
    // where page() would be null.
    public IPage Page => _event.Page;
}

internal class BrowserContextConsoleEvent
{
    [JsonPropertyName("page")]
    public Page Page { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("args")]
    public List<JSHandle> Args { get; set; }

    [JsonPropertyName("location")]
    public ConsoleMessageLocation Location { get; set; }
}
