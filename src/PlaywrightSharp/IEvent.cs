using System;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Events for <see cref="IPage.WaitForEventAsync(string, float?)"/>/>
    /// and <see cref="IBrowserContext.WaitForEventAsync(string, float?)"/>.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Event Name.
        /// </summary>
        string Name { get; }
    }
}
