using System;
using PlaywrightSharp.Chromium;

namespace PlaywrightSharp
{
    /// <summary>
    /// Context events. See <see cref="IBrowserContext.WaitForEventAsync{T}(PlaywrightEvent{T}, float?)"/>.
    /// </summary>
    public static class ContextEvent
    {
        internal const string PageEventName = "Page";
        internal const string CloseEventName = "Close";

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.Page"/>.
        /// </summary>
        public static PlaywrightEvent<IPage> Page { get; } = new PlaywrightEvent<IPage>() { Name = PageEventName };

        /// <summary>
        /// <see cref="PlaywrightEvent{T}"/> representing a <see cref="IBrowserContext.Close"/>.
        /// </summary>
        public static PlaywrightEvent<IBrowserContext> Close { get; } = new PlaywrightEvent<IBrowserContext>() { Name = CloseEventName };

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
