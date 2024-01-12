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
using System.Web;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;

using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class Request : ChannelOwner, IRequest
{
    internal readonly RequestInitializer _initializer;
    private readonly RawHeaders _provisionalHeaders;
    private readonly RouteFallbackOptions _fallbackOverrides = new();
    private Task<RawHeaders> _rawHeadersTask;

    internal Request(ChannelOwner parent, string guid, RequestInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
        RedirectedFrom = _initializer.RedirectedFrom;
        Timing = new();

        if (RedirectedFrom != null)
        {
            _initializer.RedirectedFrom.RedirectedTo = this;
        }

        _provisionalHeaders = new RawHeaders(initializer.Headers.ConvertAll(x => new NameValue() { Name = x.Name, Value = x.Value }).ToList());
        _fallbackOverrides.PostData = initializer.PostData;
    }

    public string Failure { get; internal set; }

    public IFrame Frame
    {
        get
        {
            if (_initializer.Frame == null && ServiceWorker != null)
            {
                throw new PlaywrightException("Service Worker requests do not have an associated frame.");
            }
            var frame = _initializer.Frame;
            if (frame.Page == null)
            {
                throw new PlaywrightException(string.Join("\n", new string[]
                {
                    "Frame for this navigation request is not available, because the request",
                    "was issued before the frame is created. You can check whether the request",
                    "is a navigation request by calling isNavigationRequest() method.",
                }));
            }
            return frame;
        }
    }

    public Dictionary<string, string> Headers
    {
        get
        {
            if (_fallbackOverrides.Headers != null)
            {
                return RawHeaders.FromHeadersObjectLossy(_fallbackOverrides.Headers).Headers;
            }
            return _provisionalHeaders.Headers;
        }
    }

    internal Page SafePage
    {
        get
        {
            return (_initializer.Frame?.Page as Page) ?? null;
        }
    }

    public bool IsNavigationRequest => _initializer.IsNavigationRequest;

    public string Method => !string.IsNullOrEmpty(_fallbackOverrides.Method) ? _fallbackOverrides.Method : _initializer.Method;

    public string PostData
    {
        get
        {
            if (_fallbackOverrides.PostData != null)
            {
                return Encoding.UTF8.GetString(_fallbackOverrides.PostData);
            }
            return null;
        }
    }

    public byte[] PostDataBuffer
    {
        get
        {
            return _fallbackOverrides.PostData;
        }
    }

    public IRequest RedirectedFrom { get; }

    public IRequest RedirectedTo { get; internal set; }

    public string ResourceType => _initializer.ResourceType;

    public RequestTimingResult Timing { get; internal set; }

    public string Url => !string.IsNullOrEmpty(_fallbackOverrides.Url) ? _fallbackOverrides.Url : _initializer.Url;

    internal Request FinalRequest => RedirectedTo != null ? ((Request)RedirectedTo).FinalRequest : this;

    public RequestSizesResult Sizes { get; internal set; }

    public IWorker ServiceWorker => _initializer.ServiceWorker;

    internal BrowserContext _context => (BrowserContext)Frame.Page.Context;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IResponse> ResponseAsync() => await SendMessageToServerAsync<Response>("response").ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public JsonElement? PostDataJSON()
    {
        if (PostData == null)
        {
            return null;
        }

        string content = PostData;
        Headers.TryGetValue("content-type", out string contentType);
        if (contentType == "application/x-www-form-urlencoded")
        {
            var parsed = HttpUtility.ParseQueryString(PostData);
            var dictionary = new Dictionary<string, string>();

            foreach (string key in parsed.Keys)
            {
                dictionary[key] = parsed[key];
            }

            content = JsonSerializer.Serialize(dictionary);
        }

        if (content == null)
        {
            return null;
        }

        return JsonDocument.Parse(content).RootElement;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<RequestSizesResult> SizesAsync()
    {
        if (await ResponseAsync().ConfigureAwait(false) is not Response res)
        {
            throw new PlaywrightException("Unable to fetch resources sizes.");
        }

        return (await res.SendMessageToServerAsync("sizes").ConfigureAwait(false))?.GetProperty("sizes").ToObject<RequestSizesResult>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<Dictionary<string, string>> AllHeadersAsync()
        => (await ActualHeadersAsync().ConfigureAwait(false)).Headers;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IReadOnlyList<Header>> HeadersArrayAsync()
        => (await ActualHeadersAsync().ConfigureAwait(false)).HeadersArray;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> HeaderValueAsync(string name)
        => (await ActualHeadersAsync().ConfigureAwait(false)).Get(name);

    private Task<RawHeaders> ActualHeadersAsync()
    {
        if (_fallbackOverrides.Headers != null)
        {
            return Task.FromResult(RawHeaders.FromHeadersObjectLossy(_fallbackOverrides.Headers));
        }
        if (_rawHeadersTask == null)
        {
            _rawHeadersTask = GetRawHeadersTaskAsync();
        }

        return _rawHeadersTask;
    }

    private async Task<RawHeaders> GetRawHeadersTaskAsync()
    {
        var headerList = (await SendMessageToServerAsync("rawRequestHeaders").ConfigureAwait(false))?.GetProperty("headers").ToObject<List<NameValue>>();
        return new(headerList);
    }

    internal void ApplyFallbackOverrides(RouteFallbackOptions overrides)
    {
        _fallbackOverrides.Url = overrides?.Url ?? _fallbackOverrides.Url;
        _fallbackOverrides.Method = overrides?.Method ?? _fallbackOverrides.Method;
        _fallbackOverrides.Headers = overrides?.Headers ?? _fallbackOverrides.Headers;
        _fallbackOverrides.PostData = overrides?.PostData ?? _fallbackOverrides.PostData;
    }

    internal RouteFallbackOptions FallbackOverridesForContinue() => _fallbackOverrides;

    internal Task<bool> TargetClosedAsync()
    {
        var frame = _initializer.Frame;
        var result = ((Worker)ServiceWorker)?.ClosedTcs.Task ?? (frame?.Page as Page)?.ClosedOrCrashedTcs.Task;
        return result ?? new TaskCompletionSource<bool>().Task;
    }

    internal void SetResponseEndTiming(float responseEndTiming)
    {
        Timing.ResponseEnd = responseEndTiming;
        if (Timing.ResponseStart == -1)
        {
            Timing.ResponseStart = responseEndTiming;
        }
    }
}
