using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Channel;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport
{
    internal class ConnectionScope
    {
        private readonly Playwright _client;
        private readonly string _guid;
        private readonly List<ConnectionScope> _children = new List<ConnectionScope>();
        private readonly ConcurrentDictionary<string, IChannelOwner> _objects = new ConcurrentDictionary<string, IChannelOwner>();

        public ConnectionScope(Playwright client, string guid)
        {
            _client = client;
            _guid = guid;
        }

        public ConnectionScope Parent { get; set; }

        internal void Dispose()
        {
            foreach (var child in _children)
            {
                child.Dispose();
            }

            _children.Clear();

            // Delete self from scopes and objects.
            _client.RemoveScope(_guid);
            _client.RemoveObject(_guid);

            // Delete all of the objects from connection.
            foreach (string guid in _objects.Keys)
            {
                _client.RemoveObject(guid);
            }

            if (Parent != null)
            {
                Parent.RemoveObject(_guid);
                Parent.RemoveChild(this);
            }
        }

        internal ConnectionScope CreateChild(string guid)
        {
            var scope = _client.CreateScope(guid);
            _children.Add(scope);
            scope.Parent = this;
            return scope;
        }

        internal void CreateRemoteObject(ChannelOwnerType type, string guid, JsonElement? initializer)
        {
            IChannelOwner result = null;

            switch (type)
            {
                case ChannelOwnerType.BindingCall:
                    result = new BindingCall(this, guid, initializer?.ToObject<BindingCallInitializer>());
                    break;
                case ChannelOwnerType.Browser:
                    result = new Browser(this, guid, initializer?.ToObject<BrowserInitializer>());
                    break;
                case ChannelOwnerType.BrowserType:
                    result = new BrowserType(this, guid, initializer?.ToObject<BrowserTypeInitializer>());
                    break;
                case ChannelOwnerType.Context:
                    result = new BrowserContext(this, guid, initializer?.ToObject<BrowserContextInitializer>());
                    break;
                case ChannelOwnerType.ConsoleMessage:
                    result = new ConsoleMessage(this, guid, initializer?.ToObject<ConsoleMessageInitializer>());
                    break;
                case ChannelOwnerType.Dialog:
                    result = new Dialog(this, guid, initializer?.ToObject<DialogInitializer>());
                    break;
                case ChannelOwnerType.Download:
                    result = new Download(this, guid, initializer?.ToObject<DownloadInitializer>());
                    break;
                case ChannelOwnerType.ElementHandle:
                    result = new ElementHandle(this, guid, initializer?.ToObject<ElementHandleInitializer>());
                    break;
                case ChannelOwnerType.Frame:
                    result = new Frame(this, guid, initializer?.ToObject<FrameInitializer>());
                    break;
                case ChannelOwnerType.JSHandle:
                    result = new JSHandle(this, guid, initializer?.ToObject<JSHandleInitializer>());
                    break;
                case ChannelOwnerType.Page:
                    result = new Page(this, guid, initializer?.ToObject<PageInitializer>());
                    break;
                case ChannelOwnerType.Request:
                    result = new Request(this, guid, initializer?.ToObject<RequestInitializer>());
                    break;
                case ChannelOwnerType.Response:
                    result = new Response(this, guid, initializer?.ToObject<ResponseInitializer>());
                    break;
                case ChannelOwnerType.Route:
                    result = new Route(this, guid, initializer?.ToObject<RouteInitializer>());
                    break;
                default:
                    Debug.Write("Missing type " + type);
                    break;
            }

            _objects.TryAdd(guid, result);
            _client.OnObjectCreated(guid, result);
        }

        internal Task<JsonElement?> SendMessageToServer(string guid, string method, params object[] args)
            => _client.SendMessageToServerAsync(guid, method, args);

        private void RemoveObject(string guid) => _objects.TryRemove(guid, out _);

        private void RemoveChild(ConnectionScope scope) => _children.Remove(scope);
    }
}
