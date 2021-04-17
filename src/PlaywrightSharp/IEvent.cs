using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Events for <see cref="IPage.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, float?)"/> and <see cref="IBrowserContext.WaitForEventAsync{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Event Name.
        /// </summary>
        string Name { get; }
    }
}
