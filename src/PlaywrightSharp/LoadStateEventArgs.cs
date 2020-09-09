namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="Frame.LoadState"/>.
    /// </summary>
    public class LoadStateEventArgs
    {
        /// <summary>
        /// Load state being added.
        /// </summary>
        public LifecycleEvent LifecycleEvent { get; set; }
    }
}
