namespace PlaywrightSharp.Protocol
{
    /// <summary>
    /// Base type for protocol request type.
    /// </summary>
    /// <typeparam name="TProtocolResponse">The protocol response type for a specific request.</typeparam>
    internal interface IProtocolRequest<out TProtocolResponse>
        where TProtocolResponse : IProtocolResponse
    {
        /// <summary>
        /// The protocol command name.
        /// </summary>
        string Command { get; }
    }
}
