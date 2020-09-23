using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Page events for <see cref="IPage.WaitForEvent{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    public static class PageEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Request"/>.
        /// </summary>
        public static PlaywrightEvent<RequestEventArgs> Request => new PlaywrightEvent<RequestEventArgs>() { Name = "Request" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.RequestFinished"/>.
        /// </summary>
        public static PlaywrightEvent<RequestEventArgs> RequestFinished => new PlaywrightEvent<RequestEventArgs>() { Name = "RequestFinished" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Crash"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Crash => new PlaywrightEvent<EventArgs>() { Name = "Crash" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Closed"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Closed => new PlaywrightEvent<EventArgs>() { Name = "Closed" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Response"/>.
        /// </summary>
        public static PlaywrightEvent<ResponseEventArgs> Response => new PlaywrightEvent<ResponseEventArgs>() { Name = "Response" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Download"/>.
        /// </summary>
        public static PlaywrightEvent<DownloadEventArgs> Download => new PlaywrightEvent<DownloadEventArgs>() { Name = "Download" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Console"/>.
        /// </summary>
        public static PlaywrightEvent<ConsoleEventArgs> Console => new PlaywrightEvent<ConsoleEventArgs>() { Name = "Console" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Popup"/>.
        /// </summary>
        public static PlaywrightEvent<PopupEventArgs> Popup => new PlaywrightEvent<PopupEventArgs>() { Name = "Popup" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameNavigated"/>.
        /// </summary>
        public static PlaywrightEvent<FrameEventArgs> FrameNavigated => new PlaywrightEvent<FrameEventArgs>() { Name = "FrameNavigated" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FrameDetached"/>.
        /// </summary>
        public static PlaywrightEvent<FrameEventArgs> FrameDetached => new PlaywrightEvent<FrameEventArgs>() { Name = "FrameDetached" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Worker"/>.
        /// </summary>
        public static PlaywrightEvent<WorkerEventArgs> Worker => new PlaywrightEvent<WorkerEventArgs>() { Name = "Worker" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Dialog"/>.
        /// </summary>
        public static PlaywrightEvent<DialogEventArgs> Dialog => new PlaywrightEvent<DialogEventArgs>() { Name = "Dialog" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.FileChooser"/>.
        /// </summary>
        public static PlaywrightEvent<FileChooserEventArgs> FileChooser => new PlaywrightEvent<FileChooserEventArgs>() { Name = "FileChooser" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.PageError"/>.
        /// </summary>
        public static PlaywrightEvent<PageErrorEventArgs> PageError => new PlaywrightEvent<PageErrorEventArgs>() { Name = "PageError" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.Load"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Load => new PlaywrightEvent<EventArgs>() { Name = "Load" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IPage.DOMContentLoaded"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> DOMContentLoaded => new PlaywrightEvent<EventArgs>() { Name = "DOMContentLoaded" };
    }
}
