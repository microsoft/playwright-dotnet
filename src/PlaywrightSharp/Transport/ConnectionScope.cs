using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport
{
    internal class ConnectionScope
    {
        private readonly string _guid;
        private readonly ILoggerFactory _loggerFactory;
        private readonly List<ConnectionScope> _children = new List<ConnectionScope>();
        private readonly ConcurrentDictionary<string, IChannelOwner> _objects = new ConcurrentDictionary<string, IChannelOwner>();

        public ConnectionScope(Connection connection, string guid, ILoggerFactory loggerFactory)
        {
            Connection = connection;
            _guid = guid;
            _loggerFactory = loggerFactory;
        }

        public Connection Connection { get; }

        public ConnectionScope Parent { get; set; }

        internal void Dispose()
        {
            foreach (var child in _children.ToArray())
            {
                child.Dispose();
            }

            _children.Clear();

            // Delete self from scopes and objects.
            Connection.RemoveScope(_guid);
            Connection.RemoveObject(_guid);

            // Delete all of the objects from connection.
            foreach (string guid in _objects.Keys)
            {
                Connection.RemoveObject(guid);
            }

            if (Parent != null)
            {
                Parent.RemoveObject(_guid);
                Parent.RemoveChild(this);
            }
        }

        internal ConnectionScope CreateChild(string guid)
        {
            var scope = Connection.CreateScope(guid);
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
                    result = new BindingCall(this, guid, initializer?.ToObject<BindingCallInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Playwright:
#pragma warning disable CA2000 // Dispose objects before losing scope
                    result = new Playwright(this, guid, initializer?.ToObject<PlaywrightInitializer>(Connection.GetDefaultJsonSerializerOptions()), _loggerFactory);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    break;
                case ChannelOwnerType.Browser:
                    result = new Browser(this, guid, initializer?.ToObject<BrowserInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.BrowserServer:
                    result = new BrowserServer(this, guid, initializer?.ToObject<BrowserServerInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.BrowserType:
                    result = new BrowserType(this, guid, initializer?.ToObject<BrowserTypeInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Context:
                    result = new BrowserContext(this, guid, initializer?.ToObject<BrowserContextInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.ConsoleMessage:
                    result = new ConsoleMessage(this, guid, initializer?.ToObject<ConsoleMessageInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Dialog:
                    result = new Dialog(this, guid, initializer?.ToObject<DialogInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Download:
                    result = new Download(this, guid, initializer?.ToObject<DownloadInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.ElementHandle:
                    result = new ElementHandle(this, guid, initializer?.ToObject<ElementHandleInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Frame:
                    result = new Frame(this, guid, initializer?.ToObject<FrameInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.JSHandle:
                    result = new JSHandle(this, guid, initializer?.ToObject<JSHandleInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Page:
                    result = new Page(this, guid, initializer?.ToObject<PageInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Request:
                    result = new Request(this, guid, initializer?.ToObject<RequestInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Response:
                    result = new Response(this, guid, initializer?.ToObject<ResponseInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Route:
                    result = new Route(this, guid, initializer?.ToObject<RouteInitializer>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                default:
                    Debug.Write("Missing type " + type);
                    break;
            }

            _objects.TryAdd(guid, result);
            Connection.OnObjectCreated(guid, result);
        }

        internal Task<JsonElement?> SendMessageToServer(string guid, string method, object args = null)
            => Connection.SendMessageToServerAsync<JsonElement?>(guid, method, args);

        internal Task<T> SendMessageToServer<T>(string guid, string method, object args)
            => Connection.SendMessageToServerAsync<T>(guid, method, args);

        private void RemoveObject(string guid) => _objects.TryRemove(guid, out _);

        private void RemoveChild(ConnectionScope scope) => _children.Remove(scope);
    }
}
