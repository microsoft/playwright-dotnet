using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="IBrowserContext.Page"/>.
    /// </summary>
    public class PageEventArgs : EventArgs
    {
        /// <summary>
        /// Page created.
        /// </summary>
        public IPage Page { get; internal set; }
    }
}
