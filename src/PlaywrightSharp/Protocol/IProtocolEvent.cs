namespace PlaywrightSharp.Protocol
{
    /// <summary>
    /// Base type for protocol events.
    /// </summary>
    internal interface IProtocolEvent
    {
        /// <summary>
        /// The event name raised by the devtools protocol.
        /// </summary>
        string InternalName { get; }
    }
}
