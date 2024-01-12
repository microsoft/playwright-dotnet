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
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

#nullable enable

namespace Microsoft.Playwright.Core;

internal class APIResponse : IAPIResponse
{
    internal APIRequestContext _context;

    private readonly Transport.Protocol.APIResponse _initializer;

    private readonly RawHeaders _headers;

    public APIResponse(APIRequestContext context, Transport.Protocol.APIResponse initializer)
    {
        _initializer = initializer;
        _context = context;
        _headers = new RawHeaders(initializer.Headers);
    }

    public Dictionary<string, string> Headers => _headers.Headers;

    public IReadOnlyList<Header> HeadersArray => _headers.HeadersArray;

    public bool Ok => _initializer.Status >= 200 && _initializer.Status <= 299;

    public int Status => _initializer.Status;

    public string StatusText => _initializer.StatusText;

    public string Url => _initializer.Url;

    public async Task<byte[]> BodyAsync()
    {
        try
        {
            var response = await _context.SendMessageToServerAsync("fetchResponseBody", new Dictionary<string, object> { ["fetchUid"] = FetchUid() }).ConfigureAwait(false);
            if (response?.TryGetProperty("binary", out var binary) == true)
            {
                return Convert.FromBase64String(binary.ToString());
            }
            throw new PlaywrightException("Response has been disposed");
        }
        catch (Exception e) when (DriverMessages.IsTargetClosedError(e))
        {
            throw new PlaywrightException("Response has been disposed");
        }
    }

    public async Task<JsonElement?> JsonAsync() => JsonSerializer.Deserialize<JsonElement>(await BodyAsync().ConfigureAwait(false));

    public async Task<T?> JsonAsync<T>(JsonSerializerOptions? options) => JsonSerializer.Deserialize<T>(await BodyAsync().ConfigureAwait(false), options);

    public async Task<string> TextAsync()
    {
        var buffer = await BodyAsync().ConfigureAwait(false);
        return System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
    }

    internal string FetchUid() => _initializer.FetchUid;

    internal async Task<string[]> FetchLogAsync()
    {
        var response = await _context.SendMessageToServerAsync("fetchLog", new Dictionary<string, object> { ["fetchUid"] = FetchUid() }).ConfigureAwait(false);
        return response.Value.GetProperty("log").ToObject<string[]>();
    }

    public ValueTask DisposeAsync() => new(_context.SendMessageToServerAsync("disposeAPIResponse", new Dictionary<string, object> { ["fetchUid"] = FetchUid() }));

    public override string ToString()
    {
        var headers = HeadersArray.Select(h => $"  {h.Name}: {h.Value}");
        return $"APIResponse: {Status} {StatusText}\n{string.Join("\n", headers)}";
    }
}
