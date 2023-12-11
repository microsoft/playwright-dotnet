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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Response : ChannelOwnerBase, IResponse
{
    private readonly ResponseInitializer _initializer;
    private readonly TaskCompletionSource<string> _finishedTask;
    private readonly RawHeaders _headers;
    private Task<RawHeaders> _rawHeadersTask;

    internal Response(ChannelOwnerBase parent, string guid, ResponseInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
        _initializer.Request.Timing = _initializer.Timing;
        _finishedTask = new();

        _headers = new RawHeaders(_initializer.Headers.ConvertAll(x => new NameValue() { Name = x.Name, Value = x.Value }).ToList());
    }

    public IFrame Frame => _initializer.Request.Frame;

    public Dictionary<string, string> Headers => _headers.Headers;

    public bool Ok => Status is 0 or >= 200 and <= 299;

    public IRequest Request => _initializer.Request;

    public int Status => _initializer.Status;

    public string StatusText => _initializer.StatusText;

    public string Url => _initializer.Url;

    public bool FromServiceWorker => _initializer.FromServiceWorker;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<Dictionary<string, string>> AllHeadersAsync()
        => (await GetRawHeadersAsync().ConfigureAwait(false)).Headers;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<byte[]> BodyAsync() => (await SendMessageToServerAsync("body").ConfigureAwait(false))?.GetProperty("binary").GetBytesFromBase64();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> FinishedAsync()
    {
        var targetClosedTask = _initializer.Request.TargetClosedAsync();
        if (targetClosedTask == await Task.WhenAny(_finishedTask.Task, targetClosedTask).ConfigureAwait(false))
        {
            throw new PlaywrightException("Target page, context or browser has been closed");
        }
        return await _finishedTask.Task.ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IReadOnlyList<Header>> HeadersArrayAsync()
        => (await GetRawHeadersAsync().ConfigureAwait(false)).HeadersArray;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> HeaderValueAsync(string name)
        => (await GetRawHeadersAsync().ConfigureAwait(false)).Get(name);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IReadOnlyList<string>> HeaderValuesAsync(string name)
        => (await GetRawHeadersAsync().ConfigureAwait(false)).GetAll(name);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<JsonElement?> JsonAsync()
    {
        byte[] content = await BodyAsync().ConfigureAwait(false);
        return JsonDocument.Parse(content).RootElement;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> JsonAsync<T>()
        => JsonSerializer.Deserialize<T>(await BodyAsync().ConfigureAwait(false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ResponseSecurityDetailsResult> SecurityDetailsAsync() => (await SendMessageToServerAsync("securityDetails").ConfigureAwait(false))
            ?.GetProperty("value").ToObject<ResponseSecurityDetailsResult>(_connection.DefaultJsonSerializerOptions);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<ResponseServerAddrResult> ServerAddrAsync() => (await SendMessageToServerAsync("serverAddr").ConfigureAwait(false))
            ?.GetProperty("value").ToObject<ResponseServerAddrResult>(_connection.DefaultJsonSerializerOptions);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> TextAsync()
    {
        byte[] content = await BodyAsync().ConfigureAwait(false);
        return Encoding.UTF8.GetString(content);
    }

    internal void ReportFinished(string erroMessage = null)
    {
        _finishedTask.SetResult(erroMessage);
    }

    private Task<RawHeaders> GetRawHeadersAsync()
    {
        if (_rawHeadersTask == null)
        {
            _rawHeadersTask = GetRawHeadersTaskAsync();
        }

        return _rawHeadersTask;
    }

    private async Task<RawHeaders> GetRawHeadersTaskAsync()
    {
        return new((await SendMessageToServerAsync("rawResponseHeaders").ConfigureAwait(false))?.GetProperty("headers").ToObject<List<NameValue>>());
    }
}
