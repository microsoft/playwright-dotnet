using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Context events. See <see cref="IBrowserContext.WaitForEvent{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    public static class ContextEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.Page"/>.
        /// </summary>
        public static PlaywrightEvent<PageEventArgs> Page { get; } = new PlaywrightEvent<PageEventArgs>() { Name = "Page" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.Closed"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Closed { get; } = new PlaywrightEvent<EventArgs>() { Name = "Closed" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.BackgroundPage"/>.
        /// </summary>
        public static PlaywrightEvent<PageEventArgs> BackgroundPage { get; } = new PlaywrightEvent<PageEventArgs>() { Name = "BackgroundPage" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.ServiceWorker"/>.
        /// </summary>
        public static PlaywrightEvent<WorkerEventArgs> ServiceWorker { get; } = new PlaywrightEvent<WorkerEventArgs>() { Name = "ServiceWorker" };
    }
}
