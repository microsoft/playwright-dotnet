using System.Collections.Concurrent;
using Microsoft.Playwright.Protocol.Channels;

namespace Microsoft.Playwright.Protocol.Contracts
{
    /// <summary>
    /// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
    /// </summary>
    internal interface IChannelOwner
    {
        /// <summary>
        /// Connection.
        /// </summary>
        Connection Connection { get; }

        /// <summary>
        /// Channel.
        /// </summary>
        ChannelBase Channel { get; }

        /// <summary>
        /// Child objects.
        /// </summary>
        ConcurrentDictionary<string, IChannelOwner> Objects { get; }

        /// <summary>
        /// Removes the object from the parent and the connection list.
        /// </summary>
        void DisposeOwner();
    }

    /// <summary>
    /// An IChannelOwner has the ability to build data coming from a Playwright server and convert it into a Playwright class.
    /// </summary>
    /// <typeparam name="T">Channel Owner implementation.</typeparam>
    internal interface IChannelOwner<T> : IChannelOwner
        where T : ChannelOwnerBase, IChannelOwner<T>
    {
        /// <summary>
        /// Channel.
        /// </summary>
        new IChannel<T> Channel { get; }
    }
}
