namespace PlaywrightSharp
{
    /// <summary>
    /// Enums for <see cref="IPage.WaitForEvent{T}(PageEvent, WaitForEventOptions)"/>.
    /// </summary>
    public enum PageEvent
    {
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
        /// Response event
        /// </summary>
        /// <see cref="IPage.Response"/>
        Response,

        /// <summary>
        /// Error event
        /// </summary>
        /// <see cref="IPage.Error"/>
        Error,
    }
}
