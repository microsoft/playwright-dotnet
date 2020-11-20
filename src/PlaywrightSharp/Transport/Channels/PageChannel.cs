using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport.Channels
{
    internal class PageChannel : Channel<Page>
    {
        public PageChannel(string guid, Connection connection, Page owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Closed;

        internal event EventHandler Crashed;

        internal event EventHandler<RequestEventArgs> Request;

        internal event EventHandler<PageChannelRequestEventArgs> RequestFinished;

        internal event EventHandler<PageChannelRequestEventArgs> RequestFailed;

        internal event EventHandler<ResponseEventArgs> Response;

        internal event EventHandler<WebSocketEventArgs> WebSocket;

        internal event EventHandler DOMContentLoaded;

        internal event EventHandler<PageChannelPopupEventArgs> Popup;

        internal event EventHandler<BindingCallEventArgs> BindingCall;

        internal event EventHandler<RouteEventArgs> Route;

        internal event EventHandler<FrameEventArgs> FrameAttached;

        internal event EventHandler<FrameEventArgs> FrameDetached;

        internal event EventHandler<DialogEventArgs> Dialog;

        internal event EventHandler<ConsoleEventArgs> Console;

        internal event EventHandler<DownloadEventArgs> Download;

        internal event EventHandler<PageErrorEventArgs> PageError;

        internal event EventHandler<FileChooserChannelEventArgs> FileChooser;

        internal event EventHandler<EventArgs> Load;

        internal event EventHandler<WorkerChannelEventArgs> Worker;

        internal event EventHandler<VideoEventArgs> Video;

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
                case "popup":
                    Popup?.Invoke(this, new PageChannelPopupEventArgs
                    {
                        Page = serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.GetDefaultJsonSerializerOptions()).Object,
                    });
                    break;
                case "pageError":
                    PageError?.Invoke(this, serverParams?.GetProperty("error").GetProperty("error").ToObject<PageErrorEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "fileChooser":
                    FileChooser?.Invoke(this, serverParams?.ToObject<FileChooserChannelEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "frameAttached":
                    FrameAttached?.Invoke(this, new FrameEventArgs(serverParams?.GetProperty("frame").ToObject<FrameChannel>(Connection.GetDefaultJsonSerializerOptions()).Object));
                    break;
                case "frameDetached":
                    FrameDetached?.Invoke(this, new FrameEventArgs(serverParams?.GetProperty("frame").ToObject<FrameChannel>(Connection.GetDefaultJsonSerializerOptions()).Object));
                    break;
                case "dialog":
                    Dialog?.Invoke(this, new DialogEventArgs(serverParams?.GetProperty("dialog").ToObject<DialogChannel>(Connection.GetDefaultJsonSerializerOptions()).Object));
                    break;
                case "console":
                    Console?.Invoke(this, new ConsoleEventArgs(serverParams?.GetProperty("message").ToObject<ConsoleMessage>(Connection.GetDefaultJsonSerializerOptions())));
                    break;
                case "request":
                    Request?.Invoke(this, new RequestEventArgs { Request = serverParams?.GetProperty("request").ToObject<RequestChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "requestFinished":
                    RequestFinished?.Invoke(this, serverParams?.ToObject<PageChannelRequestEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "requestFailed":
                    RequestFailed?.Invoke(this, serverParams?.ToObject<PageChannelRequestEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "response":
                    Response?.Invoke(this, new ResponseEventArgs { Response = serverParams?.GetProperty("response").ToObject<ResponseChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "webSocket":
                    WebSocket?.Invoke(this, new WebSocketEventArgs { WebSocket = serverParams?.GetProperty("webSocket").ToObject<WebSocketChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "download":
                    Download?.Invoke(this, new DownloadEventArgs() { Download = serverParams?.GetProperty("download").ToObject<DownloadChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "video":
                    Video?.Invoke(this, new VideoEventArgs() { RelativePath = serverParams?.GetProperty("relativePath").ToString() });
                    break;
                case "worker":
                    Worker?.Invoke(
                        this,
                        new WorkerChannelEventArgs
                        {
                            WorkerChannel = serverParams?.GetProperty("worker").ToObject<WorkerChannel>(Connection.GetDefaultJsonSerializerOptions()),
                        });
                    break;
            }
        }

        internal Task SetDefaultTimeoutNoReplyAsync(int timeout)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetDefaultNavigationTimeoutNoReplyAsync(int timeout)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setDefaultNavigationTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetFileChooserInterceptedNoReplyAsync(bool intercepted)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setFileChooserInterceptedNoReply",
                new Dictionary<string, object>
                {
                    ["intercepted"] = intercepted,
                });

        internal Task CloseAsync(bool runBeforeUnload)
            => Connection.SendMessageToServer(
                Guid,
                "close",
                new Dictionary<string, object>
                {
                    ["runBeforeUnload"] = runBeforeUnload,
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

        internal Task BringToFrontAsync() => Connection.SendMessageToServer(Guid, "bringToFront");

        internal Task<ResponseChannel> GoBackAsync(int? timeout, LifecycleEvent? waitUntil)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Connection.SendMessageToServer<ResponseChannel>(Guid, "goBack", args);
        }

        internal Task<ResponseChannel> GoForwardAsync(int? timeout, LifecycleEvent? waitUntil)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Connection.SendMessageToServer<ResponseChannel>(Guid, "goForward", args);
        }

        internal Task<ResponseChannel> ReloadAsync(int? timeout, LifecycleEvent? waitUntil)
        {
            var args = new Dictionary<string, object>();

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (waitUntil != null)
            {
                args["waitUntil"] = waitUntil;
            }

            return Connection.SendMessageToServer<ResponseChannel>(Guid, "reload", args);
        }

        internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
            => Connection.SendMessageToServer<PageChannel>(
                Guid,
                "setNetworkInterceptionEnabled",
                new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                });

        internal Task<PageChannel> GetOpenerAsync() => Connection.SendMessageToServer<PageChannel>(Guid, "opener", null);

        internal async Task<SerializedAXNode> AccessibilitySnapshotAsync(bool? interestingOnly, IChannel<ElementHandle> root)
        {
            var args = new Dictionary<string, object>
            {
                ["interestingOnly"] = interestingOnly ?? true,
            };

            if (root != null)
            {
                args["root"] = root;
            }

            if ((await Connection.SendMessageToServer(Guid, "accessibilitySnapshot", args).ConfigureAwait(false)).Value.TryGetProperty("rootAXNode", out var jsonElement))
            {
                return jsonElement.ToObject<SerializedAXNode>(Connection.GetDefaultJsonSerializerOptions());
            }

            return null;
        }

        internal Task SetViewportSizeAsync(ViewportSize viewport)
            => Connection.SendMessageToServer(
                Guid,
                "setViewportSize",
                new Dictionary<string, object>
                {
                    ["viewportSize"] = viewport,
                });

        internal Task KeyboardDownAsync(string key)
            => Connection.SendMessageToServer(
                Guid,
                "keyboardDown",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task EmulateMediaAsync(Dictionary<string, object> args)
            => Connection.SendMessageToServer(Guid, "emulateMedia", args);

        internal Task KeyboardUpAsync(string key)
            => Connection.SendMessageToServer(
                Guid,
                "keyboardUp",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task TypeAsync(string text, int delay)
            => Connection.SendMessageToServer(
                Guid,
                "keyboardType",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                    ["delay"] = delay,
                });

        internal Task PressAsync(string key, int delay)
            => Connection.SendMessageToServer(
                Guid,
                "keyboardPress",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["delay"] = delay,
                });

        internal Task InsertTextAsync(string text)
            => Connection.SendMessageToServer(
                Guid,
                "keyboardInsertText",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                });

        internal Task MouseDownAsync(MouseButton button, int clickCount)
            => Connection.SendMessageToServer(
                Guid,
                "mouseDown",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseMoveAsync(decimal x, decimal y, int? steps)
            => Connection.SendMessageToServer(
                Guid,
                "mouseMove",
                new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["steps"] = steps,
                });

        internal Task MouseUpAsync(MouseButton button, int clickCount)
            => Connection.SendMessageToServer(
                Guid,
                "mouseUp",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseClickAsync(decimal x, decimal y, int delay, MouseButton button, int clickCount)
            => Connection.SendMessageToServer(
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
            => Connection.SendMessageToServer(
                Guid,
                "setExtraHTTPHeaders",
                new Dictionary<string, object>
                {
                    ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
                });

        internal async Task<string> ScreenshotAsync(
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
                ["fullPage"] = fullPage,
                ["omitBackground"] = omitBackground,
            };

            if (clip != null)
            {
                args["clip"] = clip;
            }

            if (path != null)
            {
                args["path"] = path;
            }

            if (type != null)
            {
                args["type"] = type;
            }

            if (timeout != null)
            {
                args["timeout"] = timeout;
            }

            if (quality != null)
            {
                args["quality"] = quality;
            }

            return (await Connection.SendMessageToServer(Guid, "screenshot", args).ConfigureAwait(false))?.GetProperty("binary").ToString();
        }

        internal Task StartCSSCoverageAsync(bool resetOnNavigation)
            => Connection.SendMessageToServer(
                Guid,
                "crStartCSSCoverage",
                new Dictionary<string, object>
                {
                    ["resetOnNavigation"] = resetOnNavigation,
                });

        internal Task StartJSCoverageAsync(bool resetOnNavigation, bool reportAnonymousScripts)
            => Connection.SendMessageToServer(
                Guid,
                "crStartJSCoverage",
                new Dictionary<string, object>
                {
                    ["resetOnNavigation"] = resetOnNavigation,
                    ["reportAnonymousScripts"] = reportAnonymousScripts,
                });

        internal async Task<CSSCoverageEntry[]> StopCSSCoverageAsync()
            => (await Connection.SendMessageToServer(Guid, "crStopCSSCoverage", null).ConfigureAwait(false))?.GetProperty("entries").ToObject<CSSCoverageEntry[]>();

        internal async Task<JSCoverageEntry[]> StopJSCoverageAsync()
            => (await Connection.SendMessageToServer(Guid, "crStopJSCoverage", null).ConfigureAwait(false))?.GetProperty("entries").ToObject<JSCoverageEntry[]>();

        internal async Task<string> GetPdfAsync(
            decimal scale,
            bool displayHeaderFooter,
            string headerTemplate,
            string footerTemplate,
            bool printBackground,
            bool landscape,
            string pageRanges,
            PaperFormat? format,
            string width,
            string height,
            Margin margin,
            bool preferCSSPageSize)
        {
            var args = new Dictionary<string, object>
            {
                ["scale"] = scale,
                ["displayHeaderFooter"] = displayHeaderFooter,
                ["printBackground"] = printBackground,
                ["landscape"] = landscape,
                ["preferCSSPageSize"] = preferCSSPageSize,
            };

            if (pageRanges != null)
            {
                args["pageRanges"] = pageRanges;
            }

            if (headerTemplate != null)
            {
                args["headerTemplate"] = headerTemplate;
            }

            if (footerTemplate != null)
            {
                args["footerTemplate"] = footerTemplate;
            }

            if (margin != null)
            {
                args["margin"] = margin;
            }

            if (!string.IsNullOrEmpty(width))
            {
                args["width"] = width;
            }

            if (format != null)
            {
                args["format"] = format;
            }

            if (!string.IsNullOrEmpty(height))
            {
                args["height"] = height;
            }

            return (await Connection.SendMessageToServer(Guid, "pdf", args).ConfigureAwait(false))?.GetProperty("pdf").ToString();
        }
    }
}
