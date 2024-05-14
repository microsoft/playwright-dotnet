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
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class APIRequestContext : ChannelOwner, IAPIRequestContext
{
    internal readonly Tracing _tracing;

    internal APIRequest _request;

    public APIRequestContext(ChannelOwner parent, string guid, APIRequestContextInitializer initializer) : base(parent, guid)
    {
        _tracing = initializer.Tracing;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask DisposeAsync()
    {
        await SendMessageToServerAsync("dispose").ConfigureAwait(false);
        _tracing.ResetStackCounter();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> FetchAsync(IRequest request, APIRequestContextOptions options = null)
        => InnerFetchAsync(request, null, options);

    internal async Task<IAPIResponse> InnerFetchAsync(IRequest request, string urlOverride, APIRequestContextOptions options = null)
    {
        options ??= new APIRequestContextOptions();
        if (string.IsNullOrEmpty(options.Method))
        {
            options.Method = request.Method;
        }
        if (options.Headers == null)
        {
            // Cannot call allHeaders() here as the request may be paused inside route handler.
            options.Headers = request.Headers;
        }
        if (options.Data == null && options.DataByte == null && options.DataObject == null && options.DataString == null && options.Form == null && options.Multipart == null)
        {
            options.DataByte = request.PostDataBuffer;
        }
        return await FetchAsync(urlOverride ?? request.Url, options).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IAPIResponse> FetchAsync(string url, APIRequestContextOptions options = null)
    {
        options ??= new APIRequestContextOptions();

        if (options.MaxRedirects != null && options.MaxRedirects < 0)
        {
            throw new PlaywrightException("'maxRedirects' should be greater than or equal to '0'");
        }
        if (new[] { options.Data, options.DataByte, options.DataObject, options.DataString, options.Form, options.Multipart }.Count(x => x != null) > 1)
        {
            throw new PlaywrightException("Only one of 'data', 'form' or 'multipart' can be specified");
        }

        var queryParams = new Dictionary<string, string>();
        if (options.Params != null)
        {
            queryParams = options.Params.ToDictionary(x => x.Key, x => x.Value.ToString());
        }
        byte[] postData = null;
        object jsonData = null;
        string dataString = !string.IsNullOrEmpty(options.Data) ? options.Data : options.DataString;
        if (!string.IsNullOrEmpty(dataString))
        {
            if (IsJsonContentType(options.Headers?.ToDictionary(x => x.Key, x => x.Value)))
            {
                jsonData = IsJsonParsable(dataString) ? dataString : JsonSerializer.Serialize(dataString, _connection.DefaultJsonSerializerOptionsKeepNulls);
            }
            else
            {
                postData = System.Text.Encoding.UTF8.GetBytes(dataString);
            }
        }
        else if (options.DataByte != null)
        {
            postData = options.DataByte;
        }
        else if (options.DataObject != null)
        {
            jsonData = JsonSerializer.Serialize(options.DataObject, _connection.DefaultJsonSerializerOptionsKeepNulls);
        }

        var message = new Dictionary<string, object>
        {
            ["url"] = url,
            ["method"] = options?.Method,
            ["failOnStatusCode"] = options?.FailOnStatusCode,
            ["ignoreHTTPSErrors"] = options?.IgnoreHTTPSErrors,
            ["maxRedirects"] = options?.MaxRedirects,
            ["timeout"] = options.Timeout,
            ["params"] = queryParams?.ToProtocol(),
            ["headers"] = options.Headers?.ToProtocol(),
            ["jsonData"] = jsonData,
            ["postData"] = postData != null ? Convert.ToBase64String(postData) : null,
            ["formData"] = ((FormData)options.Form)?.ToProtocol(throwWhenSerializingFilePayloads: true),
            ["multipartData"] = ((FormData)options.Multipart)?.ToProtocol(),
        };

        var response = await SendMessageToServerAsync("fetch", message).ConfigureAwait(false);
        return new APIResponse(this, response?.GetProperty("response").ToObject<Transport.Protocol.APIResponse>());
    }

    private bool IsJsonContentType(IDictionary<string, string> headers)
    {
        if (headers == null)
        {
            return false;
        }
        var contentType = headers.FirstOrDefault(x => x.Key.Equals("content-type", StringComparison.OrdinalIgnoreCase));
        if (contentType.Value == null)
        {
            return false;
        }
        return contentType.Value.Contains("application/json");
    }

    private bool IsJsonParsable(string dataString)
    {
        try
        {
            JsonSerializer.Deserialize<JsonElement>(dataString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> DeleteAsync(string url, APIRequestContextOptions options = null) => FetchAsync(url, WithMethod(options, "DELETE"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> GetAsync(string url, APIRequestContextOptions options = null) => FetchAsync(url, WithMethod(options, "GET"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> HeadAsync(string url, APIRequestContextOptions options = null) => FetchAsync(url, WithMethod(options, "HEAD"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> PatchAsync(string url, APIRequestContextOptions options = null) => FetchAsync(url, WithMethod(options, "PATCH"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> PostAsync(string url, APIRequestContextOptions options = null) => FetchAsync(url, WithMethod(options, "POST"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<IAPIResponse> PutAsync(string url, APIRequestContextOptions options = null) => FetchAsync(url, WithMethod(options, "PUT"));

    private APIRequestContextOptions WithMethod(APIRequestContextOptions options, string method)
    {
        options = ClassUtils.Clone<APIRequestContextOptions>(options);
        options.Method = method;
        return options;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> StorageStateAsync(APIRequestContextStorageStateOptions options = null)
    {
        string state = JsonSerializer.Serialize(
            await SendMessageToServerAsync<StorageState>("storageState").ConfigureAwait(false),
            JsonExtensions.DefaultJsonSerializerOptions);

        if (!string.IsNullOrEmpty(options?.Path))
        {
            File.WriteAllText(options?.Path, state);
        }

        return state;
    }

    IFormData IAPIRequestContext.CreateFormData() => new FormData();
}
