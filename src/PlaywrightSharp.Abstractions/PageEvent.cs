namespace PlaywrightSharp
{
    /// <summary>
    /// Enums for <see cref="IPage.WaitForEvent{T}(PageEvent, WaitForEventOptions)"/>.
    /// </summary>
    public enum PageEvent
    {
        /// <summary>
        /// Console event
        /// </summary>
        /// <see cref="IPage.Console"/>
        Console,

        /// <summary>
        /// Request event
        /// </summary>
        /// <see cref="IPage.Request"/>
        Request,
    }
}
