using System;
using PlaywrightSharp.Chromium;

namespace PlaywrightSharp
{
    /// <summary>
    /// Context events. See <see cref="IBrowserContext.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    public static class ContextEvent
    {
        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.Page"/>.
        /// </summary>
        public static PlaywrightEvent<PageEventArgs> Page { get; } = new PlaywrightEvent<PageEventArgs>() { Name = "Page" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.Close"/>.
        /// </summary>
        public static PlaywrightEvent<EventArgs> Close { get; } = new PlaywrightEvent<EventArgs>() { Name = "Close" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IChromiumBrowserContext.BackgroundPage"/>.
        /// </summary>
        public static PlaywrightEvent<PageEventArgs> BackgroundPage { get; } = new PlaywrightEvent<PageEventArgs>() { Name = "BackgroundPage" };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IChromiumBrowserContext.ServiceWorker"/>.
        /// </summary>
        public static PlaywrightEvent<WorkerEventArgs> ServiceWorker { get; } = new PlaywrightEvent<WorkerEventArgs>() { Name = "ServiceWorker" };
    }
}
