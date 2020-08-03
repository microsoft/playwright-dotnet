using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannel : Channel<Page>
    {
        public PageChannel(string guid, ConnectionScope scope, Page owner) : base(guid, scope, owner)
        {
        }

        internal event EventHandler Closed;

        internal event EventHandler Crashed;

        internal event EventHandler<PageChannelRequestEventArgs> Request;

        internal event EventHandler<PageChannelPopupEventArgs> Popup;

        internal event EventHandler<BindingCallEventArgs> BindingCall;

        internal event EventHandler<RouteEventArgs> Route;

        internal event EventHandler<FrameNavigatedEventArgs> FrameNavigated;

        internal event EventHandler<FrameEventArgs> FrameAttached;

        internal event EventHandler<FrameEventArgs> FrameDetached;

        internal event EventHandler<DialogEventArgs> Dialog;

        internal event EventHandler<ConsoleEventArgs> Console;

        internal override void OnMessage(string method, JsonElement? serverParams)
        {
            switch (method)
            {
                case "close":
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
                case "crash":
                    Crashed?.Invoke(this, EventArgs.Empty);
                    break;
                case "bindingCall":
                    BindingCall?.Invoke(
                        this,
                        new BindingCallEventArgs
                        {
                            BidingCall = serverParams?.ToObject<BindingCallChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object,
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
                case "popup":
                    Popup?.Invoke(this, new PageChannelPopupEventArgs
                    {
                        Page = serverParams?.ToObject<PageChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object,
                    });
                    break;
                case "frameAttached":
                    FrameAttached?.Invoke(this, new FrameEventArgs(serverParams?.ToObject<FrameChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object));
                    break;
                case "frameDetached":
                    FrameDetached?.Invoke(this, new FrameEventArgs(serverParams?.ToObject<FrameChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object));
                    break;
                case "frameNavigated":
                    FrameNavigated?.Invoke(this, serverParams?.ToObject<FrameNavigatedEventArgs>(Scope.Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "dialog":
                    Dialog?.Invoke(this, new DialogEventArgs(serverParams?.ToObject<DialogChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object));
                    break;
                case "console":
                    Console?.Invoke(this, new ConsoleEventArgs(serverParams?.ToObject<ConsoleMessage>(Scope.Connection.GetDefaultJsonSerializerOptions())));
                    break;
                case "request":
                    Request?.Invoke(this, new PageChannelRequestEventArgs { RequestChannel = null });
                    break;
            }
        }

        internal Task CloseAsync(PageCloseOptions options)
            => Scope.SendMessageToServer(
                Guid,
                "close",
                options);

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

        internal Task<ResponseChannel> ReloadAsync(NavigationOptions options)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = options?.Timeout,
            };

            if (options != null)
            {
                args["waitUntil"] = options.WaitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "reload",
                args);
        }

        internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setNetworkInterceptionEnabled",
                new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                });

        internal Task<PageChannel> GetOpenerAsync() => Scope.SendMessageToServer<PageChannel>(Guid, "opener", null);

        internal Task<SerializedAXNode> AccessibilitySnapshotAsync(bool? interestingOnly, IChannel<ElementHandle> root)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "accessibilitySnapshot",
                new Dictionary<string, object>
                {
                    ["interestingOnly"] = interestingOnly ?? true,
                    ["root"] = root,
                });

        internal Task SetViewportSizeAsync(ViewportSize viewport)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "setViewportSize",
                new Dictionary<string, object>
                {
                    ["viewportSize"] = viewport,
                });

        internal Task KeyboardDownAsync(string key)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "keyboardDown",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task KeyboardUpAsync(string key)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "keyboardUp",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task MouseDownAsync(MouseButton button, int clickCount)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "mouseDown",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseMoveAsync(double x, double y, int? steps)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "mouseMove",
                new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["steps"] = steps,
                });

        internal Task MouseUpAsync(MouseButton button, int clickCount)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "mouseUp",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseClickAsync(double x, double y, int delay, MouseButton button, int clickCount)
            => Scope.SendMessageToServer<SerializedAXNode>(
                Guid,
                "mouseClick",
                new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["delay"] = delay,
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });
    }
}
