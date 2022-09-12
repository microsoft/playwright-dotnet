/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

namespace Microsoft.Playwright.Core;

internal static class PageEvent
{
    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Request"/>.
    /// </summary>
    public static PlaywrightEvent<IRequest> Request { get; } = new() { Name = "Request" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.RequestFinished"/>.
    /// </summary>
    public static PlaywrightEvent<IRequest> RequestFinished { get; } = new() { Name = "RequestFinished" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Crash"/>.
    /// </summary>
    public static PlaywrightEvent<IPage> Crash { get; } = new() { Name = "Crash" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Close"/>.
    /// </summary>
    public static PlaywrightEvent<IPage> Close { get; } = new() { Name = "Close" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Response"/>.
    /// </summary>
    public static PlaywrightEvent<IResponse> Response { get; } = new() { Name = "Response" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Download"/>.
    /// </summary>
    public static PlaywrightEvent<IDownload> Download { get; } = new() { Name = "Download" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Console"/>.
    /// </summary>
    public static PlaywrightEvent<IConsoleMessage> Console { get; } = new() { Name = "Console" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Popup"/>.
    /// </summary>
    public static PlaywrightEvent<IPage> Popup { get; } = new() { Name = "Popup" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameNavigated"/>.
    /// </summary>
    public static PlaywrightEvent<IFrame> FrameNavigated { get; } = new() { Name = "FrameNavigated" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameDetached"/>.
    /// </summary>
    public static PlaywrightEvent<IFrame> FrameDetached { get; } = new() { Name = "FrameDetached" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Worker"/>.
    /// </summary>
    public static PlaywrightEvent<IWorker> Worker { get; } = new() { Name = "Worker" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Dialog"/>.
    /// </summary>
    public static PlaywrightEvent<IDialog> Dialog { get; } = new() { Name = "Dialog" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FileChooser"/>.
    /// </summary>
    public static PlaywrightEvent<IFileChooser> FileChooser { get; } = new() { Name = "FileChooser" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.PageError"/>.
    /// </summary>
    public static PlaywrightEvent<string> PageError { get; } = new() { Name = "PageError" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Load"/>.
    /// </summary>
    public static PlaywrightEvent<IPage> Load { get; } = new() { Name = "Load" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.DOMContentLoaded"/>.
    /// </summary>
    public static PlaywrightEvent<IPage> DOMContentLoaded { get; } = new() { Name = "DOMContentLoaded" };

    /// <summary>
    /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.WebSocket"/>.
    /// </summary>
    public static PlaywrightEvent<IWebSocket> WebSocket { get; } = new() { Name = "WebSocket" };
}
