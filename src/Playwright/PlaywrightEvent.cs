using System;

namespace Microsoft.Playwright
{
    public class PlaywrightEvent<T> : IEvent
    {
        public string Name { get; set; }
    }
}
