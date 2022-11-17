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

namespace Microsoft.Playwright.Transport.Channels;

internal class PageChannel : Channel<Page>
{
    public PageChannel(string guid, Connection connection, Page owner) : base(guid, connection, owner)
    {
    }

    internal event EventHandler Closed;

    internal event EventHandler Crashed;

    internal event EventHandler<IWebSocket> WebSocket;

    internal event EventHandler<Page> Popup;

    internal event EventHandler<BindingCall> BindingCall;

    internal event EventHandler<Route> Route;

    internal event EventHandler<IFrame> FrameAttached;

    internal event EventHandler<IFrame> FrameDetached;

    internal event EventHandler<IDialog> Dialog;

    internal event EventHandler<IConsoleMessage> Console;

    internal event EventHandler<PageDownloadEvent> Download;

    internal event EventHandler<SerializedError> PageError;

    internal event EventHandler<FileChooserChannelEventArgs> FileChooser;

    internal event EventHandler<Worker> Worker;

    internal event EventHandler<Artifact> Video;

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
                    serverParams?.GetProperty("binding").ToObject<BindingCallChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "route":
                var route = serverParams?.GetProperty("route").ToObject<RouteChannel>(Connection.DefaultJsonSerializerOptions).Object;
                Route?.Invoke(this, route);
                break;
            case "popup":
                Popup?.Invoke(this, serverParams?.GetProperty("page").ToObject<PageChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "pageError":
                PageError?.Invoke(this, serverParams?.GetProperty("error").ToObject<SerializedError>(Connection.DefaultJsonSerializerOptions));
                break;
            case "fileChooser":
                FileChooser?.Invoke(this, serverParams?.ToObject<FileChooserChannelEventArgs>(Connection.DefaultJsonSerializerOptions));
                break;
            case "frameAttached":
                FrameAttached?.Invoke(this, serverParams?.GetProperty("frame").ToObject<FrameChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "frameDetached":
                FrameDetached?.Invoke(this, serverParams?.GetProperty("frame").ToObject<FrameChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "dialog":
                Dialog?.Invoke(this, serverParams?.GetProperty("dialog").ToObject<DialogChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "console":
                Console?.Invoke(this, serverParams?.GetProperty("message").ToObject<ConsoleMessage>(Connection.DefaultJsonSerializerOptions));
                break;
            case "webSocket":
                WebSocket?.Invoke(this, serverParams?.GetProperty("webSocket").ToObject<WebSocketChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "download":
                Download?.Invoke(this, serverParams?.ToObject<PageDownloadEvent>(Connection.DefaultJsonSerializerOptions));
                break;
            case "video":
                Video?.Invoke(this, serverParams?.GetProperty("artifact").ToObject<ArtifactChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
            case "worker":
                Worker?.Invoke(
                    this,
                    serverParams?.GetProperty("worker").ToObject<WorkerChannel>(Connection.DefaultJsonSerializerOptions).Object);
                break;
        }
    }

    internal Task SetDefaultTimeoutNoReplyAsync(float timeout)
        => Object.SendMessageToServerAsync<PageChannel>(
            "setDefaultTimeoutNoReply",
            new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            });

    internal Task SetDefaultNavigationTimeoutNoReplyAsync(float timeout)
        => Object.SendMessageToServerAsync<PageChannel>(
            "setDefaultNavigationTimeoutNoReply",
            new Dictionary<string, object>
            {
                ["timeout"] = timeout,
            });

    internal Task CloseAsync(bool runBeforeUnload)
        => Object.SendMessageToServerAsync(
            "close",
            new Dictionary<string, object>
            {
                ["runBeforeUnload"] = runBeforeUnload,
            });

    internal Task ExposeBindingAsync(string name, bool needsHandle)
        => Object.SendMessageToServerAsync<PageChannel>(
            "exposeBinding",
            new Dictionary<string, object>
            {
                ["name"] = name,
                ["needsHandle"] = needsHandle,
            });

    internal Task AddInitScriptAsync(string script)
        => Object.SendMessageToServerAsync<PageChannel>(
            "addInitScript",
            new Dictionary<string, object>
            {
                ["source"] = script,
            });

    internal Task BringToFrontAsync() => Object.SendMessageToServerAsync("bringToFront");

    internal Task<ResponseChannel> GoBackAsync(float? timeout, WaitUntilState? waitUntil)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
            ["waitUntil"] = waitUntil,
        };
        return Object.SendMessageToServerAsync<ResponseChannel>("goBack", args);
    }

    internal Task<ResponseChannel> GoForwardAsync(float? timeout, WaitUntilState? waitUntil)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
            ["waitUntil"] = waitUntil,
        };
        return Object.SendMessageToServerAsync<ResponseChannel>("goForward", args);
    }

    internal Task<ResponseChannel> ReloadAsync(float? timeout, WaitUntilState? waitUntil)
    {
        var args = new Dictionary<string, object>
        {
            ["timeout"] = timeout,
            ["waitUntil"] = waitUntil,
        };
        return Object.SendMessageToServerAsync<ResponseChannel>("reload", args);
    }

    internal Task SetNetworkInterceptionEnabledAsync(bool enabled)
        => Object.SendMessageToServerAsync<PageChannel>(
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

        if ((await Object.SendMessageToServerAsync("accessibilitySnapshot", args).ConfigureAwait(false)).Value.TryGetProperty("rootAXNode", out var jsonElement))
        {
            return jsonElement;
        }

        return null;
    }

    internal Task SetViewportSizeAsync(PageViewportSizeResult viewport)
        => Object.SendMessageToServerAsync(
            "setViewportSize",
            new Dictionary<string, object>
            {
                ["viewportSize"] = viewport,
            });

    internal Task KeyboardDownAsync(string key)
        => Object.SendMessageToServerAsync(
            "keyboardDown",
            new Dictionary<string, object>
            {
                ["key"] = key,
            });

    internal Task EmulateMediaAsync(Dictionary<string, object> args)
        => Object.SendMessageToServerAsync("emulateMedia", args);

    internal Task KeyboardUpAsync(string key)
        => Object.SendMessageToServerAsync(
            "keyboardUp",
            new Dictionary<string, object>
            {
                ["key"] = key,
            });

    internal Task TypeAsync(string text, float? delay)
        => Object.SendMessageToServerAsync(
            "keyboardType",
            new Dictionary<string, object>
            {
                ["text"] = text,
                ["delay"] = delay,
            });

    internal Task PressAsync(string key, float? delay)
        => Object.SendMessageToServerAsync(
            "keyboardPress",
            new Dictionary<string, object>
            {
                ["key"] = key,
                ["delay"] = delay,
            });

    internal Task InsertTextAsync(string text)
        => Object.SendMessageToServerAsync(
            "keyboardInsertText",
            new Dictionary<string, object>
            {
                ["text"] = text,
            });

    internal Task MouseDownAsync(MouseButton? button, int? clickCount)
        => Object.SendMessageToServerAsync(
            "mouseDown",
            new Dictionary<string, object>
            {
                ["button"] = button,
                ["clickCount"] = clickCount,
            });

    internal Task MouseMoveAsync(float x, float y, int? steps)
        => Object.SendMessageToServerAsync(
            "mouseMove",
            new Dictionary<string, object>
            {
                ["x"] = x,
                ["y"] = y,
                ["steps"] = steps,
            });

    internal Task MouseUpAsync(MouseButton? button, int? clickCount)
        => Object.SendMessageToServerAsync(
            "mouseUp",
            new Dictionary<string, object>
            {
                ["button"] = button,
                ["clickCount"] = clickCount,
            });

    internal Task MouseClickAsync(float x, float y, float? delay, MouseButton? button, int? clickCount)
        => Object.SendMessageToServerAsync(
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
        => Object.SendMessageToServerAsync(
            "mouseWheel",
            new Dictionary<string, object>
            {
                ["deltaX"] = deltaX,
                ["deltaY"] = deltaY,
            });

    internal Task TouchscreenTapAsync(float x, float y)
        => Object.SendMessageToServerAsync(
            "touchscreenTap",
            new Dictionary<string, object>
            {
                ["x"] = x,
                ["y"] = y,
            });

    internal Task SetExtraHTTPHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers)
        => Object.SendMessageToServerAsync(
            "setExtraHTTPHeaders",
            new Dictionary<string, object>
            {
                ["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }),
            });

    internal async Task<byte[]> ScreenshotAsync(
        string path,
        bool? fullPage,
        Clip clip,
        bool? omitBackground,
        ScreenshotType? type,
        int? quality,
        IEnumerable<ILocator> mask,
        ScreenshotAnimations? animations,
        ScreenshotCaret? caret,
        ScreenshotScale? scale,
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
            ["animations"] = animations,
            ["caret"] = caret,
            ["scale"] = scale,
            ["quality"] = quality,
            ["mask"] = mask?.Select(locator => new Dictionary<string, object>
            {
                ["frame"] = ((Locator)locator)._frame._channel,
                ["selector"] = ((Locator)locator)._selector,
            }).ToArray(),
        };
        return (await Object.SendMessageToServerAsync("screenshot", args).ConfigureAwait(false))?.GetProperty("binary").GetBytesFromBase64();
    }

    internal Task StartCSSCoverageAsync(bool resetOnNavigation)
        => Object.SendMessageToServerAsync(
            "crStartCSSCoverage",
            new Dictionary<string, object>
            {
                ["resetOnNavigation"] = resetOnNavigation,
            });

    internal Task StartJSCoverageAsync(bool resetOnNavigation, bool reportAnonymousScripts)
        => Object.SendMessageToServerAsync(
            "crStartJSCoverage",
            new Dictionary<string, object>
            {
                ["resetOnNavigation"] = resetOnNavigation,
                ["reportAnonymousScripts"] = reportAnonymousScripts,
            });

    internal async Task<byte[]> PdfAsync(
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
        return (await Object.SendMessageToServerAsync("pdf", args).ConfigureAwait(false))?.GetProperty("pdf").GetBytesFromBase64();
    }
}
