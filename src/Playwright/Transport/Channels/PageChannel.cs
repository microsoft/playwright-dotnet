/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
    internal class PageChannel : Channel<Page>
    {
        public PageChannel(string guid, Connection connection, Page owner) : base(guid, connection, owner)
        {
        }

        internal event EventHandler Closed;

        internal event EventHandler Crashed;

        internal event EventHandler<IWebSocket> WebSocket;

        internal event EventHandler DOMContentLoaded;

        internal event EventHandler<PageChannelPopupEventArgs> Popup;

        internal event EventHandler<BindingCallEventArgs> BindingCall;

        internal event EventHandler<RouteEventArgs> Route;

        internal event EventHandler<IFrame> FrameAttached;

        internal event EventHandler<IFrame> FrameDetached;

        internal event EventHandler<IDialog> Dialog;

        internal event EventHandler<IConsoleMessage> Console;

        internal event EventHandler<PageDownloadEvent> Download;

        internal event EventHandler<PageErrorEventArgs> PageError;

        internal event EventHandler<FileChooserChannelEventArgs> FileChooser;

        internal event EventHandler Load;

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
                        new() { BidingCall = serverParams?.GetProperty("binding").ToObject<BindingCallChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "route":
                    var route = serverParams?.GetProperty("route").ToObject<RouteChannel>(Connection.GetDefaultJsonSerializerOptions()).Object;
                    var request = serverParams?.GetProperty("request").ToObject<RequestChannel>(Connection.GetDefaultJsonSerializerOptions()).Object;
                    Route?.Invoke(
                        this,
                        new() { Route = route, Request = request });
                    break;
                case "popup":
                    Popup?.Invoke(this, new() { Page = serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "pageError":
                    PageError?.Invoke(this, serverParams?.GetProperty("error").GetProperty("error").ToObject<PageErrorEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "fileChooser":
                    FileChooser?.Invoke(this, serverParams?.ToObject<FileChooserChannelEventArgs>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "frameAttached":
                    FrameAttached?.Invoke(this, serverParams?.GetProperty("frame").ToObject<FrameChannel>(Connection.GetDefaultJsonSerializerOptions()).Object);
                    break;
                case "frameDetached":
                    FrameDetached?.Invoke(this, serverParams?.GetProperty("frame").ToObject<FrameChannel>(Connection.GetDefaultJsonSerializerOptions()).Object);
                    break;
                case "dialog":
                    var dialog = serverParams?.GetProperty("dialog").ToObject<DialogChannel>(Connection.GetDefaultJsonSerializerOptions()).Object;
                    if ("beforeunload".Equals(dialog.Type, StringComparison.Ordinal))
                    {
                        try
                        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                            dialog.AcceptAsync(null).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                        }
                        catch (PlaywrightException)
                        {
                            // Noop
                        }
                    }
                    else
                    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                        dialog.DismissAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                    }
                    Dialog?.Invoke(this, dialog);
                    break;
                case "console":
                    Console?.Invoke(this, serverParams?.GetProperty("message").ToObject<ConsoleMessage>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "webSocket":
                    WebSocket?.Invoke(this, serverParams?.GetProperty("webSocket").ToObject<WebSocketChannel>(Connection.GetDefaultJsonSerializerOptions()).Object);
                    break;
                case "download":
                    Download?.Invoke(this, serverParams?.ToObject<PageDownloadEvent>(Connection.GetDefaultJsonSerializerOptions()));
                    break;
                case "video":
                    Video?.Invoke(this, new() { Artifact = serverParams?.GetProperty("artifact").ToObject<ArtifactChannel>(Connection.GetDefaultJsonSerializerOptions()).Object });
                    break;
                case "worker":
                    Worker?.Invoke(
                        this,
                        new() { WorkerChannel = serverParams?.GetProperty("worker").ToObject<WorkerChannel>(Connection.GetDefaultJsonSerializerOptions()) });
                    break;
            }
        }

        internal Task SetDefaultTimeoutNoReplyAsync(float timeout)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setDefaultTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetDefaultNavigationTimeoutNoReplyAsync(float timeout)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setDefaultNavigationTimeoutNoReply",
                new Dictionary<string, object>
                {
                    ["timeout"] = timeout,
                });

        internal Task SetFileChooserInterceptedNoReplyAsync(bool intercepted)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setFileChooserInterceptedNoReply",
                new Dictionary<string, object>
                {
                    ["intercepted"] = intercepted,
                });

        internal Task CloseAsync(bool runBeforeUnload)
            => Connection.SendMessageToServerAsync(
                Guid,
                "close",
                new Dictionary<string, object>
                {
                    ["runBeforeUnload"] = runBeforeUnload,
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

        internal Task BringToFrontAsync() => Connection.SendMessageToServerAsync(Guid, "bringToFront");

        internal Task<ResponseChannel> GoBackAsync(float? timeout, WaitUntilState? waitUntil)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = timeout,
                ["waitUntil"] = waitUntil,
            };
            return Connection.SendMessageToServerAsync<ResponseChannel>(Guid, "goBack", args);
        }

        internal Task<ResponseChannel> GoForwardAsync(float? timeout, WaitUntilState? waitUntil)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = timeout,
                ["waitUntil"] = waitUntil,
            };
            return Connection.SendMessageToServerAsync<ResponseChannel>(Guid, "goForward", args);
        }

        internal Task<ResponseChannel> ReloadAsync(float? timeout, WaitUntilState? waitUntil)
        {
            var args = new Dictionary<string, object>
            {
                ["timeout"] = timeout,
                ["waitUntil"] = waitUntil,
            };
            return Connection.SendMessageToServerAsync<ResponseChannel>(Guid, "reload", args);
        }

        internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
            => Connection.SendMessageToServerAsync<PageChannel>(
                Guid,
                "setNetworkInterceptionEnabled",
                new Dictionary<string, object>
                {
                    ["enabled"] = enabled,
                });

        internal async Task<JsonElement?> AccessibilitySnapshotAsync(bool? interestingOnly, IChannel<ElementHandle> root)
        {
            var args = new Dictionary<string, object>
            {
                ["interestingOnly"] = interestingOnly,
                ["root"] = root,
            };

            if ((await Connection.SendMessageToServerAsync(Guid, "accessibilitySnapshot", args).ConfigureAwait(false)).Value.TryGetProperty("rootAXNode", out var jsonElement))
            {
                Connection.GetDefaultJsonSerializerOptions();
                return jsonElement;
            }

            return null;
        }

        internal Task SetViewportSizeAsync(PageViewportSizeResult viewport)
            => Connection.SendMessageToServerAsync(
                Guid,
                "setViewportSize",
                new Dictionary<string, object>
                {
                    ["viewportSize"] = viewport,
                });

        internal Task KeyboardDownAsync(string key)
            => Connection.SendMessageToServerAsync(
                Guid,
                "keyboardDown",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task EmulateMediaAsync(Dictionary<string, object> args)
            => Connection.SendMessageToServerAsync(Guid, "emulateMedia", args);

        internal Task KeyboardUpAsync(string key)
            => Connection.SendMessageToServerAsync(
                Guid,
                "keyboardUp",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                });

        internal Task TypeAsync(string text, float? delay)
            => Connection.SendMessageToServerAsync(
                Guid,
                "keyboardType",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                    ["delay"] = delay,
                });

        internal Task PressAsync(string key, float? delay)
            => Connection.SendMessageToServerAsync(
                Guid,
                "keyboardPress",
                new Dictionary<string, object>
                {
                    ["key"] = key,
                    ["delay"] = delay,
                });

        internal Task InsertTextAsync(string text)
            => Connection.SendMessageToServerAsync(
                Guid,
                "keyboardInsertText",
                new Dictionary<string, object>
                {
                    ["text"] = text,
                });

        internal Task MouseDownAsync(MouseButton? button, int? clickCount)
            => Connection.SendMessageToServerAsync(
                Guid,
                "mouseDown",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseMoveAsync(float x, float y, int? steps)
            => Connection.SendMessageToServerAsync(
                Guid,
                "mouseMove",
                new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                    ["steps"] = steps,
                });

        internal Task MouseUpAsync(MouseButton? button, int? clickCount)
            => Connection.SendMessageToServerAsync(
                Guid,
                "mouseUp",
                new Dictionary<string, object>
                {
                    ["button"] = button,
                    ["clickCount"] = clickCount,
                });

        internal Task MouseClickAsync(float x, float y, float? delay, MouseButton? button, int? clickCount)
            => Connection.SendMessageToServerAsync(
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

        internal Task MouseWheelAsync(float deltaX, float deltaY)
            => Connection.SendMessageToServerAsync(
                Guid,
                "mouseWheel",
                new Dictionary<string, object>
                {
                    ["deltaX"] = deltaX,
                    ["deltaY"] = deltaY,
                });

        internal Task TouchscreenTapAsync(float x, float y)
            => Connection.SendMessageToServerAsync(
                Guid,
                "touchscreenTap",
                new Dictionary<string, object>
                {
                    ["x"] = x,
                    ["y"] = y,
                });

        internal Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
            => Connection.SendMessageToServerAsync(
                Guid,
                "setExtraHTTPHeaders",
                new Dictionary<string, object>
                {
                    ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
                });

        internal async Task<string> ScreenshotAsync(
            string path,
            bool? fullPage,
            Clip clip,
            bool? omitBackground,
            ScreenshotType? type,
            int? quality,
            float? timeout)
        {
            var args = new Dictionary<string, object>
            {
                ["fullPage"] = fullPage,
                ["omitBackground"] = omitBackground,
                ["clip"] = clip,
                ["path"] = path,
                ["type"] = type,
                ["timeout"] = timeout,
                ["quality"] = quality,
            };
            return (await Connection.SendMessageToServerAsync(Guid, "screenshot", args).ConfigureAwait(false))?.GetProperty("binary").ToString();
        }

        internal Task StartCSSCoverageAsync(bool resetOnNavigation)
            => Connection.SendMessageToServerAsync(
                Guid,
                "crStartCSSCoverage",
                new Dictionary<string, object>
                {
                    ["resetOnNavigation"] = resetOnNavigation,
                });

        internal Task StartJSCoverageAsync(bool resetOnNavigation, bool reportAnonymousScripts)
            => Connection.SendMessageToServerAsync(
                Guid,
                "crStartJSCoverage",
                new Dictionary<string, object>
                {
                    ["resetOnNavigation"] = resetOnNavigation,
                    ["reportAnonymousScripts"] = reportAnonymousScripts,
                });

        internal async Task<string> PdfAsync(
            float? scale,
            bool? displayHeaderFooter,
            string headerTemplate,
            string footerTemplate,
            bool? printBackground,
            bool? landscape,
            string pageRanges,
            string format,
            string width,
            string height,
            Margin margin,
            bool? preferCSSPageSize)
        {
            var args = new Dictionary<string, object>
            {
                ["scale"] = scale,
                ["displayHeaderFooter"] = displayHeaderFooter,
                ["printBackground"] = printBackground,
                ["landscape"] = landscape,
                ["preferCSSPageSize"] = preferCSSPageSize,
                ["pageRanges"] = pageRanges,
                ["headerTemplate"] = headerTemplate,
                ["footerTemplate"] = footerTemplate,
                ["margin"] = margin,
                ["width"] = width,
                ["format"] = format,
                ["height"] = height,
            };
            return (await Connection.SendMessageToServerAsync(Guid, "pdf", args).ConfigureAwait(false))?.GetProperty("pdf").ToString();
        }
    }
}
