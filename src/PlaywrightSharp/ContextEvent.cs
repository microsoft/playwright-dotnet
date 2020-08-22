namespace PlaywrightSharp
{
    /// <summary>
    /// Context events. See <see cref="IBrowserContext.WaitForEvent{T}(ContextEvent, System.Func{T, bool}, int?)"/>.
    /// </summary>
    public enum ContextEvent
    {
        /// <summary>
        /// <see cref="IBrowserContext.Page"/>.
        /// </summary>
        Page,

        /// <summary>
        /// <see cref="IBrowserContext.Closed"/>.
        /// </summary>
        Closed,

        /// <summary>
        /// <see cref="IBrowserContext.BackgroundPage"/>.
        /// </summary>
        BackgroundPage,

        /// <summary>
        /// <see cref="IBrowserContext.ServiceWorker"/>.
        /// </summary>
        ServiceWorker,
    }
}
