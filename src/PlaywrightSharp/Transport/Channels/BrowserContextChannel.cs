using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport.Channels
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

        internal Task<PageChannel> NewPageAsync(string url)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "newPage",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                });

        internal Task CloseAsync() => Connection.SendMessageToServer(Guid, "close");

        internal Task SetDefaultNavigationTimeoutNoReplyAsync(int timeout)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultNavigationTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetDefaultTimeoutNoReplyAsync(int timeout)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task ExposeBindingAsync(string name, bool needsHandle)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "exposeBinding",
                new Dictionary<string, object>
                {
                    ["name"] = name,
                    ["needsHandle"] = needsHandle,
                });

        internal Task AddInitScriptAsync(string script)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "addInitScript",
                new Dictionary<string, object>
                {
                    ["source"] = script,
                });

        internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setNetworkInterceptionEnabled",
                new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                });

        internal Task SetHttpCredentialsAsync(Credentials credentials)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setHTTPCredentials",
                new Dictionary<string, object>
                {
                    ["httpCredentials"] = credentials,
                });

        internal Task SetOfflineAsync(bool offline)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setOffline",
                new Dictionary<string, object>
                {
                    ["offline"] = offline,
                });

        internal async Task<IEnumerable<NetworkCookie>> GetCookiesAsync(string[] urls)
        {
            return (await Connection.SendMessageToServer(
                Guid,
                "cookies",
                new Dictionary<string, object>
                {
                    ["urls"] = urls,
                }).ConfigureAwait(false))?.GetProperty("cookies").ToObject<IEnumerable<NetworkCookie>>();
        }

        internal Task AddCookiesAsync(IEnumerable<SetNetworkCookieParam> cookies)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "addCookies",
                new Dictionary<string, object>
                {
                    ["cookies"] = cookies,
                },
                true);

        internal Task GrantPermissionsAsync(ContextPermission[] permissions, string origin)
        {
            var args = new Dictionary<string, object>
            {
                ["permissions"] = permissions,
            };

            if (origin != null)
            {
                args["origin"] = origin;
            }

            return Connection.SendMessageToServer<PageChannel>(Guid, "grantPermissions", args, true);
        }

        internal Task ClearPermissionsAsync() => Connection.SendMessageToServer<PageChannel>(Guid, "clearPermissions", null);

        internal Task SetGeolocationAsync(Geolocation geolocation)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setGeolocation",
                new Dictionary<string, object>
                {
                    ["geolocation"] = geolocation,
                },
                true);

        internal Task ClearCookiesAsync() => Connection.SendMessageToServer<PageChannel>(Guid, "clearCookies", null);

        internal Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers)
            => Connection.SendMessageToServer(
                Guid,
                "setExtraHTTPHeaders",
                new Dictionary<string, object>
                {
                    ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
                });

        internal Task<CDPSessionChannel> NewCDPSessionAsync(IPage page)
            => Connection.SendMessageToServer<CDPSessionChannel>(
                Guid,
                "crNewCDPSession",
                new Dictionary<string, object>
                {
                    ["page"] = page,
                });
    }
}
