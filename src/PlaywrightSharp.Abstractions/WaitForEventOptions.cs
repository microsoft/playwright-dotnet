using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IPage.WaitForEvent{T}(PageEvent, WaitForEventOptions{T})"/>.
    /// </summary>
    /// <typeparam name="T">Predicate input type.</typeparam>
    public class WaitForEventOptions<T>
    {
        /// <summary>
        /// Gets or sets the predicate to use when waiting for events.
        /// </summary>
        public Func<T, bool> Predicate { get; set; }

        /// <summary>
        /// Gets or sets the timeout to use when waiting for events in milliseconds.
        /// </summary>
        public int Timeout { get; set; }
    }
}
