using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Events for <see cref="IPage.WaitForEvent{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/> and <see cref="IBrowserContext.WaitForEvent{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    /// <typeparam name="T"><see cref="EventArgs"/> returned by the event.</typeparam>
    public class PlaywrightEvent<T> : IEvent
        where T : EventArgs
    {
        /// <inheritdoc/>
        public string Name { get; set; }
    }
}
