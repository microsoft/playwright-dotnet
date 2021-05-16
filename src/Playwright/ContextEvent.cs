using System;

namespace Microsoft.Playwright
{
    public static class ContextEvent
    {
        internal const string PageEventName = "Page";
        internal const string CloseEventName = "Close";

        public static PlaywrightEvent<IPage> Page { get; } = new PlaywrightEvent<IPage>() { Name = PageEventName };

        public static PlaywrightEvent<IBrowserContext> Close { get; } = new PlaywrightEvent<IBrowserContext>() { Name = CloseEventName };
    }
}
