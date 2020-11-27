using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Page events for <see cref="IPage.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    public static class PageEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Request"/>.
        /// </summary>
        public static PlaywrightEvent<RequestEventArgs> Request { get; } = new PlaywrightEvent<RequestEventArgs>() { Name = "Request" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.RequestFinished"/>.
        /// </summary>
        public static PlaywrightEvent<RequestEventArgs> RequestFinished { get; } = new PlaywrightEvent<RequestEventArgs>() { Name = "RequestFinished" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Crash"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Crash { get; } = new PlaywrightEvent<EventArgs>() { Name = "Crash" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Close"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Close { get; } = new PlaywrightEvent<EventArgs>() { Name = "Close" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Response"/>.
        /// </summary>
        public static PlaywrightEvent<ResponseEventArgs> Response { get; } = new PlaywrightEvent<ResponseEventArgs>() { Name = "Response" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Download"/>.
        /// </summary>
        public static PlaywrightEvent<DownloadEventArgs> Download { get; } = new PlaywrightEvent<DownloadEventArgs>() { Name = "Download" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Console"/>.
        /// </summary>
        public static PlaywrightEvent<ConsoleEventArgs> Console { get; } = new PlaywrightEvent<ConsoleEventArgs>() { Name = "Console" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Popup"/>.
        /// </summary>
        public static PlaywrightEvent<PopupEventArgs> Popup { get; } = new PlaywrightEvent<PopupEventArgs>() { Name = "Popup" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameNavigated"/>.
        /// </summary>
        public static PlaywrightEvent<FrameEventArgs> FrameNavigated { get; } = new PlaywrightEvent<FrameEventArgs>() { Name = "FrameNavigated" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameDetached"/>.
        /// </summary>
        public static PlaywrightEvent<FrameEventArgs> FrameDetached { get; } = new PlaywrightEvent<FrameEventArgs>() { Name = "FrameDetached" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Worker"/>.
        /// </summary>
        public static PlaywrightEvent<WorkerEventArgs> Worker { get; } = new PlaywrightEvent<WorkerEventArgs>() { Name = "Worker" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Dialog"/>.
        /// </summary>
        public static PlaywrightEvent<DialogEventArgs> Dialog { get; } = new PlaywrightEvent<DialogEventArgs>() { Name = "Dialog" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FileChooser"/>.
        /// </summary>
        public static PlaywrightEvent<FileChooserEventArgs> FileChooser { get; } = new PlaywrightEvent<FileChooserEventArgs>() { Name = "FileChooser" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.PageError"/>.
        /// </summary>
        public static PlaywrightEvent<PageErrorEventArgs> PageError { get; } = new PlaywrightEvent<PageErrorEventArgs>() { Name = "PageError" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Load"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Load { get; } = new PlaywrightEvent<EventArgs>() { Name = "Load" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.DOMContentLoaded"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> DOMContentLoaded { get; } = new PlaywrightEvent<EventArgs>() { Name = "DOMContentLoaded" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.WebSocket"/>.
        /// </summary>
        public static PlaywrightEvent<WebSocketEventArgs> WebSocket { get; } = new PlaywrightEvent<WebSocketEventArgs>() { Name = "WebSocket" };
    }
}
