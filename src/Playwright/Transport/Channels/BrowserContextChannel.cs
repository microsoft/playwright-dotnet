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
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class BrowserContextChannel : Channel<BrowserContext>
    {
        public BrowserContextChannel(string guid, Connection connection, BrowserContext owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Close;

        internal event EventHandler<BrowserContextPageEventArgs> Page;

        internal event EventHandler<BrowserContextPageEventArgs> BackgroundPage;

        internal event EventHandler<WorkerChannelEventArgs> ServiceWorker;

        internal event EventHandler<BindingCallEventArgs> BindingCall;

        internal event EventHandler<RouteEventArgs> Route;

        internal event EventHandler<BrowserContextChannelRequestEventArgs> Request;

        internal event EventHandler<BrowserContextChannelRequestEventArgs> RequestFinished;

        internal event EventHandler<BrowserContextChannelRequestEventArgs> RequestFailed;

        internal event EventHandler<BrowserContextChannelResponseEventArgs> Response;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Close?.Invoke(this, EventArgs.Empty);
                    break;
                case "bindingCall":
                    BindingCall?.Invoke(
                        this,
                        new() { BidingCall = serverParams?.GetProperty("binding").ToObject<BindingCallChannel>(Connection.DefaultJsonSerializerOptions).Object });
                    break;
                case "route":
                    var route = serverParams?.GetProperty("route").ToObject<RouteChannel>(Connection.DefaultJsonSerializerOptions).Object;
                    var request = serverParams?.GetProperty("request").ToObject<RequestChannel>(Connection.DefaultJsonSerializerOptions).Object;
                    Route?.Invoke(
                        this,
                        new() { Route = route, Request = request });
                    break;
                case "page":
                    Page?.Invoke(
                        this,
                        new() { PageChannel = serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.DefaultJsonSerializerOptions) });
                    break;
                case "crBackgroundPage":
                    BackgroundPage?.Invoke(
                        this,
                        new() { PageChannel = serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.DefaultJsonSerializerOptions) });
                    break;
                case "crServiceWorker":
                    ServiceWorker?.Invoke(
                        this,
                        new() { WorkerChannel = serverParams?.GetProperty("worker").ToObject<WorkerChannel>(Connection.DefaultJsonSerializerOptions) });
                    break;
                case "request":
                    Request?.Invoke(this, serverParams?.ToObject<BrowserContextChannelRequestEventArgs>(Connection.DefaultJsonSerializerOptions));
                    break;
                case "requestFinished":
                    RequestFinished?.Invoke(this, serverParams?.ToObject<BrowserContextChannelRequestEventArgs>(Connection.DefaultJsonSerializerOptions));
                    break;
                case "requestFailed":
                    RequestFailed?.Invoke(this, serverParams?.ToObject<BrowserContextChannelRequestEventArgs>(Connection.DefaultJsonSerializerOptions));
                    break;
                case "response":
                    Response?.Invoke(this, serverParams?.ToObject<BrowserContextChannelResponseEventArgs>(Connection.DefaultJsonSerializerOptions));
                    break;
            }
        }

        internal Task<PageChannel> NewPageAsync()
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "newPage",
                null);

        internal Task CloseAsync() => Connection.SendMessageToServerAsync(Guid, "close");

        internal Task PauseAsync()
            => Connection.SendMessageToServerAsync(Guid, "pause", null);

        internal Task SetDefaultNavigationTimeoutNoReplyAsync(float timeout)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setDefaultNavigationTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetDefaultTimeoutNoReplyAsync(float timeout)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task ExposeBindingAsync(string name, bool needsHandle)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "exposeBinding",
                new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["needsHandle"] = needsHandle,
                });

        internal Task AddInitScriptAsync(string script)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "addInitScript",
                new Dictionary<string, object>
                {
                    ["source"] = script,
                });

        internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setNetworkInterceptionEnabled",
                new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                });

        internal Task SetOfflineAsync(bool offline)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setOffline",
                new Dictionary<string, object>
                {
                    ["offline"] = offline,
                });

        internal async Task<IReadOnlyList<BrowserContextCookiesResult>> CookiesAsync(IEnumerable<string> urls)
        {
            return (await Connection.SendMessageToServerAsync(
                Guid,
                "cookies",
                new Dictionary<string, object>
                {
                    ["urls"] = urls?.ToArray() ?? Array.Empty<string>(),
                }).ConfigureAwait(false))?.GetProperty("cookies").ToObject<IReadOnlyList<BrowserContextCookiesResult>>();
        }

        internal Task AddCookiesAsync(IEnumerable<Cookie> cookies)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "addCookies",
                new Dictionary<string, object>
                {
                    ["cookies"] = cookies,
                });

        internal Task GrantPermissionsAsync(IEnumerable<string> permissions, string origin)
        {
            var args = new Dictionary<string, object>
            {
                ["permissions"] = permissions?.ToArray(),
            };

            if (origin != null)
            {
                args["origin"] = origin;
            }

            return Connection.SendMessageToServerAsync<PageChannel>(Guid, "grantPermissions", args);
        }

        internal Task ClearPermissionsAsync() => Connection.SendMessageToServerAsync<PageChannel>(Guid, "clearPermissions", null);

        internal Task SetGeolocationAsync(Geolocation geolocation)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setGeolocation",
                new Dictionary<string, object>
                {
                    ["geolocation"] = geolocation,
                });

        internal Task ClearCookiesAsync() => Connection.SendMessageToServerAsync<PageChannel>(Guid, "clearCookies", null);

        internal Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => Connection.SendMessageToServerAsync(
                Guid,
                "setExtraHTTPHeaders",
                new Dictionary<string, object>
                {
                    ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
                });

        internal Task<StorageState> GetStorageStateAsync()
            => Connection.SendMessageToServerAsync<StorageState>(Guid, "storageState", null);

        internal Task TracingStartAsync(string name, bool? screenshots, bool? snapshots)
            => Connection.SendMessageToServerAsync(
                Guid,
                "tracingStart",
                new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["screenshots"] = screenshots,
                    ["snapshots"] = snapshots,
                });

        internal Task TracingStopAsync()
            => Connection.SendMessageToServerAsync(
                Guid,
                "tracingStop");

        internal Task StartChunkAsync(string title = null)
            => Connection.SendMessageToServerAsync(Guid, "tracingStartChunk", new Dictionary<string, object>
            {
                ["title"] = title,
            });

        internal async Task<Artifact> StopChunkAsync(bool save = false, bool skipCompress = false)
        {
            var result = await Connection.SendMessageToServerAsync(Guid, "tracingStopChunk", new Dictionary<string, object>
            {
                ["save"] = save,
                ["skipCompress"] = skipCompress,
            }).ConfigureAwait(false);

            if (save)
                return result.GetObject<Artifact>("artifact", Connection);

            return null;
        }

        internal async Task<Artifact> HarExportAsync()
        {
            var result = await Connection.SendMessageToServerAsync(
            Guid,
            "harExport").ConfigureAwait(false);
            return result.GetObject<Artifact>("artifact", Connection);
        }
    }
}
