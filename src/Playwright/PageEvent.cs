using System;
using System.Collections.Generic;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Page events for <see cref="IPage.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, float?)"/>.
    /// </summary>
    public static class PageEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Request"/>.
        /// </summary>
        public static PlaywrightEvent<IRequest> Request { get; } = new PlaywrightEvent<IRequest>() { Name = "Request" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.RequestFinished"/>.
        /// </summary>
        public static PlaywrightEvent<IRequest> RequestFinished { get; } = new PlaywrightEvent<IRequest>() { Name = "RequestFinished" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Crash"/>.
        /// </summary>
        public static PlaywrightEvent<IPage> Crash { get; } = new PlaywrightEvent<IPage>() { Name = "Crash" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Close"/>.
        /// </summary>
        public static PlaywrightEvent<IPage> Close { get; } = new PlaywrightEvent<IPage>() { Name = "Close" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Response"/>.
        /// </summary>
        public static PlaywrightEvent<IResponse> Response { get; } = new PlaywrightEvent<IResponse>() { Name = "Response" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Download"/>.
        /// </summary>
        public static PlaywrightEvent<IDownload> Download { get; } = new PlaywrightEvent<IDownload>() { Name = "Download" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Console"/>.
        /// </summary>
        public static PlaywrightEvent<IConsoleMessage> Console { get; } = new PlaywrightEvent<IConsoleMessage>() { Name = "Console" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Popup"/>.
        /// </summary>
        public static PlaywrightEvent<IPage> Popup { get; } = new PlaywrightEvent<IPage>() { Name = "Popup" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameNavigated"/>.
        /// </summary>
        public static PlaywrightEvent<IFrame> FrameNavigated { get; } = new PlaywrightEvent<IFrame>() { Name = "FrameNavigated" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameDetached"/>.
        /// </summary>
        public static PlaywrightEvent<IFrame> FrameDetached { get; } = new PlaywrightEvent<IFrame>() { Name = "FrameDetached" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Worker"/>.
        /// </summary>
        public static PlaywrightEvent<IWorker> Worker { get; } = new PlaywrightEvent<IWorker>() { Name = "Worker" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Dialog"/>.
        /// </summary>
        public static PlaywrightEvent<IDialog> Dialog { get; } = new PlaywrightEvent<IDialog>() { Name = "Dialog" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FileChooser"/>.
        /// </summary>
        public static PlaywrightEvent<IFileChooser> FileChooser { get; } = new PlaywrightEvent<IFileChooser>() { Name = "FileChooser" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.PageError"/>.
        /// </summary>
        public static PlaywrightEvent<string> PageError { get; } = new PlaywrightEvent<string>() { Name = "PageError" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Load"/>.
        /// </summary>
        public static PlaywrightEvent<IPage> Load { get; } = new PlaywrightEvent<IPage>() { Name = "Load" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.DOMContentLoaded"/>.
        /// </summary>
        public static PlaywrightEvent<IPage> DOMContentLoaded { get; } = new PlaywrightEvent<IPage>() { Name = "DOMContentLoaded" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.WebSocket"/>.
        /// </summary>
        public static PlaywrightEvent<IWebSocket> WebSocket { get; } = new PlaywrightEvent<IWebSocket>() { Name = "WebSocket" };
    }
}
