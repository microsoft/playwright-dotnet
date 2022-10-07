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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

/// <summary>
/// <see cref="IRoute"/>.
/// </summary>
internal partial class Route : ChannelOwnerBase, IChannelOwner<Route>, IRoute
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

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<Route> IChannelOwner<Route>.Channel => _channel;

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
            options.Path,
            options.Response).ConfigureAwait(false);

        await RaceWithTargetCloseAsync(_channel.FulfillAsync(normalized)).ConfigureAwait(false);
        ReportHandled(true);
    }

    public async Task AbortAsync(string errorCode = RequestAbortErrorCode.Failed)
    {
        CheckNotHandled();
        await RaceWithTargetCloseAsync(_channel.AbortAsync(errorCode)).ConfigureAwait(false);
        ReportHandled(true);
    }

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
            () => RaceWithTargetCloseAsync(_channel.ContinueAsync(url: options.Url, method: options.Method, postData: options.PostData, headers: options.Headers)),
            @internal).ConfigureAwait(false);
    }

    private async Task RaceWithTargetCloseAsync(Task task)
    {
        var serviceWorkerOrPageClosedTask = ((Worker)((Request)Request).ServiceWorker)?.ClosedTcs.Task ?? ((Page)Request.Frame.Page)?.ClosedOrCrashedTcs.Task;
        if (serviceWorkerOrPageClosedTask == null)
        {
            task.IgnoreException();
            return;
        }

        // When page closes or crashes, we catch any potential rejects from this Route.
        // Note that page could be missing when routing popup's initial request that
        // does not have a Page initialized just yet.
        if (task != await Task.WhenAny(task, serviceWorkerOrPageClosedTask).ConfigureAwait(false))
        {
            task.IgnoreException();
        }
        else
        {
            await task.ConfigureAwait(false);
        }
    }

    private async Task<IDictionary<string, object>> NormalizeFulfillParametersAsync(
        int? status,
        IEnumerable<KeyValuePair<string, string>> headers,
        string contentType,
        string body,
        byte[] bodyContent,
        string path,
        IAPIResponse response)
    {
        string fetchResponseUid = null;

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
}
