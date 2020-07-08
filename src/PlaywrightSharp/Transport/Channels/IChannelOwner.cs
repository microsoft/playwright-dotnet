namespace PlaywrightSharp.Transport.Channels
{
    /// <summary>
    /// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
    /// </summary>
    /// <typeparam name="T">Channel Owner implementation.</typeparam>
    internal interface IChannelOwner
    {
        /// <summary>
        /// Scope.
        /// </summary>
        ConnectionScope Scope { get; }

        /// <summary>
        /// Channel.
        /// </summary>
        ChannelBase Channel { get; }
    }

    /// <summary>
    /// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
    /// </summary>
    /// <typeparam name="T">Channel Owner implementation.</typeparam>
    internal interface IChannelOwner<T> : IChannelOwner
        where T : IChannelOwner<T>
    {
        /// <summary>
        /// Channel.
        /// </summary>
        new Channel<T> Channel { get; }
    }
}
