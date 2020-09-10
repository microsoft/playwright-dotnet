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
        public BrowserContextChannel(string guid, ConnectionScope scope, BrowserContext owner) : base(guid, scope, owner)
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
                            BidingCall = serverParams?.GetProperty("binding").ToObject<BindingCallChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object,
                        });
                    break;
                case "route":
                    Route?.Invoke(
                        this,
                        new RouteEventArgs
                        {
                            Route = serverParams?.GetProperty("route").ToObject<RouteChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object,
                            Request = serverParams?.GetProperty("request").ToObject<RequestChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object,
                        });
                    break;
                case "page":
                    Page?.Invoke(
                        this,
                        new BrowserContextPageEventArgs
                        {
                            PageChannel = serverParams?.GetProperty("page").ToObject<PageChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
                case "crBackgroundPage":
                    BackgroundPage?.Invoke(
                        this,
                        new BrowserContextPageEventArgs
                        {
                            PageChannel = serverParams?.GetProperty("page").ToObject<PageChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
                case "crServiceWorker":
                    ServiceWorker?.Invoke(
                        this,
                        new WorkerChannelEventArgs
                        {
                            WorkerChannel = serverParams?.GetProperty("worker").ToObject<WorkerChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
            }
        }

        internal Task<PageChannel> NewPageAsync(string url)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "newPage",
                new Dictionary<string, object>
                {
                    ["url"] = url,
                });

        internal Task CloseAsync() => Scope.SendMessageToServer(Guid, "close");

        internal Task SetDefaultNavigationTimeoutNoReplyAsync(int timeout)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultNavigationTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetDefaultTimeoutNoReplyAsync(int timeout)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task ExposeBindingAsync(string name)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "exposeBinding",
                new Dictionary<string, object>
                {
                    ["name"] = name,
                });

        internal Task AddInitScriptAsync(string script)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "addInitScript",
                new Dictionary<string, object>
                {
                    ["source"] = script,
                });

        internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setNetworkInterceptionEnabled",
                new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                });

        internal Task SetHttpCredentialsAsync(Credentials credentials)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setHTTPCredentials",
                new Dictionary<string, object>
                {
                    ["httpCredentials"] = credentials,
                });

        internal Task SetOfflineAsync(bool offline)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setOffline",
                new Dictionary<string, object>
                {
                    ["offline"] = offline,
                });

        internal Task<IEnumerable<NetworkCookie>> GetCookiesAsync(string[] urls)
            => Scope.SendMessageToServer<IEnumerable<NetworkCookie>>(
                Guid,
                "cookies",
                new Dictionary<string, object>
                {
                    ["urls"] = urls,
                });

        internal Task AddCookiesAsync(IEnumerable<SetNetworkCookieParam> cookies)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "addCookies",
                new Dictionary<string, object>
                {
                    ["cookies"] = cookies,
                },
                true);

        internal Task GrantPermissionsAsync(ContextPermission[] permissions, string origin)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "grantPermissions",
                new Dictionary<string, object>
                {
                    ["permissions"] = permissions,
                    ["origin"] = origin,
                },
                true);

        internal Task ClearPermissionsAsync() => Scope.SendMessageToServer<PageChannel>(Guid, "clearPermissions", null);

        internal Task SetGeolocationAsync(Geolocation geolocation)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setGeolocation",
                new Dictionary<string, object>
                {
                    ["geolocation"] = geolocation,
                },
                true);

        internal Task ClearCookiesAsync() => Scope.SendMessageToServer<PageChannel>(Guid, "clearCookies", null);

        internal Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers)
            => Scope.SendMessageToServer(
                Guid,
                "setExtraHTTPHeaders",
                new Dictionary<string, object>
                {
                    ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
                });

        internal Task<CDPSessionChannel> NewCDPSessionAsync(IPage page)
            => Scope.SendMessageToServer<CDPSessionChannel>(
                Guid,
                "crNewCDPSession",
                new Dictionary<string, object>
                {
                    ["page"] = page,
                });
    }
}
