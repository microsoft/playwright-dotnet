namespace PlaywrightSharp
{
    /// <summary>
    /// Context events. See <see cref="IBrowserContext.WaitForEvent{T}(ContextEvent, System.Func{T, bool}, int?)"/>.
    /// </summary>
    public enum ContextEvent
    {
        /// <summary>
        /// Page created event.
        /// </summary>
        PageCreated,

        /// <summary>
        /// Context closed event.
        /// </summary>
        Closed,
    }
}
