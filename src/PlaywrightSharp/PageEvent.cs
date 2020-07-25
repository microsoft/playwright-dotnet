namespace PlaywrightSharp
{
    /// <summary>
    /// Enums for <see cref="IPage.WaitForEvent{T}(PageEvent, WaitForEventOptions{T})"/>.
    /// </summary>
    public enum PageEvent
    {
        /// <summary>
        /// Close event.
        /// </summary>
        /// <see cref="IPage.Closed"/>
        Closed,

        /// <summary>
        /// Load event
        /// </summary>
        /// <see cref="IPage.Load"/>
        Load,

        /// <summary>
        /// The JavaScript <c>DOMContentLoaded</c> <see href="https://developer.mozilla.org/en-US/docs/Web/Events/DOMContentLoaded"/> event
        /// </summary>
        DOMContentLoaded,

        /// <summary>
        /// Console event
        /// </summary>
        /// <see cref="IPage.Console"/>
        Console,

        /// <summary>
        /// Popup event
        /// </summary>
        /// <see cref="IPage.Popup"/>
        Popup,

        /// <summary>
        /// Dialog event
        /// </summary>
        /// <see cref="IPage.Dialog"/>
        Dialog,

        /// <summary>
        /// Request event
        /// </summary>
        /// <see cref="IPage.Request"/>
        Request,

        /// <summary>
        /// FileChooser event.
        /// </summary>
        /// <see cref="IPage.FileChooser"/>
        FileChooser,

        /// <summary>
        /// Response event
        /// </summary>
        /// <see cref="IPage.Response"/>
        Response,

        /// <summary>
        /// Page error event
        /// </summary>
        /// <see cref="IPage.PageError"/>
        PageError,

        /// <summary>
        /// Page WorkerCreated event
        /// </summary>
        /// <see cref="IPage.WorkerCreated"/>
        WorkerCreated,

        /// <summary>
        /// Page Crashed event.
        /// </summary>
        /// <see cref="IPage.Crashed"/>
        Crashed,

        /// <summary>
        /// Frame navigated event.
        /// </summary>
        /// <see cref="IPage.FrameNavigated"/>
        FrameNavigated,
    }
}
