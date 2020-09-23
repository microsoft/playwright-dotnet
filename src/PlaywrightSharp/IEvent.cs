using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Events for <see cref="IPage.WaitForEvent{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/> and <see cref="IBrowserContext.WaitForEvent{T}(PlaywrightEvent{T}, Func{T, bool}, int?)"/>.
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Event Name.
        /// </summary>
        string Name { get; }
    }
}
