using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
                        new BindingCallEventArgs
                        {
                            BidingCall = serverParams?.GetProperty("binding").ToObject<BindingCallChannel>(Connection.GetDefaultJsonSerializerOptions()).Object,
                        });
                    break;
                case "route":
                    Route?.Invoke(
                        this,
                        new RouteEventArgs
                        {
                            Route = serverParams?.GetProperty("route").ToObject<RouteChannel>(Connection.GetDefaultJsonSerializerOptions()).Object,
                            Request = serverParams?.GetProperty("request").ToObject<RequestChannel>(Connection.GetDefaultJsonSerializerOptions()).Object,
                        });
                    break;
                case "page":
                    Page?.Invoke(
                        this,
                        new BrowserContextPageEventArgs
                        {
                            PageChannel = serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
                case "crBackgroundPage":
                    BackgroundPage?.Invoke(
                        this,
                        new BrowserContextPageEventArgs
                        {
                            PageChannel = serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
                case "crServiceWorker":
                    ServiceWorker?.Invoke(
                        this,
                        new WorkerChannelEventArgs
                        {
                            WorkerChannel = serverParams?.GetProperty("worker").ToObject<WorkerChannel>(Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
            }
        }

        internal Task<PageChannel> NewPageAsync()
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "newPage",
                null);

        internal Task CloseAsync() => Connection.SendMessageToServerAsync(Guid, "close");

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

        internal Task SetHttpCredentialsAsync(HttpCredentials credentials)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setHTTPCredentials",
                new Dictionary<string, object>
                {
                    ["httpCredentials"] = credentials,
                });

        internal Task SetOfflineAsync(bool offline)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setOffline",
                new Dictionary<string, object>
                {
                    ["offline"] = offline,
                });

        internal async Task<IReadOnlyCollection<BrowserContextCookiesResult>> GetCookiesAsync(IEnumerable<string> urls)
        {
            return (await Connection.SendMessageToServerAsync(
                Guid,
                "cookies",
                new Dictionary<string, object>
                {
                    ["urls"] = urls?.ToArray() ?? Array.Empty<string>(),
                }).ConfigureAwait(false))?.GetProperty("cookies").ToObject<IReadOnlyCollection<BrowserContextCookiesResult>>();
        }

        internal Task AddCookiesAsync(IEnumerable<Cookie> cookies)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "addCookies",
                new Dictionary<string, object>
                {
                    ["cookies"] = cookies,
                },
                true);

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

            return Connection.SendMessageToServerAsync<PageChannel>(Guid, "grantPermissions", args, true);
        }

        internal Task ClearPermissionsAsync() => Connection.SendMessageToServerAsync<PageChannel>(Guid, "clearPermissions", null);

        internal Task SetGeolocationAsync(Geolocation geolocation)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setGeolocation",
                new Dictionary<string, object>
                {
                    ["geolocation"] = geolocation,
                },
                true);

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
    }
}
