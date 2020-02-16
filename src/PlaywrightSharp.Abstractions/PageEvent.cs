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
        /// Error event
        /// </summary>
        /// <see cref="IPage.Error"/>
        Error,
    }
}
