namespace PlaywrightSharp
{
    /// <summary>
    /// Lyfe cycle event.
    /// </summary>
    public enum LifecycleEvent
    {
        /// <summary>
        /// Consider navigation to be finished when the <c>load</c> event is fired
        /// </summary>
        Load,

        /// <summary>
        /// Consider navigation to be finished when the <c>DOMContentLoaded</c> event is fired
        /// </summary>
        DOMContentLoaded,

        /// <summary>
        /// Consider navigation to be finished when there are no more than 0 network connections for at least <c>500</c> ms
        /// </summary>
        Networkidle,
    }
}
