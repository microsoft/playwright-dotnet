using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="Frame.LoadState"/>.
    /// </summary>
    public class LoadStateEventArgs : EventArgs
    {
        /// <summary>
        /// Load state being added.
        /// </summary>
        public LifecycleEvent LifecycleEvent { get; set; }
    }
}
