namespace PlaywrightSharp.Transport.Channel
{
    /// <summary>
    /// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
    /// </summary>
    internal interface IChannelOwner
    {
        /// <summary>
        /// Scope.
        /// </summary>
        ConnectionScope Scope { get; }

        /// <summary>
        /// Channel.
        /// </summary>
        Channel Channel { get; }
    }
}
