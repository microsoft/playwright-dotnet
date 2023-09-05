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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

/// <summary>
/// <see cref="IRoute"/>.
/// </summary>
internal class Route : ChannelOwnerBase, IChannelOwner<Route>, IRoute
{
    private readonly RouteChannel _channel;
    private readonly RouteInitializer _initializer;
    private readonly Request _request;
    private TaskCompletionSource<bool> _handlingTask;

    internal Route(IChannelOwner parent, string guid, RouteInitializer initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
        _initializer = initializer;
        _request = initializer.Request;
    }

    public IRequest Request => _initializer.Request;

    internal BrowserContext _context { get; set; }

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Route> IChannelOwner<Route>.Channel => _channel;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task FulfillAsync(RouteFulfillOptions options = default)
    {
        CheckNotHandled();
        options ??= new RouteFulfillOptions();
        var normalized = await NormalizeFulfillParametersAsync(
            options.Status,
            options.Headers,
            options.ContentType,
            options.Body,
            options.BodyBytes,
            options.Json,
            options.Path,
            options.Response).ConfigureAwait(false);
        normalized["requestUrl"] = _request._initializer.Url;
        await RaceWithTargetCloseAsync(_channel.FulfillAsync(normalized)).ConfigureAwait(false);
        ReportHandled(true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task AbortAsync(string errorCode = RequestAbortErrorCode.Failed)
    {
        CheckNotHandled();
        await RaceWithTargetCloseAsync(_channel.AbortAsync(_request._initializer.Url, errorCode)).ConfigureAwait(false);
        ReportHandled(true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task ContinueAsync(RouteContinueOptions options = default)
    {
        CheckNotHandled();
        _request.ApplyFallbackOverrides(new RouteFallbackOptions().FromRouteContinueOptions(options));
        await InnerContinueAsync().ConfigureAwait(false);
        ReportHandled(true);
    }

    internal async Task InnerContinueAsync(bool @internal = false)
    {
        var options = _request.FallbackOverridesForContinue();
        await _channel.Connection.WrapApiCallAsync(
            () => RaceWithTargetCloseAsync(_channel.ContinueAsync(requestUrl: _request._initializer.Url, url: options.Url, method: options.Method, postData: options.PostData, headers: options.Headers, isFallback: @internal)),
            @internal).ConfigureAwait(false);
    }

    private async Task RaceWithTargetCloseAsync(Task task)
    {
        var targetClosedTask = ((Request)Request).TargetClosedAsync();

        // When page closes or crashes, we catch any potential rejects from this Route.
        // Note that page could be missing when routing popup's initial request that
        // does not have a Page initialized just yet.
        if (task != await Task.WhenAny(task, targetClosedTask).ConfigureAwait(false))
        {
            task.IgnoreException();
        }
        else
        {
            await task.ConfigureAwait(false);
        }
    }

    private async Task<Dictionary<string, object>> NormalizeFulfillParametersAsync(
        int? status,
        IEnumerable<KeyValuePair<string, string>> headers,
        string contentType,
        string body,
        byte[] bodyContent,
        object json,
        string path,
        IAPIResponse response)
    {
        string fetchResponseUid = null;

        if (json != null)
        {
            if (body != null || bodyContent != null || path != null)
            {
                throw new ArgumentException("Cannot provide both 'json' and 'body', 'bodyBytes' or 'path'");
            }
            body = JsonSerializer.Serialize(json);
        }

        if (response != null)
        {
            status ??= response.Status;
            headers ??= response.Headers;
            if (body == null && path == null && response is APIResponse responseImpl)
            {
                if (responseImpl._context._channel.Connection == this._channel.Connection)
                {
                    fetchResponseUid = responseImpl.FetchUid();
                }
                else
                {
                    bodyContent = await response.BodyAsync().ConfigureAwait(false);
                }
            }
        }

        string resultBody = null;
        bool isBase64 = false;
        int length = 0;

        if (!string.IsNullOrEmpty(path))
        {
            byte[] content = File.ReadAllBytes(path);
            resultBody = Convert.ToBase64String(content);
            isBase64 = true;
            length = resultBody.Length;
        }
        else if (!string.IsNullOrEmpty(body))
        {
            resultBody = body;
            isBase64 = false;
            length = resultBody.Length;
        }
        else if (bodyContent != null)
        {
            resultBody = Convert.ToBase64String(bodyContent);
            isBase64 = true;
            length = resultBody.Length;
        }

        var resultHeaders = new Dictionary<string, string>();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                resultHeaders[header.Key.ToLowerInvariant()] = header.Value;
            }
        }

        if (!string.IsNullOrEmpty(contentType))
        {
            resultHeaders["content-type"] = contentType;
        }
        else if (json != null)
        {
            resultHeaders["content-type"] = "application/json";
        }
        else if (!string.IsNullOrEmpty(path))
        {
            resultHeaders["content-type"] = path.GetContentType();
        }

        if (length > 0 && !resultHeaders.ContainsKey("content-length"))
        {
            resultHeaders["content-length"] = length.ToString(CultureInfo.InvariantCulture);
        }

        return new Dictionary<string, object>()
        {
            ["status"] = status ?? 200,
            ["headers"] = resultHeaders.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray(),
            ["body"] = resultBody,
            ["isBase64"] = isBase64,
            ["fetchResponseUid"] = fetchResponseUid,
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task FallbackAsync(RouteFallbackOptions options = null)
    {
        CheckNotHandled();
        _request.ApplyFallbackOverrides(options);
        ReportHandled(false);
        return Task.CompletedTask;
    }

    internal async Task RedirectNavigationRequestAsync(string url)
    {
        CheckNotHandled();
        await RaceWithTargetCloseAsync(_channel.RedirectNavigationRequestAsync(url)).ConfigureAwait(false);
        ReportHandled(true);
    }

    internal Task<bool> StartHandlingAsync()
    {
        _handlingTask = new();
        return _handlingTask.Task;
    }

    private void CheckNotHandled()
    {
        if (_handlingTask == null)
        {
            throw new InvalidOperationException("Route is already handled!");
        }
    }

    private void ReportHandled(bool handled)
    {
        var chain = _handlingTask;
        _handlingTask = null;
        chain.SetResult(handled);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> FetchAsync(RouteFetchOptions options)
        => _channel.Connection.WrapApiCallAsync(
            () =>
            {
                var apiRequest = (APIRequestContext)_context.APIRequest;
                return apiRequest.InnerFetchAsync(_request, options?.Url, new()
                {
                    Headers = options?.Headers,
                    Method = options?.Method,
                    DataByte = options?.PostData,
                    MaxRedirects = options?.MaxRedirects,
                    Timeout = options?.Timeout,
                });
            });
}
