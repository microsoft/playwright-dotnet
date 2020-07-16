using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="IBrowserContext.PageCreated"/>.
    /// </summary>
    public class PageEventArgs : EventArgs
    {
        /// <summary>
        /// Page created.
        /// </summary>
        public Page Page { get; internal set; }
    }
}