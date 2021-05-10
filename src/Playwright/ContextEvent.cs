using System;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Context events. See <see cref="IBrowserContext.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, float?)"/>.
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
    }
}
