using System;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Events for <see cref="IPage.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, float?)"/> and <see cref="IBrowserContext.WaitForEventAsync(string, float?)"/>.
    /// </summary>
    /// <typeparam name="T"><see cref="EventArgs"/> returned by the event.</typeparam>
    public class PlaywrightEvent<T> : IEvent
    {
        /// <inheritdoc/>
        public string Name { get; set; }
    }
}
