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

        internal event EventHandler<RequestEventArgs> Request;

        internal event EventHandler<RequestEventArgs> RequestFinished;

        internal event EventHandler<PageChannelRequestFailedEventArgs> RequestFailed;

        internal event EventHandler<ResponseEventArgs> Response;

        internal event EventHandler DOMContentLoaded;

        internal event EventHandler<PageChannelPopupEventArgs> Popup;

        internal event EventHandler<BindingCallEventArgs> BindingCall;

        internal event EventHandler<RouteEventArgs> Route;

        internal event EventHandler<FrameNavigatedEventArgs> FrameNavigated;

        internal event EventHandler<FrameEventArgs> FrameAttached;

        internal event EventHandler<FrameEventArgs> FrameDetached;

        internal event EventHandler<DialogEventArgs> Dialog;

        internal event EventHandler<ConsoleEventArgs> Console;

        internal event EventHandler<DownloadEventArgs> Download;

        internal event EventHandler<PageErrorEventArgs> PageError;

        internal event EventHandler<FileChooserChannelEventArgs> FileChooser;

        internal event EventHandler<EventArgs> Load;

        internal event EventHandler<WorkerChannelEventArgs> Worker;

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
                case "domcontentloaded":
                    DOMContentLoaded?.Invoke(this, EventArgs.Empty);
                    break;
                case "load":
                    Load?.Invoke(this, EventArgs.Empty);
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
                case "pageError":
                    PageError?.Invoke(this, serverParams?.GetProperty("error").ToObject<PageErrorEventArgs>(Scope.Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "fileChooser":
                    FileChooser?.Invoke(this, serverParams?.ToObject<FileChooserChannelEventArgs>(Scope.Connection.GetDefaultJsonSerializerOptions()));
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
                    Request?.Invoke(this, new RequestEventArgs { Request = serverParams?.ToObject<RequestChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "requestFinished":
                    RequestFinished?.Invoke(this, new RequestEventArgs { Request = serverParams?.ToObject<RequestChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "requestFailed":
                    RequestFailed?.Invoke(this, serverParams?.ToObject<PageChannelRequestFailedEventArgs>(Scope.Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "response":
                    Response?.Invoke(this, new ResponseEventArgs { Response = serverParams?.ToObject<ResponseChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "download":
                    Download?.Invoke(this, new DownloadEventArgs() { Download = serverParams?.ToObject<DownloadChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "worker":
                    Worker?.Invoke(
                        this,
                        new WorkerChannelEventArgs
                        {
                            WorkerChannel = serverParams?.ToObject<WorkerChannel>(Scope.Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
            }
        }

        internal Task SetDefaultTimeoutNoReplyAsync(int timeout)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetDefaultNavigationTimeoutNoReplyAsync(int timeout)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultNavigationTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetFileChooserInterceptedNoReplyAsync(bool intercepted)
            => Scope.SendMessageToServer<PageChannel>(
                Guid,
                "setFileChooserInterceptedNoReply",
                new Dictionary<string, object>
                {
                    ["intercepted"] = intercepted,
                });

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

        internal Task<ResponseChannel> GoBackAsync(int? timeout, LifecycleEvent? waitUntil)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            };

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(Guid, "goBack", args);
        }

        internal Task<ResponseChannel> GoForwardAsync(int? timeout, LifecycleEvent? waitUntil)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            };

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(Guid, "goForward", args);
        }

        internal Task<ResponseChannel> ReloadAsync(int? timeout, LifecycleEvent? waitUntil)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            };

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Scope.SendMessageToServer<ResponseChannel>(Guid, "reload", args);
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
            => Scope.SendMessageToServer(
                Guid,
                "setViewportSize",
                new Dictionary<string, object>
                {
                    ["viewportSize"] = viewport,
                });

        internal Task KeyboardDownAsync(string key)
            => Scope.SendMessageToServer(
                Guid,
                "keyboardDown",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task EmulateMediaAsync(Dictionary<string, object> args)
            => Scope.SendMessageToServer(Guid, "emulateMedia", args);

        internal Task KeyboardUpAsync(string key)
            => Scope.SendMessageToServer(
                Guid,
                "keyboardUp",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task TypeAsync(string text, int delay)
            => Scope.SendMessageToServer(
                Guid,
                "keyboardType",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                    ["delay"] = delay,
                });

        internal Task PressAsync(string key, int delay)
            => Scope.SendMessageToServer(
                Guid,
                "keyboardPress",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["delay"] = delay,
                });

        internal Task InsertTextAsync(string text)
            => Scope.SendMessageToServer(
                Guid,
                "keyboardInsertText",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                });

        internal Task MouseDownAsync(MouseButton button, int clickCount)
            => Scope.SendMessageToServer(
                Guid,
                "mouseDown",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseMoveAsync(double x, double y, int? steps)
            => Scope.SendMessageToServer(
                Guid,
                "mouseMove",
                new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["steps"] = steps,
                });

        internal Task MouseUpAsync(MouseButton button, int clickCount)
            => Scope.SendMessageToServer(
                Guid,
                "mouseUp",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseClickAsync(double x, double y, int delay, MouseButton button, int clickCount)
            => Scope.SendMessageToServer(
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

        internal Task SetExtraHttpHeadersAsync(IDictionary<string, string> headers)
            => Scope.SendMessageToServer(
                Guid,
                "setExtraHTTPHeaders",
                new Dictionary<string, object>
                {
                    ["headers"] = headers,
                });

        internal Task<string> ScreenshotAsync(
            string path,
            bool fullPage,
            Rect clip,
            bool omitBackground,
            ScreenshotFormat? type,
            int? quality,
            int? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["path"] = path,
                ["fullPage"] = fullPage,
                ["clip"] = clip,
                ["omitBackground"] = omitBackground,
                ["type"] = type,
                ["timeout"] = timeout,
            };

            if (quality != null)
            {
                args["quality"] = quality;
            }

            return Scope.SendMessageToServer<string>(Guid, "screenshot", args);
        }
    }
}
