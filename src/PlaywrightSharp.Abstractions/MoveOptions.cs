﻿namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IMouse.MoveAsync(decimal, decimal, MoveOptions)"/>.
    /// </summary>
    public class MoveOptions
    {
        /// <summary>
        /// Sends intermediate <c>mousemove</c> events. Defaults to 1.
        /// </summary>
        public int Steps { get; set; } = 1;
    }
}