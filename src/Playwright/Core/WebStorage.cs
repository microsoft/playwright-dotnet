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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class WebStorage : IWebStorage
{
    private readonly Page _page;
    private readonly string _kind;

    public WebStorage(Page page, string kind)
    {
        _page = page;
        _kind = kind;
    }

    public async Task<IReadOnlyList<WebStorageItem>> ItemsAsync()
    {
        var result = await _page.SendMessageToServerAsync("webStorageItems", new Dictionary<string, object?>
        {
            ["kind"] = _kind,
        }).ConfigureAwait(false);
        return result!.Value.GetProperty("items").ToObject<List<WebStorageItem>>(_page._connection.DefaultJsonSerializerOptions);
    }

    public async Task<string?> GetItemAsync(string name)
    {
        var result = await _page.SendMessageToServerAsync("webStorageGetItem", new Dictionary<string, object?>
        {
            ["kind"] = _kind,
            ["name"] = name,
        }).ConfigureAwait(false);
        if (result?.TryGetProperty("value", out var value) == true && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    public Task SetItemAsync(string name, string value)
        => _page.SendMessageToServerAsync("webStorageSetItem", new Dictionary<string, object?>
        {
            ["kind"] = _kind,
            ["name"] = name,
            ["value"] = value,
        });

    public Task RemoveItemAsync(string name)
        => _page.SendMessageToServerAsync("webStorageRemoveItem", new Dictionary<string, object?>
        {
            ["kind"] = _kind,
            ["name"] = name,
        });

    public Task ClearAsync()
        => _page.SendMessageToServerAsync("webStorageClear", new Dictionary<string, object?>
        {
            ["kind"] = _kind,
        });
}
